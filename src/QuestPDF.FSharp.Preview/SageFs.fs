namespace QuestPDF.FSharp.PreviewServer

open System
open System.IO
open System.Net.Http
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

    /// The session of the current process, by the projects of SAGEFS_SESSION_PROJECTS or by the current directory.
    let session (daemon: Uri) (projects: string) (currentDirectory: string) : Result<SessionInfo, string> =
        try
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

/// Reloads a script on every save of an F# file in the script's directory tree, one reload at a time. Saves within
/// 100 ms of each other make one reload, and saves during a reload make one follow-up reload.
type internal ScriptWatcher(script: string, port: int, now: unit -> DateTime, reload: unit -> ReloadOutcome, report: ReloadOutcome -> unit) =
    let root = Path.GetDirectoryName script
    let window = TimeSpan.FromMilliseconds 100.0
    let sync = obj ()
    let wake = new AutoResetEvent (false)
    let mutable state = ChangeFilter.idle
    let mutable target = port, reload, report
    let mutable disposed = false

    let save () =
        lock sync (fun () -> state <- ChangeFilter.event window (now ()) state)
        wake.Set () |> ignore

    let changed (change: WatcherChangeTypes) (path: string) =
        try
            if ChangeFilter.classify root change path then
                save ()
        with _ ->
            ()

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

    let thread =
        Thread (
            (fun () ->
                while not disposed do
                    try
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
                    with _ ->
                        ()),
            IsBackground = true,
            Name = "QuestPDF preview script reload"
        )

    do
        watcher.Changed.Add (fun e -> changed e.ChangeType e.FullPath)
        watcher.Created.Add (fun e -> changed e.ChangeType e.FullPath)
        watcher.Renamed.Add (fun e -> changed e.ChangeType e.FullPath)
        // A lost event buffer may hide a save.
        watcher.Error.Add (fun _ -> save ())
        watcher.EnableRaisingEvents <- true
        thread.Start ()

    /// The full path of the watched script.
    member _.Script = script

    /// The port of the preview that shows the outcomes.
    member _.Port =
        lock sync (fun () ->
            let port, _, _ = target
            port)

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

                wake.Set () |> ignore
