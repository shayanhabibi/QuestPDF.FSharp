module QuestPDF.FSharp.Preview.Tests.ScriptReloadTests

open System
open System.IO
open System.Text.Json
open System.Threading
open Expecto
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.FSharp
open QuestPDF.FSharp.Preview.Tests.Support

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

/// Waits up to 3 s for a condition.
let private eventually (condition: unit -> bool) =
    within 3.0 condition

/// The code of a POST /exec body.
let private code (body: string) =
    let document = JsonDocument.Parse body

    try
        document.RootElement.GetProperty("code").GetString ()
    finally
        document.Dispose ()

/// A live preview in a temporary directory, reloaded through a fake daemon whose only session has that directory as
/// its working directory.
type private Live =
    { Daemon: FakeDaemon
      Directory: string
      Script: string
      Port: int
      Env: QuestPDF.FSharp.PreviewServer.ServeEnv }

    /// Options on the port with no polling and reloads through the fake daemon.
    member live.Options =
        { quiet live.Port with
            Reload = Preview.SageFs live.Daemon.Uri }

    /// The running server of the port.
    member live.Server = (Preview.tryServer live.Port).Value

    /// Runs Live for the script, as a reload of the script would.
    member live.Run(line: string) =
        Preview.liveWith live.Env live.Options live.Script (fun () -> onePage line)

    /// Saves the script in place.
    member live.Save(content: string) =
        File.WriteAllText (live.Script, content)

    /// Waits for a number of /exec requests, then 400 ms more, and returns the requests.
    member live.Execs(expected: int) =
        eventually (fun () -> live.Daemon.Execs.Length >= expected)
        |> ignore

        Thread.Sleep 400
        live.Daemon.Execs

/// Runs a test body with a fake daemon, a temporary directory holding invoice.fsx and a free port, and stops them
/// afterwards. Live is not called.
let private withLive (body: Live -> unit) =
    configure ()
    let directory = tempDirectory ()
    let script = Path.Combine (directory, "invoice.fsx")
    File.WriteAllText (script, "// version 1")
    let daemon = new FakeDaemon ()
    daemon.Sessions <- sessionsJson "7ccbc644" directory []
    let port = freePort ()

    let live =
        { Daemon = daemon
          Directory = directory
          Script = script
          Port = port
          Env =
            { testEnv with
                Daemon = daemon.Uri
                CurrentDirectory = fun () -> directory } }

    try
        body live
    finally
        Preview.stop port
        (daemon :> IDisposable).Dispose ()

        try
            Directory.Delete (directory, true)
        with _ ->
            ()

let private watchers (script: string) =
    Preview.watchedScripts ()
    |> List.filter (fun path -> String.Equals (path, Path.GetFullPath script, StringComparison.OrdinalIgnoreCase))
    |> List.length

