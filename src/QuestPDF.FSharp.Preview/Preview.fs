namespace QuestPDF.FSharp

open System
open System.Collections.Concurrent
open System.IO
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open QuestPDF.Infrastructure
open QuestPDF.FSharp.PreviewServer

[<RequireQualifiedAccess>]
module Preview =

    type Reload =
        | Auto
        | SageFs of daemon: Uri
        | Manual

    type Options =
        { Port: int
          Poll: TimeSpan option
          Reload: Reload
          WatchProjectFiles: bool
          Fonts: string list
          OpenBrowser: bool }

    let defaults =
        { Port = 5800
          Poll = Some (TimeSpan.FromMilliseconds 500.0)
          Reload = Auto
          WatchProjectFiles = true
          Fonts = []
          OpenBrowser = false }

    type Diagnostic =
        { File: string
          Line: int
          Column: int
          Message: string }

    type Status =
        | Starting
        | Rendered of pages: int * renderTime: TimeSpan
        | RenderFailed of error: string
        | CompileFailed of Diagnostic list
        | ReloadFailed of reason: string

    let private status (status: RenderStatus) =
        match status with
        | RenderStatus.Starting -> Starting
        | RenderStatus.Rendered (pages, time) -> Rendered (pages, time)
        | RenderStatus.RenderFailed error -> RenderFailed error
        | RenderStatus.CompileFailed diagnostics ->
            diagnostics
            |> List.map (fun d ->
                { File = d.File
                  Line = d.Line
                  Column = d.Column
                  Message = d.Message })
            |> CompileFailed
        | RenderStatus.ReloadFailed reason -> ReloadFailed reason

    [<Sealed>]
    type Server internal (engine: Engine, release: Server -> unit) =
        member _.Url = engine.Url
        member _.Port = engine.Port
        member _.Status = status engine.Snapshot.Status
        member _.Hint = engine.Snapshot.Hint
        member _.Version = engine.Snapshot.Version

        member _.Refresh() =
            engine.Refresh ()

        member internal _.Snapshot = engine.Snapshot
        member internal _.Clients = engine.Clients
        member internal _.Engine = engine

        interface IDisposable with
            member server.Dispose() =
                (engine :> IDisposable).Dispose ()
                release server

    /// The servers of the process by port.
    let private servers = ConcurrentDictionary<int, Lazy<Server>> ()

    /// The comparison of full script paths: case-insensitive on Windows.
    let private samePath: StringComparer =
        if OperatingSystem.IsWindows () then
            StringComparer.OrdinalIgnoreCase
        else
            StringComparer.Ordinal

    /// The script watchers of the process by full script path.
    let private scripts = ConcurrentDictionary<string, Lazy<ScriptWatcher>> (samePath)

    /// The stopped watchers of the process by full script path, kept while their last reload may still run the script.
    let private retired = ConcurrentDictionary<string, ScriptWatcher> (samePath)

    /// Stops the watcher of a script.
    let private unwatch (path: string) (entry: Lazy<ScriptWatcher>) =
        if
            scripts.TryRemove (Collections.Generic.KeyValuePair (path, entry))
            && entry.IsValueCreated
        then
            (entry.Value :> IDisposable).Dispose ()
            retired[path] <- entry.Value

    /// Whether a run of a script belongs to a reload of a stopped watcher; such a run leaves the preview stopped.
    let private orphaned (path: string) =
        match retired.TryGetValue path with
        | true, watcher when watcher.Reloading -> true
        | true, watcher ->
            retired.TryRemove (Collections.Generic.KeyValuePair (path, watcher))
            |> ignore

            false
        | _ -> false

    /// The link of a server to a SageFs project session: the reader of the daemon's events, whether the preview watches
    /// project files, and the session file watching was turned on for.
    type private ProjectLink =
        { Server: Server
          Daemon: Uri
          Nudger: ProjectNudger
          mutable WatchFiles: bool
          mutable Watched: string option }

    /// The links of servers in SageFs project sessions by port, guarded by projectSync.
    let private links = Collections.Generic.Dictionary<int, ProjectLink> ()

    let private projectSync = obj ()

    /// Stops the reader of SageFs events of a port, when the link of the port satisfies a condition.
    let private unlinkWhen (condition: ProjectLink -> bool) (port: int) =
        let removed =
            lock projectSync (fun () ->
                match links.TryGetValue port with
                | true, link when condition link ->
                    links.Remove port |> ignore
                    Some link.Nudger
                | _ -> None)

        removed
        |> Option.iter (fun nudger -> (nudger :> IDisposable).Dispose ())

    /// Stops the reader of SageFs events of a port.
    let private unlink (port: int) = unlinkWhen (fun _ -> true) port

    /// Stops the reader of SageFs events of the port of a server, when the reader belongs to the server.
    let private unlinkServer (server: Server) =
        unlinkWhen (fun link -> obj.ReferenceEquals (link.Server, server)) server.Port

    /// Stops the watchers of the scripts shown on a port and the reader of SageFs events of the port.
    let private stopWatchers (port: int) =
        for KeyValue (path, entry) in scripts do
            if entry.IsValueCreated && entry.Value.Port = port then
                unwatch path entry

        unlink port

    /// Whether a server is the server of its port.
    let private current (server: Server) =
        match servers.TryGetValue server.Port with
        | true, entry -> entry.IsValueCreated && obj.ReferenceEquals (entry.Value, server)
        | _ -> false

    let private release (server: Server) =
        match servers.TryGetValue server.Port with
        | true, entry when
            entry.IsValueCreated
            && obj.ReferenceEquals (entry.Value, server)
            ->
            stopWatchers server.Port

            servers.TryRemove (Collections.Generic.KeyValuePair (server.Port, entry))
            |> ignore

            // A serve that ran before the removal may have linked the server again.
            unlinkServer server
        | _ -> ()

    /// The render settings of options, with the reload mode shown in the status bar.
    let private settings (options: Options) (reload: string) (reloadHint: string option) : RenderSettings =
        { Poll = options.Poll
          Fonts = options.Fonts
          Reload = reload
          ReloadHint = reloadHint }

    /// The server of a port and whether this call started it. A started server has finished its first render.
    let internal start (env: ServeEnv) (options: Options) (reload: string) (reloadHint: string option) (document: unit -> IDocument) : Server * bool =
        // SageFs events re-render only a server in a project session.
        if reload <> "sagefs-project" then
            unlink options.Port

        let renderSettings = settings options reload reloadHint

        let candidate =
            lazy (new Server (new Engine (options.Port, env, renderSettings, document), release))

        let entry = servers.GetOrAdd (options.Port, candidate)

        if obj.ReferenceEquals (entry, candidate) then
            let server =
                try
                    entry.Value
                with _ ->
                    servers.TryRemove (Collections.Generic.KeyValuePair (options.Port, entry))
                    |> ignore

                    reraise ()

            server.Refresh ()

            if options.OpenBrowser then
                env.OpenBrowser server.Url

            server, true
        else
            let server = entry.Value
            server.Engine.Update (document, renderSettings)

            // A reload issue belongs to the SageFs script mode.
            if reload <> "sagefs-script" then
                server.Engine.Reloaded Loaded

            server.Refresh ()
            server, false

    /// The SageFs daemon that reloads scripts under options: the daemon of Reload.SageFs, or with Reload.Auto the
    /// daemon of the environment when the process is a SageFs session.
    let private daemonOf (env: ServeEnv) (options: Options) : Uri option =
        match options.Reload with
        | Manual -> None
        | SageFs daemon -> Some daemon
        | Auto ->
            env.Variable "SAGEFS_DAEMON_PID"
            |> Option.map (fun _ -> env.Daemon)

    /// The daemon and the projects of the SageFs project session of the process under options.
    let private projectSession (env: ServeEnv) (options: Options) : (Uri * string) option =
        match daemonOf env options, env.Variable "SAGEFS_SESSION_PROJECTS" with
        | Some daemon, Some projects -> Some (daemon, projects)
        | _ -> None

    /// Clears a routing failure of the server of a port in a SageFs project session, and turns on file watching for a
    /// session found for the port once, when the preview watches project files.
    let private identified (port: int) (session: SessionInfo) =
        match servers.TryGetValue port with
        | true, entry when
            entry.IsValueCreated
            && entry.Value.Snapshot.Reload = "sagefs-project"
            ->
            entry.Value.Engine.Reloaded Loaded

            let claimed =
                lock projectSync (fun () ->
                    match links.TryGetValue port with
                    | true, link when link.WatchFiles && link.Watched <> Some session.Id ->
                        link.Watched <- Some session.Id
                        Some link.Daemon
                    | _ -> None)

            match claimed with
            | Some daemon ->
                match SageFsClient.watchAll daemon session.Id with
                | Ok count -> printfn "Preview: SageFs session %s, watching %d files" session.Id count
                | Error reason ->
                    let route =
                        Uri (SageFsProtocol.dashboard daemon, $"api/sessions/{session.Id}/hotreload/watch-all")

                    printfn "Preview: SageFs session %s; file watching is off: %s" session.Id reason
                    printfn "Preview: turn it on with: curl -X POST %O" route
            | None -> ()
        | _ -> ()

    /// The reader of SageFs events of the port of a server, started when the port has none or has one of another server
    /// or daemon; None when the server is no longer the server of its port.
    let private link (server: Server) (daemon: Uri) (watchFiles: bool) (identify: unit -> Result<SessionInfo, string>) =
        let port = server.Port

        let nudge () =
            match servers.TryGetValue port with
            | true, entry when entry.IsValueCreated -> entry.Value.Engine.Nudge ()
            | _ -> ()

        let find () =
            match identify () with
            | Ok session as found ->
                identified port session
                found
            | failed -> failed

        let linked, replaced =
            lock projectSync (fun () ->
                if not (current server) then
                    None, None
                else
                    match links.TryGetValue port with
                    | true, link when
                        link.Daemon = daemon
                        && obj.ReferenceEquals (link.Server, server)
                        ->
                        link.WatchFiles <- watchFiles
                        Some link.Nudger, None
                    | found ->
                        let link =
                            { Server = server
                              Daemon = daemon
                              Nudger = new ProjectNudger (daemon, find, nudge)
                              WatchFiles = watchFiles
                              Watched = None }

                        links[port] <- link

                        Some link.Nudger,
                        (match found with
                         | true, previous -> Some previous.Nudger
                         | _ -> None))

        replaced
        |> Option.iter (fun nudger -> (nudger :> IDisposable).Dispose ())

        linked

    /// The server of a port, re-rendered on the SageFs events of the session in a SageFs project session. The first
    /// start of a port prints the URL when announce is true. In a project session, file watching is turned on once for
    /// the session found for the port, and the call returns once the event stream is open, waiting up to 2 s.
    let private serveIn (env: ServeEnv) (announce: bool) (options: Options) (document: unit -> IDocument) : Server =
        let launch (reload: string) =
            let server, started = start env options reload None document

            if started && announce then
                printfn "Preview: %O" server.Url

            server

        match projectSession env options with
        | None -> launch "manual"
        | Some (daemon, projects) ->
            let server = launch "sagefs-project"

            let identify () =
                SageFsClient.session daemon projects (env.CurrentDirectory ())

            match link server daemon options.WatchProjectFiles identify with
            | Some nudger ->
                match identify () with
                | Error reason -> server.Engine.Reloaded (Routing reason)
                | Ok session ->
                    identified options.Port session
                    nudger.WaitConnected (TimeSpan.FromSeconds 2.0) |> ignore
            | None -> ()

            server

    let internal serveWith (env: ServeEnv) (options: Options) (document: unit -> #IDocument) : Server =
        serveIn env false options (fun () -> document () :> IDocument)

    let serve (options: Options) (document: unit -> #IDocument) : Server =
        serveWith ServeEnv.standard options document

    let showOn (port: int) (document: unit -> #IDocument) : unit =
        serveIn ServeEnv.standard true { defaults with Port = port } (fun () -> document () :> IDocument)
        |> ignore

    let show (document: unit -> #IDocument) : unit =
        showOn defaults.Port document

    let tryServer (port: int) : Server option =
        match servers.TryGetValue port with
        | true, entry when entry.IsValueCreated -> Some entry.Value
        | _ -> None

    let stop (port: int) : unit =
        stopWatchers port

        match servers.TryRemove port with
        | true, entry ->
            // A server still binding its port is disposed once it exists; the serve that creates it then raises
            // ObjectDisposedException.
            let server =
                try
                    Some entry.Value
                with _ ->
                    None

            server
            |> Option.iter (fun server ->
                (server :> IDisposable).Dispose ()
                // A serve that ran before the removal may have linked the server again.
                unlinkServer server)
        | _ -> ()

    /// The banner of a script preview: why saving the script does not reload it.
    let internal liveHint (script: string) =
        if
            String.IsNullOrEmpty script
            || not (File.Exists script)
        then
            "The script path is unknown, as for code sent as a selection; evaluate the file (for example with #load) to reload it on save."
        else
            "Automatic reload needs SageFs; send the file to FSI again."

    /// Shows the outcome of a script reload on the server of a port.
    let private report (port: int) (outcome: ReloadOutcome) =
        tryServer port
        |> Option.iter (fun server -> server.Engine.Reloaded outcome)

    let internal liveWith (env: ServeEnv) (options: Options) (script: string) (document: unit -> IDocument) : unit =
        let known =
            not (String.IsNullOrEmpty script)
            && File.Exists script

        match daemonOf env options with
        | _ when known && orphaned (Path.GetFullPath script) -> ()
        | Some daemon when known ->
            let path = Path.GetFullPath script
            let server, started = start env options "sagefs-script" None document

            if started then
                printfn "Preview: %O" server.Url

            let projects =
                env.Variable "SAGEFS_SESSION_PROJECTS"
                |> Option.defaultValue ""

            let reload () =
                SageFsClient.reload daemon path projects (env.CurrentDirectory ())

            let others (changed: string) =
                not (samePath.Equals (changed, path))
                && scripts.ContainsKey changed

            let candidate =
                lazy (new ScriptWatcher (path, options.Port, env.Now, reload, report options.Port, others))

            let entry = scripts.GetOrAdd (path, candidate)
            let first = obj.ReferenceEquals (entry, candidate)

            if first then
                try
                    entry.Force () |> ignore
                with _ ->
                    scripts.TryRemove (Collections.Generic.KeyValuePair (path, entry))
                    |> ignore

                    reraise ()
            else
                entry.Value.Retarget (options.Port, reload, report options.Port)

            // Outside a reload the script was evaluated by hand: the new code replaces a failed reload, and the page
            // shows whether saves can reach the session.
            if not entry.Value.Reloading then
                let name = Path.GetFileName path

                match SageFsClient.session daemon projects (env.CurrentDirectory ()) with
                | Ok session ->
                    server.Engine.Reloaded Loaded

                    if first then
                        printfn "Preview: reloading %s through SageFs session %s on save" name session.Id
                | Error reason ->
                    server.Engine.Reloaded (Routing reason)

                    if first then
                        printfn "Preview: reloading %s through SageFs on save; %s" name reason
        | _ ->
            if known then
                match scripts.TryGetValue (Path.GetFullPath script) with
                | true, entry -> unwatch (Path.GetFullPath script) entry
                | _ -> ()

            let server, started = start env options "manual" (Some (liveHint script)) document

            if started then
                printfn "Preview: %O" server.Url

    let internal watchedScripts () : string list =
        scripts.Keys |> List.ofSeq

[<AbstractClass; Sealed>]
type Preview =
    static member LiveWith
        (options: Preview.Options, document: unit -> #IDocument, [<CallerFilePath; Optional; DefaultParameterValue "">] script: string)
        : unit =
        Preview.liveWith ServeEnv.standard options script (fun () -> document () :> IDocument)

    static member Live(document: unit -> #IDocument, [<CallerFilePath; Optional; DefaultParameterValue "">] script: string) : unit =
        Preview.LiveWith (Preview.defaults, document, script)
