namespace QuestPDF.FSharp.PreviewServer

open System
open System.IO
open System.Net.Http
open System.Net.NetworkInformation
open System.Text
open System.Threading

/// The HTTP client of the SageFs daemon. Every call blocks the calling thread.
[<RequireQualifiedAccess>]
module internal SageFsClient =
    let private client = new HttpClient (Timeout = Timeout.InfiniteTimeSpan)

    /// The status code and body of a request, sent with a timeout. Raises on a connection failure or the timeout.
    let private send (request: HttpRequestMessage) (timeout: TimeSpan) : int * string =
        let cancel = new CancellationTokenSource (timeout)

        try
            let response = client.SendAsync(request, cancel.Token).GetAwaiter().GetResult ()

            try
                int response.StatusCode, response.Content.ReadAsStringAsync(cancel.Token).GetAwaiter().GetResult ()
            finally
                response.Dispose ()
        finally
            cancel.Dispose ()
            request.Dispose ()

    /// The reason given for a daemon that cannot be reached.
    let private unreachable (daemon: Uri) (error: exn) =
        let error =
            match error with
            | :? OperationCanceledException -> "the request timed out"
            | error -> error.Message

        $"The SageFs daemon at {daemon} is unreachable: {error}"

    /// Whether a daemon can accept connections: true for a remote host, and for a loopback host while a TCP listener
    /// holds its port.
    let private listening (daemon: Uri) =
        // Windows retries a refused loopback connection for about 2 s per address of localhost.
        try
            not daemon.IsLoopback
            || IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners ()
               |> Array.exists (fun endpoint -> endpoint.Port = daemon.Port)
        with _ ->
            true

    /// The session of the current process, by the projects of SAGEFS_SESSION_PROJECTS or by the current directory.
    let session (daemon: Uri) (projects: string) (currentDirectory: string) : Result<SessionInfo, string> =
        try
            if not (listening daemon) then
                raise (HttpRequestException $"no process listens on port {daemon.Port}")

            let status, body =
                send (new HttpRequestMessage (HttpMethod.Get, Uri (daemon, "api/sessions"))) (TimeSpan.FromSeconds 5.0)

            if status <> 200 then
                Error $"The SageFs daemon answered HTTP {status} to GET /api/sessions."
            else
                SageFsProtocol.sessions body
                |> SageFsProtocol.matchSession (OperatingSystem.IsWindows ()) projects currentDirectory
        with error ->
            Error (unreachable daemon error)

    /// Loads a script again into the session of the current process, and returns when the load has finished.
    let reload (daemon: Uri) (script: string) (projects: string) (currentDirectory: string) : ReloadOutcome =
        match session daemon projects currentDirectory with
        | Error reason -> Routing reason
        | Ok session ->
            try
                let request = new HttpRequestMessage (HttpMethod.Post, Uri (daemon, "exec"))

                request.Content <- new StringContent (ExecProtocol.request script session.WorkingDirectory, Encoding.UTF8, "application/json")

                let status, body = send request (TimeSpan.FromMinutes 5.0)
                ExecProtocol.parse script status body
            with error ->
                Routing (unreachable daemon error)

    /// Turns on file watching for every project file of a session, through the dashboard of the daemon, and returns
    /// the number of watched files.
    let watchAll (daemon: Uri) (sessionId: string) : Result<int, string> =
        let dashboard = SageFsProtocol.dashboard daemon

        try
            let request =
                new HttpRequestMessage (HttpMethod.Post, Uri (dashboard, $"api/sessions/{sessionId}/hotreload/watch-all"))

            request.Content <- new StringContent ("", Encoding.UTF8, "application/json")

            match send request (TimeSpan.FromSeconds 5.0) with
            | 200, body ->
                match SageFsProtocol.watchedCount body with
                | Some count -> Ok count
                | None -> Error "The SageFs dashboard answered watch-all without a watchedCount."
            | status, _ -> Error $"The SageFs dashboard answered HTTP {status} to watch-all."
        with error ->
            Error (unreachable dashboard error)

/// Calls nudge for every event of the SageFs daemon that reports new code in the session of the process, from a
/// background thread. The event stream is opened again after it ends or fails, after 0.5 s doubled per failed attempt
/// up to 10 s, and the session is identified again on every attempt.
type internal ProjectNudger(daemon: Uri, session: unit -> Result<SessionInfo, string>, nudge: unit -> unit) =
    let client = new HttpClient (Timeout = Timeout.InfiniteTimeSpan)
    let stopping = new CancellationTokenSource ()
    let mutable disposed = false

    /// Reads the event stream of the daemon until it ends, and returns whether the stream opened.
    let read (sessionId: string) : bool =
        let mutable opened = false
        let request = new HttpRequestMessage (HttpMethod.Get, Uri (daemon, "events"))

        try
            try
                let response =
                    client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, stopping.Token).GetAwaiter().GetResult ()

                try
                    if response.IsSuccessStatusCode then
                        opened <- true
                        let reader = new StreamReader (response.Content.ReadAsStream stopping.Token)

                        try
                            let next () =
                                reader.ReadLineAsync(stopping.Token).AsTask().GetAwaiter().GetResult ()

                            let mutable pending = SageFsProtocol.sseStart
                            let mutable line = next ()

                            while not (isNull line) do
                                let state, event = SageFsProtocol.sseLine pending line
                                pending <- state

                                match event with
                                | Some event when SageFsProtocol.nudges sessionId event ->
                                    try
                                        nudge ()
                                    with _ ->
                                        ()
                                | _ -> ()

                                line <- next ()
                        finally
                            reader.Dispose ()
                finally
                    response.Dispose ()
            with _ ->
                ()
        finally
            request.Dispose ()

        opened

    let thread =
        Thread (
            (fun () ->
                try
                    let mutable attempt = 0

                    while not disposed do
                        let opened =
                            try
                                match session () with
                                | Ok found when not disposed -> read found.Id
                                | _ -> false
                            with _ ->
                                false

                        attempt <- if opened then 0 else attempt + 1

                        if not disposed then
                            try
                                stopping.Token.WaitHandle.WaitOne (SageFsProtocol.reconnectDelay attempt)
                                |> ignore
                            with _ ->
                                ()
                finally
                    client.Dispose ()),
            IsBackground = true,
            Name = "QuestPDF preview SageFs events"
        )

    do thread.Start ()

    interface IDisposable with
        member _.Dispose() =
            if not disposed then
                disposed <- true

                try
                    stopping.Cancel ()
                with _ ->
                    ()

