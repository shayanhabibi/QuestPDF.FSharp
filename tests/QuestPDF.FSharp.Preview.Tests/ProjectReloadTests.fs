module QuestPDF.FSharp.Preview.Tests.ProjectReloadTests

open System
open System.Threading
open Expecto
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.FSharp
open QuestPDF.FSharp.PreviewServer
open QuestPDF.FSharp.Preview.Tests.Support

/// A one-page A5 document with a line of text.
let private onePage (line: string) : Document =
    document [ page [ Page.size PageSizes.A5; Page.margin 20; Page.content (text line) ] ]

/// Waits up to 3 s for a condition, checking every 20 ms.
let private eventually (condition: unit -> bool) =
    let deadline = DateTime.UtcNow + TimeSpan.FromSeconds 3.0
    let mutable met = condition ()

    while not met && DateTime.UtcNow < deadline do
        Thread.Sleep 20
        met <- condition ()

    met

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
          } ]
