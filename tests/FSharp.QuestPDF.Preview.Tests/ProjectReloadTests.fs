module FSharp.QuestPDF.Preview.Tests.ProjectReloadTests

open System
open System.Threading
open Expecto
open QuestPDF.Fluent
open QuestPDF.Helpers
open FSharp.QuestPDF
open FSharp.QuestPDF.PreviewServer
open FSharp.QuestPDF.Preview.Tests.Support

/// A one-page A5 document with a line of text.
let private onePage (line: string) : Document =
    document [ page [ Page.size PageSizes.A5; Page.margin 20; Page.content (text line) ] ]

/// Waits up to a number of seconds for a condition, checking every 20 ms.
let private within (seconds: float) (condition: unit -> bool) =
    let deadline = DateTime.UtcNow + TimeSpan.FromSeconds seconds
    let mutable met = condition ()

    while not met && DateTime.UtcNow < deadline do
        Thread.Sleep 20
        met <- condition ()

    met

/// Waits up to 3 s for a condition, checking every 20 ms.
let private eventually (condition: unit -> bool) = within 3.0 condition

/// The session of the fake daemon.
[<Literal>]
let private sessionId = "e9d55efc"

/// The project of the session.
[<Literal>]
let private projectPath = "C:/work/invoice/Invoice.fsproj"

/// The state event SageFs sends when a session patched new code.
let private hotReloadChanged (session: string) =
    $"{{\"hotReloadChanged\":true,\"sessionId\":\"{session}\"}}"

/// A preview in a SageFs project session, served through a fake daemon.
type private Project =
    { Daemon: FakeDaemon
      Port: int
      Env: ServeEnv }

    /// Options on the port with polling off and Reload.Auto.
    member project.Options =
        { quiet project.Port with
            Reload = Preview.Auto }

    /// The running server of the port.
    member project.Server = (Preview.tryServer project.Port).Value

    /// Serves a document function with the options.
    member project.Serve(document: unit -> Document) =
        Preview.serveWith project.Env project.Options document
        |> ignore

    /// The text of page 1.
    member project.Page = (get project.Server "/page/1.svg").Text

/// Runs a test body with a fake daemon whose only session has one project, an environment of that session and a
/// free port, and stops them afterwards.
let private withProject (body: Project -> unit) =
    configure ()
    let daemon = new FakeDaemon ()
    daemon.Sessions <- sessionsJson sessionId "C:/work/invoice" [ projectPath ]
    let port = freePort ()

    let project =
        { Daemon = daemon
          Port = port
          Env =
            { testEnv with
                Daemon = daemon.Uri
                Variable =
                    function
                    | "SAGEFS_DAEMON_PID" -> Some "4242"
                    | "SAGEFS_SESSION_PROJECTS" -> Some projectPath
                    | _ -> None } }

    try
        body project
    finally
        Preview.stop port
        (daemon :> IDisposable).Dispose ()

