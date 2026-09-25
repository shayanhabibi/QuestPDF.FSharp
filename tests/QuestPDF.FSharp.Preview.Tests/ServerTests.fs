module QuestPDF.FSharp.Preview.Tests.ServerTests

open System
open System.IO
open System.Net
open System.Net.Sockets
open System.Text.RegularExpressions
open System.Threading
open Expecto
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.FSharp
open QuestPDF.FSharp.Preview.Tests.Support

/// A one-page A5 document with a line of text.
let private onePage (line: string) : Document =
    document [ page [ Page.size PageSizes.A5; Page.margin 20; Page.content (text line) ] ]

let private hashes (server: Preview.Server) =
    server.Snapshot.Hashes

/// Waits up to a timeout for a condition, checking every 20 ms.
let private eventually (timeout: TimeSpan) (condition: unit -> bool) =
    let deadline = DateTime.UtcNow + timeout
    let mutable met = condition ()

    while not met && DateTime.UtcNow < deadline do
        Thread.Sleep 20
        met <- condition ()

    met

let private rendering =
    testList
        "render"
        [ test "a text change and Refresh change the hash and the page" {
              configure ()
              let mutable line = "first text"

              withServer (fun () -> onePage line) (fun server ->
                  let before = hashes server
                  Expect.stringContains (get server "/page/1.svg").Text "first text" "the first page"
                  line <- "second text"
                  server.Refresh ()
                  Expect.notEqual (hashes server) before "the hash changes"
                  Expect.stringContains (get server "/page/1.svg").Text "second text" "the new text")
          }
          test "a throwing function gives RenderFailed and keeps the pages" {
              configure ()
              let mutable fail = false

              withServer (fun () -> if fail then failwith "no invoice today" else onePage "good") (fun server ->
                  let good = hashes server
                  fail <- true
                  server.Refresh ()

                  match server.Status with
                  | Preview.RenderFailed error -> Expect.stringContains error "no invoice today" "the message"
                  | status -> failtest $"expected RenderFailed, got {status}"

                  Expect.equal (hashes server) good "the last good pages"
                  fail <- false
                  server.Refresh ()

                  match server.Status with
                  | Preview.Rendered (1, _) -> ()
                  | status -> failtest $"expected Rendered, got {status}")
          }
          test "the same document object twice gives the frozen hint" {
              configure ()
              let frozen = onePage "frozen"

              withServer (fun () -> frozen) (fun server ->
                  server.Refresh ()
                  Expect.isSome server.Hint "the hint"
                  Expect.stringContains server.Hint.Value "same document" "the text")
          }
          test "Refresh on a stopped server raises ObjectDisposedException" {
              configure ()
              let server = serveFree (fun () -> onePage "stopped")
              Preview.stop server.Port
              Expect.throwsT<ObjectDisposedException> server.Refresh "Refresh"
          }
          testSequenced
          <| test "a missing licence gives RenderFailed naming License.community" {
              configure ()
              let license = QuestPDF.Settings.License
              QuestPDF.Settings.License <- Nullable ()

              try
                  withServer (fun () -> onePage "licence") (fun server ->
                      match server.Status with
                      | Preview.RenderFailed error -> Expect.stringContains error "License.community" "the message"
                      | status -> failtest $"expected RenderFailed, got {status}")
              finally
                  QuestPDF.Settings.License <- license
          } ]

