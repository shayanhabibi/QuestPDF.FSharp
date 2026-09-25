/// End-to-end checks against a running SageFs daemon on port 37749: a script reloaded on save, and a project patched
/// by Hot Reload. Skipped unless QPDF_SAGEFS_E2E=1 and the daemon answers.
module QuestPDF.FSharp.Preview.Tests.SageFsEndToEndTests

open System
open System.Diagnostics
open System.IO
open System.Net.Http
open System.Text
open System.Text.Json
open System.Threading
open Expecto
open QuestPDF.FSharp
open QuestPDF.FSharp.PreviewServer
open QuestPDF.FSharp.Preview.Tests.Support

/// The daemon of the checks.
let private daemon = Uri "http://localhost:37749/"

let private http = new HttpClient (Timeout = TimeSpan.FromMinutes 5.0)

/// The repository root.
let private repository =
    Path.GetFullPath (Path.Combine (__SOURCE_DIRECTORY__, "..", ".."))

/// The sample of the recipes.
let private sample = Path.Combine (repository, "samples", "HotReloadPreview")

/// Whether the checks run: QPDF_SAGEFS_E2E is 1 and GET /api/sessions answers within 1 s.
let private enabled =
    lazy
        (Environment.GetEnvironmentVariable "QPDF_SAGEFS_E2E" = "1"
         && (try
                 let cancel = new CancellationTokenSource (TimeSpan.FromSeconds 1.0)

                 try
                     let response =
                         http.GetAsync(Uri (daemon, "api/sessions"), cancel.Token).GetAwaiter().GetResult ()

                     try
                         response.IsSuccessStatusCode
                     finally
                         response.Dispose ()
                 finally
                     cancel.Dispose ()
             with _ ->
                 false))

/// A check that is skipped unless the end-to-end checks are enabled.
let private e2e (name: string) (body: unit -> unit) =
    test name {
        if not enabled.Value then
            skiptest "Set QPDF_SAGEFS_E2E=1 and start the SageFs daemon on port 37749 to run the SageFs checks."

        body ()
    }

/// A path in the form SageFs reports working directories: forward slashes, no trailing separator, lower case.
let private comparable (path: string) =
    path.Replace('\\', '/').TrimEnd('/').ToLowerInvariant ()

/// The sessions of the daemon as (id, working directory, status).
let private sessions () : (string * string * string) list =
    let body =
        http.GetStringAsync(Uri (daemon, "api/sessions")).GetAwaiter().GetResult ()

    let document = JsonDocument.Parse body

    try
        [ for session in document.RootElement.GetProperty("sessions").EnumerateArray () do
              session.GetProperty("id").GetString (), session.GetProperty("workingDirectory").GetString (), session.GetProperty("status").GetString () ]
    finally
        document.Dispose ()