[<Tests>]
let tests =
    testSequenced
    <| testList
        "ProjectReload"
        [ test "serve in a project session posts watch-all once to the dashboard on the next port" {
              withProject (fun project ->
                  project.Serve (fun () -> onePage "first")
                  project.Serve (fun () -> onePage "second")
                  Expect.isTrue (eventually (fun () -> not project.Daemon.WatchAlls.IsEmpty)) "a watch-all"
                  Thread.Sleep 300

                  Expect.equal project.Daemon.WatchAlls [ $"/api/sessions/{sessionId}/hotreload/watch-all" ] "one watch-all for the session"

                  Expect.equal project.Server.Snapshot.Reload "sagefs-project" "the reload mode")
          }
          test "WatchProjectFiles false posts no watch-all" {
              withProject (fun project ->
                  Preview.serveWith
                      project.Env
                      { project.Options with
                          WatchProjectFiles = false }
                      (fun () -> onePage "first")
                  |> ignore

                  Thread.Sleep 400
                  Expect.isEmpty project.Daemon.WatchAlls "no watch-all")
          }
          test "a hotReloadChanged event of the session re-renders with polling off" {
              withProject (fun project ->
                  let mutable line = "first"
                  project.Serve (fun () -> onePage line)
                  Expect.isTrue (eventually (fun () -> project.Daemon.EventConnections = 1)) "the event stream"
                  line <- "second"
                  project.Daemon.Push ("state", hotReloadChanged sessionId)
                  Expect.isTrue (eventually (fun () -> project.Page.Contains "second")) "the new text")
          }
          test "a fileReloaded event of the session re-renders with polling off" {
              withProject (fun project ->
                  let mutable line = "first"
                  project.Serve (fun () -> onePage line)
                  Expect.isTrue (eventually (fun () -> project.Daemon.EventConnections = 1)) "the event stream"
                  line <- "second"
                  project.Daemon.Push ("state", $"{{\"fileReloaded\":\"C:\\\\work\\\\invoice\\\\Invoice.fs\",\"sessionId\":\"{sessionId}\"}}")
                  Expect.isTrue (eventually (fun () -> project.Page.Contains "second")) "the new text")
          }
          test "events of another session and other state events don't re-render" {
              withProject (fun project ->
                  let mutable line = "first"
                  project.Serve (fun () -> onePage line)
                  Expect.isTrue (eventually (fun () -> project.Daemon.EventConnections = 1)) "the event stream"
                  let version = project.Server.Version
                  line <- "second"
                  project.Daemon.Push ("state", hotReloadChanged "4758ad63")
                  project.Daemon.Push ("state", """{"diagCount":0,"outputCount":13}""")
                  project.Daemon.Push ("session", hotReloadChanged sessionId)
                  Thread.Sleep 800
                  Expect.equal project.Server.Version version "the version"
                  Expect.stringContains project.Page "first" "the old text")
          }
          test "the event reader reconnects after the daemon drops the stream" {
              withProject (fun project ->
                  let mutable line = "first"
                  project.Serve (fun () -> onePage line)
                  Expect.isTrue (eventually (fun () -> project.Daemon.EventConnections = 1)) "the event stream"
                  project.Daemon.DropStreams ()
                  Expect.isTrue (eventually (fun () -> project.Daemon.EventConnections = 2)) "a second stream"
                  line <- "second"
                  project.Daemon.Push ("state", hotReloadChanged sessionId)
                  Expect.isTrue (eventually (fun () -> project.Page.Contains "second")) "the new text")
          }
          test "Preview.stop ends the event reader" {
              withProject (fun project ->
                  project.Serve (fun () -> onePage "first")
                  Expect.isTrue (eventually (fun () -> project.Daemon.EventConnections = 1)) "the event stream"
                  Preview.stop project.Port
                  project.Daemon.DropStreams ()
                  Thread.Sleep 1500
                  Expect.equal project.Daemon.EventConnections 1 "no reconnect")
          }
          test "a session that isn't identified shows ReloadFailed with the reason" {
              withProject (fun project ->
                  project.Daemon.Sessions <- """{"sessions":[]}"""
                  project.Serve (fun () -> onePage "first")

                  match project.Server.Status with
                  | Preview.ReloadFailed reason -> Expect.stringContains reason "SageFs session not identified (0 matches)" "the reason"
                  | status -> failtest $"expected ReloadFailed, got {status}"

                  Expect.isEmpty project.Daemon.WatchAlls "no watch-all")
          }
          test "Auto in a session without projects serves manually and makes no HTTP requests" {
              withProject (fun project ->
                  let env =
                      { project.Env with
                          Variable =
                              function
                              | "SAGEFS_DAEMON_PID" -> Some "4242"
                              | _ -> None }

                  Preview.serveWith env project.Options (fun () -> onePage "first")
                  |> ignore

                  Thread.Sleep 400
                  Expect.equal project.Server.Snapshot.Reload "manual" "manual reload"

                  Expect.equal (project.Daemon.SessionRequests, project.Daemon.EventConnections, project.Daemon.WatchAlls.Length) (0, 0, 0) "no HTTP")
          }
          test "a nudge renders again 250 ms later, for a patch that lands after the event" {
              withProject (fun project ->
                  let mutable patched = false
                  project.Serve (fun () -> onePage (if patched then "second" else "first"))
                  Expect.isTrue (eventually (fun () -> project.Daemon.EventConnections = 1)) "the event stream"
                  use _patch = new Timer ((fun _ -> patched <- true), null, 120, Timeout.Infinite)
                  project.Daemon.Push ("state", hotReloadChanged sessionId)
                  Expect.isTrue (eventually (fun () -> project.Page.Contains "second")) "the late patch")
          }
          test "serve returns once the event stream is open" {
              withProject (fun project ->
                  project.Daemon.EventDelay <- TimeSpan.FromMilliseconds 400.0
                  let mutable line = "first"
                  project.Serve (fun () -> onePage line)
                  line <- "second"
                  project.Daemon.Push ("state", hotReloadChanged sessionId)
                  Expect.isTrue (eventually (fun () -> project.Page.Contains "second")) "a save right after serve")
          }
          test "a later serve on the port outside the project session stops the event reader" {
              withProject (fun project ->
                  let mutable line = "first"
                  project.Serve (fun () -> onePage line)
                  Expect.isTrue (eventually (fun () -> project.Daemon.EventConnections = 1)) "the event stream"

                  Preview.serveWith
                      project.Env
                      { project.Options with
                          Reload = Preview.Manual }
                      (fun () -> onePage line)
                  |> ignore

                  let version = project.Server.Version
                  line <- "second"
                  project.Daemon.Push ("state", hotReloadChanged sessionId)
                  Thread.Sleep 800
                  Expect.equal project.Server.Snapshot.Reload "manual" "the reload mode"
                  Expect.equal project.Server.Version version "the version")
          }
          test "a later serve on the port with another daemon reads the events of that daemon" {
              withProject (fun project ->
                  use other = new FakeDaemon ()
                  other.Sessions <- project.Daemon.Sessions
                  let mutable line = "first"
                  project.Serve (fun () -> onePage line)
                  Expect.isTrue (eventually (fun () -> project.Daemon.EventConnections = 1)) "the first stream"

                  Preview.serveWith
                      project.Env
                      { project.Options with
                          Reload = Preview.SageFs other.Uri }
                      (fun () -> onePage line)
                  |> ignore

                  Expect.isTrue (eventually (fun () -> other.EventConnections = 1)) "the stream of the other daemon"
                  line <- "second"
                  other.Push ("state", hotReloadChanged sessionId)
                  Expect.isTrue (eventually (fun () -> project.Page.Contains "second")) "the new text")
          }
          test "a session found after a failed serve clears ReloadFailed" {
              withProject (fun project ->
                  let sessions = project.Daemon.Sessions
                  project.Daemon.Sessions <- """{"sessions":[]}"""
                  project.Serve (fun () -> onePage "first")
                  project.Daemon.Sessions <- sessions

                  let rendered () =
                      match project.Server.Status with
                      | Preview.Rendered _ -> true
                      | _ -> false

                  Expect.isTrue (within 5.0 rendered) $"Rendered, got {project.Server.Status}")
          }
          test "a session found after a failed serve gets file watching once" {
              withProject (fun project ->
                  let sessions = project.Daemon.Sessions
                  project.Daemon.Sessions <- """{"sessions":[]}"""
                  project.Serve (fun () -> onePage "first")
                  project.Daemon.Sessions <- sessions
                  Expect.isTrue (within 5.0 (fun () -> not project.Daemon.WatchAlls.IsEmpty)) "a watch-all"
                  project.Serve (fun () -> onePage "first")
                  Thread.Sleep 500
                  Expect.equal project.Daemon.WatchAlls [ $"/api/sessions/{sessionId}/hotreload/watch-all" ] "one watch-all")
          }
          test "a serve sent again once the session exists turns on file watching" {
              withProject (fun project ->
                  let sessions = project.Daemon.Sessions
                  project.Daemon.Sessions <- """{"sessions":[]}"""
                  project.Serve (fun () -> onePage "first")
                  project.Daemon.Sessions <- sessions
                  project.Serve (fun () -> onePage "first")
                  Expect.equal project.Daemon.WatchAlls [ $"/api/sessions/{sessionId}/hotreload/watch-all" ] "a watch-all")
          }
          test "disposing a stale server leaves the event reader of the new server on the port" {
              withProject (fun project ->
                  let stale = Preview.serveWith project.Env project.Options (fun () -> onePage "first")
                  Expect.isTrue (eventually (fun () -> project.Daemon.EventConnections = 1)) "the first stream"
                  (stale :> IDisposable).Dispose ()
                  let mutable line = "first"
                  project.Serve (fun () -> onePage line)
                  Expect.isTrue (eventually (fun () -> project.Daemon.EventConnections = 2)) "the second stream"
                  (stale :> IDisposable).Dispose ()
                  line <- "second"
                  project.Daemon.Push ("state", hotReloadChanged sessionId)
                  Expect.isTrue (eventually (fun () -> project.Page.Contains "second")) "the new text")
          }
          test "a stream that closes as soon as it opens is reopened with a growing delay" {
              withProject (fun project ->
                  project.Serve (fun () -> onePage "first")
                  Expect.isTrue (eventually (fun () -> project.Daemon.EventConnections = 1)) "the event stream"
                  let until = DateTime.UtcNow + TimeSpan.FromSeconds 4.0

                  while DateTime.UtcNow < until do
                      project.Daemon.DropStreams ()
                      Thread.Sleep 20

                  Expect.isLessThanOrEqual project.Daemon.EventConnections 5 "connections in 4 s")
          }
          test "a stop during serve leaves no event reader" {
              withProject (fun project ->
                  let env =
                      { project.Env with
                          OpenBrowser = fun _ -> Preview.stop project.Port }

                  Preview.serveWith env { project.Options with OpenBrowser = true } (fun () -> onePage "first")
                  |> ignore

                  Thread.Sleep 1000
                  Expect.isNone (Preview.tryServer project.Port) "the server is stopped"
                  let connections = project.Daemon.EventConnections
                  project.Daemon.DropStreams ()
                  Thread.Sleep 1500
                  Expect.equal project.Daemon.EventConnections connections "no reconnect")
          } ]