let private http =
    testList
        "http"
        [ test "the routes return their content types" {
              configure ()

              withServer (fun () -> onePage "routes") (fun server ->
                  for path, contentType in
                      [ "/", "text/html"
                        "/snapshot", "application/json"
                        "/page/1.svg", "image/svg+xml"
                        "/font/0", "font/ttf"
                        "/document.pdf", "application/pdf" ] do
                      let reply = get server path
                      Expect.equal (reply.Status, reply.ContentType) (200, contentType) path

                  Expect.equal (get server "/page/2.svg").Status 404 "a missing page"
                  Expect.equal (get server "/nothing").Status 404 "an unknown route")
          }
          test "the page SVG carries @font-face rules for Lato" {
              configure ()

              withServer (fun () -> onePage "fonts") (fun server ->
                  let svg = (get server $"/page/1.svg?h={(hashes server)[0]}")
                  Expect.stringContains svg.Text "@font-face" "the rules"
                  Expect.stringContains svg.Text "font-family=\"Lato\"" "the text family"
                  Expect.stringContains svg.CacheControl "immutable" "hashed pages are immutable"
                  Expect.stringStarts (Text.Encoding.ASCII.GetString (get server "/document.pdf").Body) "%PDF" "the PDF")
          }
          test "an SSE reader receives a version after a change and none after an identical render" {
              configure ()
              let mutable line = "before"

              withServer (fun () -> onePage line) (fun server ->
                  use events = new EventStream (server)
                  Expect.equal events.ContentType "text/event-stream" "content type"
                  Expect.equal (events.Next (TimeSpan.FromSeconds 2.0)) (Some "version") "the version on connect"
                  line <- "after"
                  let started = DateTime.UtcNow
                  server.Refresh ()
                  Expect.equal (events.Next (TimeSpan.FromMilliseconds 500.0)) (Some "version") "a version after the change"
                  Expect.isLessThan (DateTime.UtcNow - started) (TimeSpan.FromMilliseconds 500.0) "within 500 ms"
                  server.Refresh ()
                  Expect.equal (events.Next (TimeSpan.FromMilliseconds 300.0)) None "no version for identical output")
          }
          test "a second serve returns the same server and renders the new function" {
              configure ()

              withServer (fun () -> onePage "one") (fun server ->
                  let again = Preview.serveWith testEnv (quiet server.Port) (fun () -> onePage "two")
                  Expect.isTrue (obj.ReferenceEquals (server, again)) "the same server"
                  Expect.stringContains (get server "/page/1.svg").Text "two" "the new function")
          }
          test "serve waits for a port released within the bind timeout" {
              configure ()
              let port = freePort ()
              let holder = new TcpListener (IPAddress.Loopback, port)
              holder.Start ()

              let release = new Timer ((fun _ -> holder.Stop ()), null, 1000, Timeout.Infinite)

              try
                  let server = Preview.serveWith testEnv (quiet port) (fun () -> onePage "late")

                  try
                      Expect.equal server.Port port "the held port"
                  finally
                      Preview.stop port
              finally
                  release.Dispose ()
                  holder.Stop ()
          }
          test "serve raises naming the port when it stays in use" {
              configure ()
              let port = freePort ()
              let holder = new TcpListener (IPAddress.Loopback, port)
              holder.Start ()

              try
                  let error =
                      Expect.throwsC
                          (fun () ->
                              Preview.serveWith testEnv (quiet port) (fun () -> onePage "never")
                              |> ignore)
                          id

                  Expect.isTrue (error :? InvalidOperationException) $"InvalidOperationException, got {error.GetType().Name}"
                  Expect.stringContains error.Message (string port) "the port"
                  Expect.isNone (Preview.tryServer port) "no server"
              finally
                  holder.Stop ()
          }
          test "a disconnected client is pruned at the next keep-alive" {
              configure ()

              withServer (fun () -> onePage "clients") (fun server ->
                  let events = new EventStream (server)
                  Expect.equal (events.Next (TimeSpan.FromSeconds 2.0)) (Some "version") "connected"
                  Expect.equal server.Clients 1 "one client"
                  (events :> IDisposable).Dispose ()
                  Expect.isTrue (eventually (TimeSpan.FromSeconds 3.0) (fun () -> server.Clients = 0)) "pruned")
          }
          test "a poll renders a change while a page is open" {
              configure ()
              let mutable line = "polled before"
              let port = freePort ()

              let server =
                  Preview.serveWith
                      testEnv
                      { quiet port with
                          Poll = Some (TimeSpan.FromMilliseconds 50.0) }
                      (fun () -> onePage line)

              try
                  use events = new EventStream (server)
                  Expect.equal (events.Next (TimeSpan.FromSeconds 2.0)) (Some "version") "the version on connect"
                  line <- "polled after"
                  Expect.equal (events.Next (TimeSpan.FromSeconds 2.0)) (Some "version") "a version without Refresh"
                  Expect.stringContains (get server "/page/1.svg").Text "polled after" "the new text"
              finally
                  Preview.stop port
          }
          test "Live with an unknown script shows the document and says how to reload" {
              configure ()
              let port = freePort ()
              let script = Path.Combine (Path.GetTempPath (), $"{Guid.NewGuid ():N}", "input.fsx")

              try
                  Preview.LiveWith (
                      { quiet port with
                          Reload = Preview.Auto },
                      (fun () -> onePage "live"),
                      script
                  )

                  let server = (Preview.tryServer port).Value

                  match server.Status, server.Hint with
                  | Preview.Rendered (1, _), Some hint -> Expect.stringContains hint "#load" "the hint"
                  | status, hint -> failtest $"expected Rendered with a hint, got {status}, {hint}"

                  Expect.equal server.Snapshot.Reload "manual" "the reload mode"
              finally
                  Preview.stop port
          }
          test "a font is cached only under the hash of its contents" {
              configure ()

              withServer (fun () -> onePage "font cache") (fun server ->
                  let svg = (get server "/page/1.svg").Text
                  let found = Regex.Match (svg, @"url\((/font/0\?h=[0-9a-f]+)\)")
                  Expect.isTrue found.Success "the page names a hashed font URL"
                  let hashed = get server found.Groups[1].Value
                  Expect.equal hashed.Status 200 "the hashed URL"
                  Expect.stringContains hashed.CacheControl "immutable" "the hashed URL is immutable"
                  Expect.equal (get server "/font/0").CacheControl "no-store" "a URL without the hash"
                  Expect.equal (get server "/font/0?h=0000000000000000").CacheControl "no-store" "a URL with another hash")
          }
          test "a closed page is pruned at the next poll, before the keep-alive" {
              configure ()
              let port = freePort ()

              let server =
                  Preview.serveWith
                      { testEnv with
                          KeepAlive = TimeSpan.FromHours 1.0 }
                      { quiet port with
                          Poll = Some (TimeSpan.FromMilliseconds 50.0) }
                      (fun () -> onePage "closed")

              try
                  let events = new EventStream (server)
                  Expect.equal (events.Next (TimeSpan.FromSeconds 2.0)) (Some "version") "connected"
                  (events :> IDisposable).Dispose ()
                  Expect.isTrue (eventually (TimeSpan.FromSeconds 5.0) (fun () -> server.Clients = 0)) "pruned within 5 s"
              finally
                  Preview.stop port
          }
          test "stop during a bind retry frees the port and leaves no server" {
              configure ()
              let port = freePort ()
              let holder = new TcpListener (IPAddress.Loopback, port)
              holder.Start ()
              let release = new Timer ((fun _ -> holder.Stop ()), null, 800, Timeout.Infinite)
              let mutable outcome: Result<Preview.Server, exn> option = None

              let serving =
                  Thread (fun () ->
                      outcome <-
                          try
                              Some (Ok (Preview.serveWith testEnv (quiet port) (fun () -> onePage "raced")))
                          with error ->
                              Some (Error error))

              try
                  serving.Start ()
                  Thread.Sleep 300
                  Preview.stop port
                  Expect.isTrue (serving.Join (TimeSpan.FromSeconds 5.0)) "serve returns"
                  Expect.isNone (Preview.tryServer port) "no server"

                  match outcome with
                  | Some (Error (:? ObjectDisposedException)) -> ()
                  | other -> failtest $"expected serve to raise ObjectDisposedException, got {other}"

                  let listener = new HttpListener ()
                  listener.Prefixes.Add $"http://localhost:{port}/"

                  try
                      listener.Start ()
                  finally
                      listener.Close ()
              finally
                  release.Dispose ()
                  holder.Stop ()
                  Preview.stop port
          }
          test "the shell ignores a version older than the one shown" {
              configure ()

              withServer (fun () -> onePage "shell") (fun server ->
                  Expect.stringContains (get server "/").Text "snap.version <= shown" "apply skips older versions")
          }
          test "stop frees the port and forgets the server" {
              configure ()
              let server = serveFree (fun () -> onePage "stop")
              let port = server.Port
              Expect.isSome (Preview.tryServer port) "running"
              Preview.stop port
              Expect.isNone (Preview.tryServer port) "forgotten"
              let listener = new HttpListener ()
              listener.Prefixes.Add $"http://localhost:{port}/"

              try
                  listener.Start ()
              finally
                  listener.Close ()
          } ]

[<Tests>]
let tests = testList "Server" [ rendering; http ]
