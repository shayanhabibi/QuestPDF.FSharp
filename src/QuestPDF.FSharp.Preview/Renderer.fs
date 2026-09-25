namespace QuestPDF.FSharp.PreviewServer

open System
open System.Diagnostics
open System.IO
open System.Net
open System.Threading
open QuestPDF.Infrastructure
open QuestPDF.FSharp

/// The seams of a preview server.
type internal ServeEnv =
    {
        /// The address of the SageFs daemon.
        Daemon: Uri
        /// Opens a page in a browser.
        OpenBrowser: Uri -> unit
        /// The current time.
        Now: unit -> DateTime
        /// How long a port in use is retried before serve raises.
        BindTimeout: TimeSpan
        /// The interval of the SSE keep-alive comments.
        KeepAlive: TimeSpan
        /// The value of an environment variable; None when unset or empty.
        Variable: string -> string option
        /// The working directory of the process.
        CurrentDirectory: unit -> string
    }

[<RequireQualifiedAccess>]
module internal ServeEnv =
    /// The daemon at http://localhost:37749/, the default browser, a 15 s bind timeout, a 15 s keep-alive and the
    /// process environment.
    let standard: ServeEnv =
        { Daemon = Uri "http://localhost:37749/"
          OpenBrowser =
            fun url ->
                try
                    Process.Start (ProcessStartInfo (string url, UseShellExecute = true))
                    |> ignore
                with _ ->
                    ()
          Now = fun () -> DateTime.UtcNow
          BindTimeout = TimeSpan.FromSeconds 15.0
          KeepAlive = TimeSpan.FromSeconds 15.0
          Variable =
            fun name ->
                match Environment.GetEnvironmentVariable name with
                | null
                | "" -> None
                | value -> Some value
          CurrentDirectory = fun () -> Environment.CurrentDirectory }

/// The settings of a running preview that a repeated serve replaces.
type internal RenderSettings =
    {
        /// The shortest interval between polls while a page is open.
        Poll: TimeSpan option
        /// Font files or directories served in addition to Font.sources ().
        Fonts: string list
        /// The reload mode shown in the status bar: sagefs-script, sagefs-project or manual.
        Reload: string
        /// The banner about reloading, shown when no other hint applies.
        ReloadHint: string option
    }

