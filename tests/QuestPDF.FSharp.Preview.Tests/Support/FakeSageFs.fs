/// Golden SageFs replies and a fake daemon that serves them.
[<AutoOpen>]
module QuestPDF.FSharp.Preview.Tests.Support.FakeSageFs

open System
open System.Collections.Concurrent
open System.IO
open System.Net
open System.Text
open System.Threading

/// A reply recorded from SageFs 0.6.828, by file name, from the resources of the test assembly.
let fixture (name: string) : string =
    let assembly = typeof<Reply>.Assembly
    let stream = assembly.GetManifestResourceStream $"sagefs-0.6.828/{name}"

    if isNull stream then
        failwith $"No fixture sagefs-0.6.828/{name}"

    let reader = new StreamReader (stream)

    try
        reader.ReadToEnd ()
    finally
        reader.Dispose ()

/// The /api/sessions body of one session.
let sessionsJson (id: string) (workingDirectory: string) (projects: string list) : string =
    let session =
        {| id = id
           workingDirectory = workingDirectory
           projects = projects
           status = "Ready" |}

    Json.JsonSerializer.Serialize {| sessions = [ session ] |}

/// A SageFs daemon on a loopback port: GET /api/sessions answers Sessions, and POST /exec records the body and
/// answers Exec.
type FakeDaemon() =
    let rec bind (tries: int) =
        let port = freePort ()
        let listener = new HttpListener ()
        listener.Prefixes.Add $"http://localhost:{port}/"

        try
            listener.Start ()
            listener, port
        with :? HttpListenerException when tries > 1 ->
            listener.Close ()
            bind (tries - 1)

    let listener, port = bind 3
    let execs = ConcurrentQueue<string> ()
    let mutable sessionRequests = 0
    let mutable sessions = """{"sessions":[]}"""
    let mutable exec = 200, fixture "exec-success.json"

    let respond (context: HttpListenerContext) (status: int) (contentType: string) (body: string) =
        let bytes = Encoding.UTF8.GetBytes body
        context.Response.StatusCode <- status
        context.Response.ContentType <- contentType
        context.Response.ContentLength64 <- int64 bytes.Length
        context.Response.OutputStream.Write (bytes, 0, bytes.Length)
        context.Response.Close ()

    let handle (context: HttpListenerContext) =
        match context.Request.HttpMethod, context.Request.Url.AbsolutePath with
        | "GET", "/api/sessions" ->
            Interlocked.Increment &sessionRequests |> ignore
            respond context 200 "application/json" sessions
        | "POST", "/exec" ->
            let reader = new StreamReader (context.Request.InputStream, Encoding.UTF8)

            let body =
                try
                    reader.ReadToEnd ()
                finally
                    reader.Dispose ()

            execs.Enqueue body
            let status, reply = exec
            respond context status "application/json; charset=utf-8" reply
        | _ -> respond context 404 "text/plain" ""

    let thread =
        Thread (
            (fun () ->
                try
                    while listener.IsListening do
                        try
                            handle (listener.GetContext ())
                        with _ ->
                            ()
                with _ ->
                    ()),
            IsBackground = true,
            Name = "Fake SageFs daemon"
        )

    do thread.Start ()

    /// The address of the daemon.
    member _.Uri = Uri $"http://localhost:{port}/"

    /// The body of GET /api/sessions.
    member _.Sessions
        with get () = sessions
        and set value = sessions <- value

    /// The status code and body of POST /exec.
    member _.Exec
        with get () = exec
        and set value = exec <- value

    /// The bodies of the POST /exec requests, in arrival order.
    member _.Execs = List.ofSeq execs

    /// The number of GET /api/sessions requests.
    member _.SessionRequests = sessionRequests

    /// Stops answering; later requests fail to connect.
    member _.Stop() =
        try
            listener.Stop ()
            listener.Close ()
        with _ ->
            ()

    interface IDisposable with
        member daemon.Dispose() =
            daemon.Stop ()