[<Tests>]
let tests =
    testSequenced
    <| testList
        "ScriptReload"
        [ test "a save in place sends exactly one /exec that loads the full script path" {
              withLive (fun live ->
                  live.Run "first"
                  Expect.equal live.Server.Snapshot.Reload "sagefs-script" "the reload mode"
                  Expect.isNone live.Server.Hint "no hint while reloading works"
                  live.Save "// version 2"
                  let execs = live.Execs 1
                  Expect.equal execs.Length 1 "one /exec"
                  Expect.stringStarts (code execs[0]) $"#load @\"{live.Script}\"" "the full path")
          }
          test "a JetBrains safe write through a temporary file and renames sends exactly one /exec" {
              withLive (fun live ->
                  live.Run "first"
                  let temporary = live.Script + "___jb_tmp___"
                  let old = live.Script + "___jb_old___"
                  File.WriteAllText (temporary, "// version 2")
                  File.Move (live.Script, old)
                  File.Move (temporary, live.Script)
                  File.Delete old
                  Expect.equal (live.Execs 1).Length 1 "one /exec")
          }
          test "a compile error shows CompileFailed at the script position and keeps the pages" {
              withLive (fun live ->
                  live.Run "first"
                  let pages = live.Server.Snapshot.Hashes
                  live.Daemon.Exec <- 200, fixture "exec-compile-error.json"
                  live.Save "// broken"

                  let failed () =
                      match live.Server.Status with
                      | Preview.CompileFailed _ -> true
                      | _ -> false

                  Expect.isTrue (eventually failed) $"CompileFailed, got {live.Server.Status}"

                  match live.Server.Status with
                  | Preview.CompileFailed [ d ] -> Expect.equal (d.File, d.Line, d.Column) (live.Script, 17, 54) "the position"
                  | status -> failtest $"expected one diagnostic, got {status}"

                  Expect.equal live.Server.Snapshot.Hashes pages "the pages")
          }
          test "a successful reload clears the compile error and the rerun script bumps the version" {
              withLive (fun live ->
                  live.Run "first"
                  live.Daemon.Exec <- 200, fixture "exec-compile-error.json"
                  live.Save "// broken"

                  let compileFailed () =
                      match live.Server.Status with
                      | Preview.CompileFailed _ -> true
                      | _ -> false

                  Expect.isTrue (eventually compileFailed) $"CompileFailed, got {live.Server.Status}"
                  let failedVersion = live.Server.Version
                  live.Daemon.Exec <- 200, fixture "exec-success.json"
                  live.Save "// fixed"
                  Expect.isTrue (eventually (compileFailed >> not)) $"cleared, got {live.Server.Status}"
                  live.Run "second"

                  match live.Server.Status with
                  | Preview.Rendered (1, _) -> ()
                  | status -> failtest $"expected Rendered, got {status}"

                  Expect.isGreaterThan live.Server.Version failedVersion "the version bumps"
                  Expect.stringContains (get live.Server "/page/1.svg").Text "second" "the new text")
          }
          test "a stopped daemon gives ReloadFailed with the connection error, and renders continue" {
              withLive (fun live ->
                  let mutable line = "first"
                  Preview.liveWith live.Env live.Options live.Script (fun () -> onePage line)
                  live.Daemon.Stop ()
                  live.Save "// version 2"

                  let failed () =
                      match live.Server.Status with
                      | Preview.ReloadFailed _ -> true
                      | _ -> false

                  // Windows retries a refused connection for about 2 s per address of localhost.
                  Expect.isTrue (within 15.0 failed) $"ReloadFailed, got {live.Server.Status}"

                  match live.Server.Status with
                  | Preview.ReloadFailed reason -> Expect.stringContains reason "unreachable" "the connection error"
                  | _ -> ()

                  let before = live.Server.Snapshot.Hashes
                  line <- "second"
                  live.Server.Refresh ()
                  Expect.notEqual live.Server.Snapshot.Hashes before "a new render"
                  Expect.isTrue (failed ()) "the reload failure stays")
          }
          test "a script path that doesn't exist starts no watcher and hints to load the file" {
              withLive (fun live ->
                  let snippet = Path.Combine (live.Directory, "input.fsx")
                  Preview.liveWith live.Env live.Options snippet (fun () -> onePage "snippet")
                  Expect.equal (watchers snippet) 0 "no watcher"
                  Expect.equal live.Server.Snapshot.Reload "manual" "manual reload"
                  Expect.isSome live.Server.Hint "a hint"
                  Expect.stringContains live.Server.Hint.Value "#load" "load the file"
                  live.Save "// version 2"
                  Thread.Sleep 400
                  Expect.equal (live.Daemon.SessionRequests, live.Daemon.Execs.Length) (0, 0) "no HTTP")
          }
          test "Live twice for one script keeps one watcher and one /exec per save" {
              withLive (fun live ->
                  live.Run "first"
                  live.Run "second"
                  Expect.equal (watchers live.Script) 1 "one watcher"
                  live.Save "// version 2"
                  Expect.equal (live.Execs 1).Length 1 "one /exec")
          }
          test "Preview.stop removes the watchers of the port" {
              withLive (fun live ->
                  live.Run "first"
                  Preview.stop live.Port
                  Expect.equal (watchers live.Script) 0 "no watcher"
                  live.Save "// version 2"
                  Thread.Sleep 400
                  Expect.isEmpty live.Daemon.Execs "no /exec")
          }
          test "Auto without SAGEFS_DAEMON_PID is manual and makes no HTTP requests" {
              withLive (fun live ->
                  Preview.liveWith
                      live.Env
                      { live.Options with
                          Reload = Preview.Auto }
                      live.Script
                      (fun () -> onePage "auto")

                  Expect.equal live.Server.Snapshot.Reload "manual" "manual reload"
                  live.Save "// version 2"
                  Thread.Sleep 400
                  Expect.equal (live.Daemon.SessionRequests, live.Daemon.Execs.Length) (0, 0) "no HTTP")
          }
          test "Auto with SAGEFS_DAEMON_PID reloads through the daemon of the environment" {
              withLive (fun live ->
                  let env =
                      { live.Env with
                          Variable =
                              function
                              | "SAGEFS_DAEMON_PID" -> Some "4242"
                              | _ -> None }

                  Preview.liveWith
                      env
                      { live.Options with
                          Reload = Preview.Auto }
                      live.Script
                      (fun () -> onePage "auto")

                  Expect.equal live.Server.Snapshot.Reload "sagefs-script" "SageFs reload"
                  live.Save "// version 2"
                  Expect.equal (live.Execs 1).Length 1 "one /exec")
          } ]