/// A client of the daemon's MCP endpoint.
type private Mcp() =
    let post (session: string option) (body: string) : HttpResponseMessage =
        let request = new HttpRequestMessage (HttpMethod.Post, daemon)
        request.Content <- new StringContent (body, Encoding.UTF8, "application/json")
        request.Headers.Accept.ParseAdd "application/json, text/event-stream"

        session
        |> Option.iter (fun id -> request.Headers.Add ("Mcp-Session-Id", id))

        http.SendAsync(request).GetAwaiter().GetResult ()

    /// The JSON-RPC message of a reply, sent as JSON or as the last data line of an event stream.
    let message (response: HttpResponseMessage) : JsonDocument =
        let text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult ()

        let json =
            if text.TrimStart().StartsWith "{" then
                text
            else
                text.Split '\n'
                |> Array.map _.TrimEnd('\r')
                |> Array.filter _.StartsWith("data: ")
                |> Array.last
                |> _.Substring(6)

        JsonDocument.Parse json

    let session =
        let initialize =
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"qpdf-preview-e2e","version":"1"}}}"""

        let response = post None initialize

        let id =
            response.Headers.GetValues "Mcp-Session-Id"
            |> Seq.head

        response.Dispose ()

        (post (Some id) """{"jsonrpc":"2.0","method":"notifications/initialized"}""").Dispose ()

        id

    /// Calls a tool and returns its text, raising when the tool reports an error.
    member _.Call(tool: string, arguments: obj) : string =
        let body =
            JsonSerializer.Serialize
                {| jsonrpc = "2.0"
                   id = 2
                   ``method`` = "tools/call"
                   ``params`` = {| name = tool; arguments = arguments |} |}

        let response = post (Some session) body

        try
            let document = message response

            try
                let result = document.RootElement.GetProperty "result"

                let text =
                    [ for content in result.GetProperty("content").EnumerateArray () do
                          match content.TryGetProperty "text" with
                          | true, text -> text.GetString ()
                          | _ -> () ]
                    |> String.concat "\n"

                match result.TryGetProperty "isError" with
                | true, error when error.ValueKind = JsonValueKind.True -> failwith $"{tool} failed: {text}"
                | _ -> text
            finally
                document.Dispose ()
        finally
            response.Dispose ()

/// Waits up to a number of seconds for a condition, checking every 50 ms, and returns the time it took.
let private waitFor (seconds: float) (what: string) (condition: unit -> bool) : TimeSpan =
    let clock = Stopwatch.StartNew ()
    let mutable met = false

    while not met && clock.Elapsed.TotalSeconds < seconds do
        met <-
            try
                condition ()
            with _ ->
                false

        if not met then
            Thread.Sleep 50

    if not met then
        failtest $"{what} within {seconds} s"

    clock.Elapsed

/// Runs a check in a new SageFs session on a directory, and stops the session afterwards.
let private inSession (directory: string) (projects: string) (workflow: string) (body: string -> unit) =
    let taken =
        sessions ()
        |> List.exists (fun (_, workingDirectory, _) -> comparable workingDirectory = comparable directory)

    if taken then
        failtest $"A SageFs session already has the working directory {directory}"

    let mcp = Mcp ()

    mcp.Call (
        "create_session",
        {| working_directory = directory
           projects = projects
           workflow = workflow
           agentName = "qpdf-preview-e2e" |}
    )
    |> ignore

    let session () =
        sessions ()
        |> List.tryFind (fun (_, workingDirectory, _) -> comparable workingDirectory = comparable directory)

    try
        waitFor 180.0 "the session is Ready" (fun () ->
            match session () with
            | Some (_, _, "Ready") -> true
            | Some (_, _, "Faulted") -> failtest "the session faulted"
            | _ -> false)
        |> ignore

        let id, _, _ = (session ()).Value
        printfn "SageFs e2e: session %s (%s) on %s" id workflow directory
        body directory
    finally
        match session () with
        | Some (id, _, _) ->
            mcp.Call ("stop_session", {| session_id = id |})
            |> ignore
        | None -> ()

/// Evaluates code in the session of a working directory through POST /exec and returns the parsed outcome.
let private exec (directory: string) (script: string) (code: string) : ReloadOutcome =
    let body =
        JsonSerializer.Serialize
            {| code = code
               working_directory = directory |}

    let response =
        http.PostAsync(Uri (daemon, "exec"), new StringContent (body, Encoding.UTF8, "application/json")).GetAwaiter().GetResult ()

    try
        ExecProtocol.parse script (int response.StatusCode) (response.Content.ReadAsStringAsync().GetAwaiter().GetResult ())
    finally
        response.Dispose ()

/// A preview page of a port.
type private Page(port: int) =
    let get (path: string) =
        http.GetStringAsync(Uri ($"http://localhost:{port}{path}")).GetAwaiter().GetResult ()

    /// The status kind of /snapshot.
    member _.Kind =
        let document = JsonDocument.Parse (get "/snapshot")

        try
            document.RootElement.GetProperty("status").GetProperty("kind").GetString ()
        finally
            document.Dispose ()

    /// The /snapshot document.
    member _.Snapshot = get "/snapshot"

    /// The SVG of page 1.
    member _.Svg = get "/page/1.svg"

/// Copies a directory tree.
let rec private copyTree (source: string) (target: string) =
    Directory.CreateDirectory target |> ignore

    for file in Directory.GetFiles source do
        File.Copy (file, Path.Combine (target, Path.GetFileName file))

    for directory in Directory.GetDirectories source do
        let name = Path.GetFileName directory

        if name <> "bin" && name <> "obj" then
            copyTree directory (Path.Combine (target, name))

/// Replaces a text in a file, failing when the text is missing.
let private rewrite (path: string) (before: string) (after: string) =
    let content = File.ReadAllText path

    if not (content.Contains before) then
        failtest $"{Path.GetFileName path} has no {before}"

    File.WriteAllText (path, content.Replace (before, after))

/// Deletes a directory, ignoring files still in use.
let private tryDelete (directory: string) =
    try
        Directory.Delete (directory, true)
    with _ ->
        ()

[<Tests>]
let tests =
    testSequenced
    <| testList
        "SageFs"
        [ e2e "a saved script reloads through the daemon: an edit, a compile error, its fix and an edit of a loaded file" (fun () ->
              let directory = tempDirectory ()
              let port = freePort ()

              try
                  // The sample script and the files it loads, with the built assemblies and the repository fonts.
                  let script = Path.Combine (directory, "invoice.fsx")
                  File.Copy (Path.Combine (sample, "invoice.fsx"), script)
                  copyTree (Path.Combine (sample, "parts")) (Path.Combine (directory, "parts"))
                  let lib = Path.Combine (directory, "lib")
                  let built = Path.GetDirectoryName typeof<Preview>.Assembly.Location
                  Directory.CreateDirectory lib |> ignore

                  for name in [ "QuestPDF.FSharp.dll"; "QuestPDF.FSharp.Preview.dll" ] do
                      File.Copy (Path.Combine (built, name), Path.Combine (lib, name), true)

                  copyTree (Path.Combine (repository, "fonts")) (Path.Combine (directory, "fonts"))
                  File.Copy (Path.Combine (repository, "global.json"), Path.Combine (directory, "global.json"))
                  let libPath = lib.Replace ('\\', '/')
                  rewrite script "\"../../src/QuestPDF.FSharp.Preview/bin/Release/net10.0/" $"\"{libPath}/"
                  rewrite script "\"../../fonts\"" "\"fonts\""
                  rewrite script "Preview.Live invoice" $"Preview.LiveWith ({{ Preview.defaults with Port = {port}; Poll = None }}, invoice)"

                  inSession directory "" "interactive" (fun directory ->
                      let page = Page port

                      match exec directory script $"#load @\"{script}\"" with
                      | Loaded -> ()
                      | outcome -> failtest $"the first #load: {outcome}"

                      Expect.equal page.Kind "rendered" $"the first render: {page.Snapshot}"
                      Expect.stringContains page.Snapshot "\"reload\":\"sagefs-script\"" "reloaded through SageFs"

                      let save (before: string) (after: string) =
                          rewrite script before after

                      save "text \"Thank you for your business.\"" "text \"Edit one\""
                      let edit = waitFor 3.0 "the edit" (fun () -> page.Svg.Contains "Edit one")
                      save "text \"Edit one\"" "text 42"
                      let broken = waitFor 3.0 "the compile error" (fun () -> page.Kind = "compileFailed")
                      Expect.stringContains page.Snapshot "invoice.fsx" "the diagnostic file"
                      save "text 42" "text \"Edit two\""

                      let fixedTime =
                          waitFor 3.0 "the fix" (fun () ->
                              page.Kind = "rendered"
                              && page.Svg.Contains "Edit two")

                      let headerFile = Path.Combine (directory, "parts", "header.fsx")
                      rewrite headerFile "$\"Invoice #{number}\"" "$\"Bill #{number}\""
                      let header = waitFor 3.0 "the header edit" (fun () -> page.Svg.Contains "Bill #1")

                      rewrite headerFile "$\"Bill #{number}\"" "(number + 0)"

                      let loadedBroken =
                          waitFor 6.0 "the loaded file's compile error" (fun () -> page.Kind = "compileFailed")

                      let snapshot = page.Snapshot
                      Expect.stringContains snapshot "header.fsx" $"the diagnostic names the loaded file: {snapshot}"
                      Expect.isFalse (snapshot.Contains "invoice.fsx") $"the diagnostic does not name the script: {snapshot}"
                      rewrite headerFile "(number + 0)" "$\"Bill #{number}\""

                      waitFor 6.0 "the loaded file's fix" (fun () -> page.Kind = "rendered")
                      |> ignore

                      printfn
                          "SageFs e2e script latencies: edit %.0f ms, compile error %.0f ms, fix %.0f ms, loaded file %.0f ms, loaded file error %.0f ms"
                          edit.TotalMilliseconds
                          broken.TotalMilliseconds
                          fixedTime.TotalMilliseconds
                          header.TotalMilliseconds
                          loadedBroken.TotalMilliseconds)
              finally
                  tryDelete directory)
          e2e "a saved project file patches the document function of a Hot Reload session" (fun () ->
              let directory = tempDirectory ()
              let port = freePort ()

              try
                  copyTree sample directory
                  File.Copy (Path.Combine (repository, "global.json"), Path.Combine (directory, "global.json"))
                  let project = Path.Combine (directory, "HotReloadPreview.fsproj")
                  let src = Path.Combine(repository, "src").Replace ('\\', '/')
                  let fonts = Path.Combine(repository, "fonts").Replace ('\\', '/')
                  rewrite project "..\\..\\src\\QuestPDF.FSharp\\" $"{src}/QuestPDF.FSharp/"
                  rewrite project "..\\..\\src\\QuestPDF.FSharp.Preview\\" $"{src}/QuestPDF.FSharp.Preview/"
                  let preview = Path.Combine (directory, "preview.fsx")
                  rewrite preview "\"../../fonts\"" $"\"{fonts}\""

                  rewrite
                      preview
                      "Preview.show Invoice.build"
                      $"Preview.serve {{ Preview.defaults with Port = {port}; Poll = None }} Invoice.build |> ignore"

                  let build =
                      Process.Start (ProcessStartInfo ("dotnet", $"build \"{project}\" -v q", WorkingDirectory = directory))

                  build.WaitForExit ()
                  Expect.equal build.ExitCode 0 "dotnet build of the sample copy"

                  inSession directory (project.Replace ('\\', '/')) "live" (fun directory ->
                      let page = Page port

                      match exec directory preview $"#load @\"{preview}\"" with
                      | Loaded -> ()
                      | outcome -> failtest $"the #load of preview.fsx: {outcome}"

                      Expect.equal page.Kind "rendered" $"the first render: {page.Snapshot}"
                      Expect.stringContains page.Snapshot "\"reload\":\"sagefs-project\"" "the project reload mode"
                      rewrite (Path.Combine (directory, "Invoice.fs")) "\"Thank you for your business.\"" "\"Patched body\""

                      let patched =
                          waitFor 3.0 "the patched body" (fun () -> page.Svg.Contains "Patched body")

                      printfn "SageFs e2e project latency: body edit %.0f ms" patched.TotalMilliseconds)
              finally
                  tryDelete directory) ]