/// The render loop and HTTP routes of a preview on a port.
type internal Engine(port: int, env: ServeEnv, initialSettings: RenderSettings, initialDocument: unit -> IDocument) =
    let listener = Http.bind port env.BindTimeout env.Now
    let hub = new SseHub (env.KeepAlive)
    let wake = new AutoResetEvent (false)
    let sync = obj ()
    let instance = Guid.NewGuid().ToString("N").Substring (0, 12)

    let mutable document = initialDocument
    let mutable settings = initialSettings
    let mutable snapshot = Snapshot.initial instance initialSettings.Reload
    let mutable fonts: ServedFont list = []
    let mutable gate = Idle
    let mutable requested = 0L
    let mutable completed = 0L
    let mutable lastDocument: obj = null
    let mutable frozenHint: string option = None
    let mutable lastRender = TimeSpan.Zero
    let mutable disposed = false

    let url = Uri $"http://localhost:{port}/"

    let versionEvent (snapshot: Snapshot) =
        $"event: version\ndata: {Snapshot.json snapshot}\n\n"

    /// Replaces the snapshot and pushes a version event when the version changed.
    let publish (update: Snapshot -> Snapshot) =
        let changed =
            lock sync (fun () ->
                let next = update snapshot
                let changed = next.Version <> snapshot.Version
                snapshot <- next
                if changed then Some next else None)

        changed
        |> Option.iter (versionEvent >> hub.Broadcast)

    let fontSources () =
        let extra =
            settings.Fonts
            |> List.map (fun path ->
                if Directory.Exists path then
                    FontDirectory (Path.GetFullPath path)
                else
                    FontFile (Path.GetFullPath path))

        Font.sources () @ extra

    let renderOnce () =
        let clock = Stopwatch.StartNew ()

        let outcome =
            try
                let served = FontFace.expand (fontSources ())
                let current = document ()
                frozenHint <- Schedule.frozen lastDocument (box current)
                lastDocument <- box current

                let svgs =
                    Pdf.svgs current
                    |> List.map (
                        FontFace.weights
                        >> FontFace.embed (FontFace.css served)
                    )

                lock sync (fun () -> fonts <- served)
                Pages (svgs, clock.Elapsed)
            with error ->
                Failure (string error)

        lastRender <- clock.Elapsed
        let hint = frozenHint |> Option.orElse settings.ReloadHint
        publish (Snapshot.render outcome hint)

    let rec work () =
        let upto = lock sync (fun () -> requested)

        try
            renderOnce ()
        with error ->
            publish (Snapshot.withStatus (RenderFailed (string error)))

        let again =
            lock sync (fun () ->
                completed <- upto
                let next, start = Schedule.finish gate
                gate <- next
                Monitor.PulseAll sync
                start)

        if again then
            work ()

    /// Requests a render and returns the number of the request.
    let trigger () =
        let number, start =
            lock sync (fun () ->
                if disposed then
                    raise (ObjectDisposedException $"The preview on port {port} is stopped.")

                requested <- requested + 1L
                let next, start = Schedule.trigger gate
                gate <- next
                requested, start)

        if start then
            ThreadPool.QueueUserWorkItem (fun _ -> work ())
            |> ignore

        number

    let refresh () =
        let number = trigger ()

        lock sync (fun () ->
            while completed < number && not disposed do
                Monitor.Wait sync |> ignore

            if completed < number then
                raise (ObjectDisposedException $"The preview on port {port} is stopped."))

    let pollThread =
        Thread (
            (fun () ->
                while not disposed do
                    try
                        match Schedule.pollDelay settings.Poll hub.Count lastRender with
                        | Some delay ->
                            if not (wake.WaitOne delay) && not disposed then
                                // A closed page is found by a failed write; without this ping it would be
                                // re-rendered until the next keep-alive.
                                hub.Ping ()

                                if hub.Count > 0 then
                                    trigger () |> ignore
                        | None -> wake.WaitOne 250 |> ignore
                    with _ ->
                        ()),
            IsBackground = true,
            Name = "QuestPDF preview poll"
        )

    let page (path: string) (query: string) =
        let current = lock sync (fun () -> snapshot)

        match Int32.TryParse (path.Substring (6, path.Length - 10)) with
        | true, n when n >= 1 && n <= current.Pages.Length ->
            let hash = current.Hashes[n - 1]

            { Status = 200
              ContentType = "image/svg+xml; charset=utf-8"
              CacheControl =
                if query = $"?h={hash}" then
                    "public, max-age=31536000, immutable"
                else
                    "no-store"
              Body = Text.Encoding.UTF8.GetBytes current.Pages[n - 1] }
        | _ -> Reply.notFound

    /// A served font; cached for good only under the hash of the bytes it returns, since an index names another
    /// font when the registrations change.
    let font (index: string) (query: string) =
        let served = lock sync (fun () -> fonts)

        match Int32.TryParse index with
        | true, i when i >= 0 && i < served.Length ->
            match FontFace.bytes served[i].Content with
            | Some bytes ->
                let isOpenType =
                    bytes.Length >= 4
                    && Text.Encoding.ASCII.GetString (bytes, 0, 4) = "OTTO"

                { Status = 200
                  ContentType = if isOpenType then "font/otf" else "font/ttf"
                  CacheControl =
                    if query = $"?h={FontFace.hash bytes}" then
                        "public, max-age=31536000, immutable"
                    else
                        "no-store"
                  Body = bytes }
            | None -> Reply.notFound
        | _ -> Reply.notFound

    let pdf () =
        try
            { Status = 200
              ContentType = "application/pdf"
              CacheControl = "no-store"
              Body = Pdf.bytes (document ()) }
        with error ->
            Reply.text 500 "text/plain; charset=utf-8" (string error)

    let handle (context: HttpListenerContext) =
        let path = context.Request.Url.AbsolutePath
        let query = context.Request.Url.Query

        match path with
        | "/events" ->
            let current = lock sync (fun () -> snapshot)
            hub.Add (context.Response, "retry: 500\n\n" + versionEvent current)
            wake.Set () |> ignore
        | "/" ->
            Http.reply
                context
                { Reply.text 200 "text/html; charset=utf-8" "" with
                    Body = Shell.html.Value }
        | "/snapshot" ->
            let current = lock sync (fun () -> snapshot)
            Http.reply context (Reply.text 200 "application/json; charset=utf-8" (Snapshot.json current))
        | "/document.pdf" -> Http.reply context (pdf ())
        | _ when path.StartsWith "/page/" && path.EndsWith ".svg" -> Http.reply context (page path query)
        | _ when path.StartsWith "/font/" -> Http.reply context (font (path.Substring 6) query)
        | _ -> Http.reply context Reply.notFound

    do
        Http.accept listener handle
        pollThread.Start ()

    /// The address of the page.
    member _.Url = url

    /// The port of the page.
    member _.Port = port

    /// The published state.
    member _.Snapshot = lock sync (fun () -> snapshot)

    /// The number of open event streams.
    member _.Clients = hub.Count

    /// Replaces the document function and the settings.
    member _.Update(next: unit -> IDocument, nextSettings: RenderSettings) =
        lock sync (fun () ->
            document <- next
            settings <- nextSettings

            if snapshot.Reload <> nextSettings.Reload then
                snapshot <-
                    { snapshot with
                        Reload = nextSettings.Reload })

        wake.Set () |> ignore

    /// Shows the outcome of a script reload: a failure stays the shown status until a reload succeeds.
    member _.Reloaded(outcome: ReloadOutcome) =
        publish (Snapshot.reloaded (ExecProtocol.issue outcome))

    /// Requests a render at once and another 250 ms later, and returns without waiting for either; a no-op on a
    /// stopped server.
    member _.Nudge() =
        let request () =
            try
                trigger () |> ignore
            with _ ->
                ()

        request ()

        Tasks.Task.Delay(250).ContinueWith (fun (_: Tasks.Task) -> request ())
        |> ignore

    /// Renders at once and returns when the render is published.
    member _.Refresh() =
        refresh ()

    interface IDisposable with
        member _.Dispose() =
            let first =
                lock sync (fun () ->
                    let first = not disposed
                    disposed <- true
                    Monitor.PulseAll sync
                    first)

            if first then
                wake.Set () |> ignore
                (hub :> IDisposable).Dispose ()

                try
                    listener.Stop ()
                    listener.Close ()
                with _ ->
                    ()
