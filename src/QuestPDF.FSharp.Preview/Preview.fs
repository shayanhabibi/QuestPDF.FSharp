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

    let private release (server: Server) =
        match servers.TryGetValue server.Port with
        | true, entry when
            entry.IsValueCreated
            && obj.ReferenceEquals (entry.Value, server)
            ->
            servers.TryRemove (Collections.Generic.KeyValuePair (server.Port, entry))
            |> ignore
        | _ -> ()

    /// The render settings of options. Every reload mode is served as manual reload.
    let private settings (options: Options) (reloadHint: string option) : RenderSettings =
        { Poll = options.Poll
          Fonts = options.Fonts
          Reload = "manual"
          ReloadHint = reloadHint }

    /// The server of a port and whether this call started it. A started server has finished its first render.
    let internal start (env: ServeEnv) (options: Options) (reloadHint: string option) (document: unit -> IDocument) : Server * bool =
        let renderSettings = settings options reloadHint

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
            server.Refresh ()
            server, false

    let internal serveWith (env: ServeEnv) (options: Options) (document: unit -> #IDocument) : Server =
        start env options None (fun () -> document () :> IDocument)
        |> fst

    let serve (options: Options) (document: unit -> #IDocument) : Server =
        serveWith ServeEnv.standard options document

    let showOn (port: int) (document: unit -> #IDocument) : unit =
        let server, started =
            start ServeEnv.standard { defaults with Port = port } None (fun () -> document () :> IDocument)

        if started then
            printfn "Preview: %O" server.Url

    let show (document: unit -> #IDocument) : unit =
        showOn defaults.Port document

    let tryServer (port: int) : Server option =
        match servers.TryGetValue port with
        | true, entry when entry.IsValueCreated -> Some entry.Value
        | _ -> None

    let stop (port: int) : unit =
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
            |> Option.iter (fun server -> (server :> IDisposable).Dispose ())
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

[<AbstractClass; Sealed>]
type Preview =
    static member LiveWith
        (options: Preview.Options, document: unit -> #IDocument, [<CallerFilePath; Optional; DefaultParameterValue "">] script: string)
        : unit =
        let server, started =
            Preview.start ServeEnv.standard options (Some (Preview.liveHint script)) (fun () -> document () :> IDocument)

        if started then
            printfn "Preview: %O" server.Url

    static member Live(document: unit -> #IDocument, [<CallerFilePath; Optional; DefaultParameterValue "">] script: string) : unit =
        Preview.LiveWith (Preview.defaults, document, script)