/// Reloads a script on every save of an F# file in the script's directory tree, one reload at a time. Saves within
/// 100 ms of each other make one reload, and saves during a reload make one follow-up reload. Saves of a path for
/// which others is true, such as the script of another watcher, are ignored.
type internal ScriptWatcher
    (script: string, port: int, now: unit -> DateTime, reload: unit -> ReloadOutcome, report: ReloadOutcome -> unit, others: string -> bool) =
    let root = Path.GetDirectoryName script
    let window = TimeSpan.FromMilliseconds 100.0
    let sync = obj ()
    let wake = new AutoResetEvent (false)
    let mutable state = ChangeFilter.idle
    let mutable target = port, reload, report
    let mutable disposed = false

    /// Wakes the loop; a no-op once the loop has ended.
    let signal () =
        try
            wake.Set () |> ignore
        with :? ObjectDisposedException ->
            ()

    /// Shows an error of the watcher as a reload failure of the script.
    let record (error: exn) =
        if not disposed then
            try
                let _, _, report = lock sync (fun () -> target)
                report (Routing $"The reload watcher of {Path.GetFileName script} failed: {error.Message}")
            with _ ->
                ()

    let save () =
        lock sync (fun () -> state <- ChangeFilter.event window (now ()) state)
        signal ()

    let changed (change: WatcherChangeTypes) (path: string) =
        if not disposed then
            try
                if
                    ChangeFilter.classify root change path
                    && not (others path)
                then
                    save ()
            with error ->
                record error

    let watcher =
        new FileSystemWatcher (
            root,
            IncludeSubdirectories = true,
            NotifyFilter =
                (NotifyFilters.FileName
                 ||| NotifyFilters.LastWrite
                 ||| NotifyFilters.Size),
            InternalBufferSize = 65536
        )

    /// Runs a due reload and reports its outcome.
    let run () =
        let _, reload, report = lock sync (fun () -> target)

        try
            let outcome =
                try
                    reload ()
                with error ->
                    Routing error.Message

            if not disposed then
                report outcome
        finally
            lock sync (fun () -> state <- ChangeFilter.finished window (now ()) state)

    /// One pass of the loop: a due reload, or a wait for the next save or due time.
    let step () =
        let start, wait =
            lock sync (fun () ->
                let next, start = ChangeFilter.tick (now ()) state
                state <- next
                start, ChangeFilter.wait (now ()) state)

        if start then
            run ()
        else
            match wait with
            | Some wait ->
                wake.WaitOne (max wait (TimeSpan.FromMilliseconds 1.0))
                |> ignore
            | None -> wake.WaitOne () |> ignore

    let thread =
        Thread (
            (fun () ->
                try
                    while not disposed do
                        try
                            step ()
                        with error ->
                            record error

                            // A failing pass waits before the next, so a persistent error reports every 250 ms.
                            try
                                wake.WaitOne 250 |> ignore
                            with _ ->
                                ()
                finally
                    wake.Dispose ()),
            IsBackground = true,
            Name = "QuestPDF preview script reload"
        )

    do
        watcher.Changed.Add (fun e -> changed e.ChangeType e.FullPath)
        watcher.Created.Add (fun e -> changed e.ChangeType e.FullPath)
        watcher.Renamed.Add (fun e -> changed e.ChangeType e.FullPath)
        // A lost event buffer may hide a save.
        watcher.Error.Add (fun _ ->
            if not disposed then
                try
                    save ()
                with error ->
                    record error)

        watcher.EnableRaisingEvents <- true
        thread.Start ()

    /// The full path of the watched script.
    member _.Script = script

    /// The port of the preview that shows the outcomes.
    member _.Port =
        lock sync (fun () ->
            let port, _, _ = target
            port)

    /// Whether a reload is in flight, from the request to the daemon until its outcome is reported.
    member _.Reloading = lock sync (fun () -> state.InFlight)

    /// Replaces the port, the reload and the report of outcomes.
    member _.Retarget(nextPort: int, nextReload: unit -> ReloadOutcome, nextReport: ReloadOutcome -> unit) =
        lock sync (fun () -> target <- nextPort, nextReload, nextReport)

    interface IDisposable with
        member _.Dispose() =
            if not disposed then
                disposed <- true

                try
                    watcher.EnableRaisingEvents <- false
                    watcher.Dispose ()
                with _ ->
                    ()

                signal ()
