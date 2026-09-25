namespace QuestPDF.FSharp

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open QuestPDF.Infrastructure

/// <summary>
/// Browser previews of document functions, served from the process that runs the document code on
/// <c>http://localhost:&lt;port&gt;/</c> and re-rendered while the page is open.
/// </summary>
[<RequireQualifiedAccess>]
module Preview =

    /// <summary>The source of new code after a file is saved.</summary>
    type Reload =
        /// <summary>
        /// SageFs at <c>http://localhost:37749/</c> when the process is a SageFs session (<c>SAGEFS_DAEMON_PID</c> is
        /// set); Manual otherwise.
        /// </summary>
        | Auto
        /// <summary>The SageFs daemon at an address, for a daemon started with <c>--mcp-port</c>.</summary>
        | SageFs of daemon: Uri
        /// <summary>Re-rendering only: new code arrives through the editor's send-to-FSI or the REPL.</summary>
        | Manual

    /// <summary>Preview settings.</summary>
    type Options =
        {
            /// <summary>
            /// The port of <c>http://localhost:&lt;port&gt;/</c>. A port identifies a preview within the process.
            /// Default 5800.
            /// </summary>
            Port: int
            /// <summary>
            /// The shortest interval between re-renders while a page is open; None re-renders only on Refresh, on a
            /// repeated call and on SageFs events. Default Some 500 ms.
            /// </summary>
            Poll: TimeSpan option
            /// <summary>The source of new code after a save. Default Auto.</summary>
            Reload: Reload
            /// <summary>
            /// Turns on SageFs file watching for the projects of the session on the first start of a port. Default
            /// true.
            /// </summary>
            WatchProjectFiles: bool
            /// <summary>
            /// Font files or directories served to the browser in addition to <c>Font.sources ()</c>. Default [].
            /// </summary>
            Fonts: string list
            /// <summary>Opens the page in the default browser on the first start of a port. Default false.</summary>
            OpenBrowser: bool
        }

    /// <summary>Port 5800, a 500 ms poll, Reload.Auto, project watching on, no extra fonts, no browser.</summary>
    val defaults: Options

    /// <summary>A compiler error reported for a saved script.</summary>
    type Diagnostic =
        {
            /// <summary>
            /// The path of the reloaded script. An error in a file loaded by the script carries the line and column
            /// within that file.
            /// </summary>
            File: string
            /// <summary>The line of the error, from 1.</summary>
            Line: int
            /// <summary>The column of the error, from 1.</summary>
            Column: int
            /// <summary>The compiler message.</summary>
            Message: string
        }

    /// <summary>The state shown above the pages.</summary>
    type Status =
        /// <summary>The server is listening and the first render has not finished.</summary>
        | Starting
        /// <summary>The pages of the last render, with their count and the render time.</summary>
        | Rendered of pages: int * renderTime: TimeSpan
        /// <summary>The document function raised. The page keeps the last good pages under the error.</summary>
        | RenderFailed of error: string
        /// <summary>
        /// The last save of a watched script failed to compile. The rendered pages come from the last script that
        /// compiled.
        /// </summary>
        | CompileFailed of Diagnostic list
        /// <summary>A save could not be applied. The reason holds the daemon's message or the connection error.</summary>
        | ReloadFailed of reason: string

    /// <summary>A running preview.</summary>
    [<Sealed>]
    type Server =
        interface IDisposable
        /// <summary>The address of the page.</summary>
        member Url: Uri
        /// <summary>The port of the page.</summary>
        member Port: int
        /// <summary>The state shown above the pages.</summary>
        member Status: Status
        /// <summary>
        /// Advisory text shown as a banner, such as a document function that returns the same document on every
        /// call.
        /// </summary>
        member Hint: string option
        /// <summary>Increases on every change of pages, status or hint.</summary>
        member Version: int64
        /// <summary>
        /// Re-renders at once and returns when the render is published. Raises ObjectDisposedException on a stopped
        /// server.
        /// </summary>
        member Refresh: unit -> unit
        /// The published state of the server.
        member internal Snapshot: QuestPDF.FSharp.PreviewServer.Snapshot
        /// The number of open event streams.
        member internal Clients: int
        /// The render loop and routes of the server.
        member internal Engine: QuestPDF.FSharp.PreviewServer.Engine

    /// <summary>
    /// Starts a preview of a document function on <c>options.Port</c> and returns the running server, after the first
    /// render. Calling it again for a running port replaces the document function and the options of that server,
    /// re-renders at once, and returns the same server. Raises InvalidOperationException when the port stays in use
    /// by another process for 15 s.
    /// </summary>
    val serve: options: Options -> document: (unit -> #IDocument) -> Server

    /// <summary>serve with defaults. The first call for a port prints the URL.</summary>
    val show: document: (unit -> #IDocument) -> unit

    /// <summary>show on a port.</summary>
    val showOn: port: int -> document: (unit -> #IDocument) -> unit

    /// <summary>The running server of a port.</summary>
    val tryServer: port: int -> Server option

    /// <summary>
    /// Stops the server of a port and the watchers of its scripts. Open pages reconnect to a server started later on
    /// the same port. A server still waiting for its port is stopped once it has bound it, and the serve that started
    /// it raises ObjectDisposedException.
    /// </summary>
    val stop: port: int -> unit

    /// The server of a port and whether this call started it. A started server has finished its first render.
    val internal start:
        env: QuestPDF.FSharp.PreviewServer.ServeEnv ->
        options: Options ->
        reload: string ->
        reloadHint: string option ->
        document: (unit -> IDocument) ->
            Server * bool

    /// serve with the seams of the tests.
    val internal serveWith:
        env: QuestPDF.FSharp.PreviewServer.ServeEnv -> options: Options -> document: (unit -> #IDocument) -> Server

    /// The banner of a script preview: why saving the script does not reload it.
    val internal liveHint: script: string -> string

    /// Live with the seams of the tests.
    val internal liveWith:
        env: QuestPDF.FSharp.PreviewServer.ServeEnv ->
        options: Options ->
        script: string ->
        document: (unit -> IDocument) ->
            unit

    /// The full paths of the scripts reloaded on save.
    val internal watchedScripts: unit -> string list

/// <summary>The script entry point of the preview.</summary>
[<AbstractClass; Sealed>]
type Preview =
    /// <summary>
    /// Shows a document function on <c>Preview.defaults.Port</c> and reloads the calling script into the SageFs session
    /// on every save of an .fs or .fsx file in the script's directory tree. Idempotent per script: the reload runs
    /// this call again, which replaces the document function. When the script path is unknown (code sent as a
    /// selection) or no SageFs session is available, it shows the document without reloading and states why on the
    /// page.
    /// </summary>
    static member Live:
        document: (unit -> #IDocument) * [<CallerFilePath; Optional; DefaultParameterValue "">] script: string -> unit

    /// <summary>Live with options.</summary>
    static member LiveWith:
        options: Preview.Options *
        document: (unit -> #IDocument) *
        [<CallerFilePath; Optional; DefaultParameterValue "">] script: string ->
            unit
