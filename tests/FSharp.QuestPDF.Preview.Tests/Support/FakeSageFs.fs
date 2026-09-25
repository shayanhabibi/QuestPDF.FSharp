/// Golden SageFs replies and a fake daemon that serves them.
[<AutoOpen>]
module FSharp.QuestPDF.Preview.Tests.Support.FakeSageFs

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

/// A listener started on http://localhost:<port>/, or None when the port is taken.
let private listen (port: int) : HttpListener option =
    let listener = new HttpListener ()
    listener.Prefixes.Add $"http://localhost:{port}/"

    try
        listener.Start ()
        Some listener
    with :? HttpListenerException ->
        listener.Close ()
        None

/// Answers the requests of a listener on a background thread until it stops.
let private serveOn (name: string) (listener: HttpListener) (handle: HttpListenerContext -> unit) =
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
            Name = name
        )

    thread.Start ()

/// A SageFs daemon on a loopback port with its dashboard on the next port. GET /api/sessions answers Sessions, POST
/// /exec records the body and answers Exec, GET /events holds an event stream open for Push, and the dashboard's
/// POST /api/sessions/<id>/hotreload/watch-all records the path and answers the recorded watch-all reply.
type FakeDaemon() =
    // Ports below the dynamic range, where port + 1 is rarely taken by an outgoing connection.
    let rec bind (tries: int) =
        let port = Random.Shared.Next (20000, 40000)

        match listen port with
        | Some listener ->
            match listen (port + 1) with
            | Some dashboard -> listener, dashboard, port
            | None when tries > 1 ->
                listener.Close ()
                bind (tries - 1)
            | None ->
                listener.Close ()
                failwith $"No free port after {port}"
        | None when tries > 1 -> bind (tries - 1)
        | None -> failwith $"Port {port} is taken"

    let listener, dashboard, port = bind 20
    let execs = ConcurrentQueue<string> ()
    let watchAlls = ConcurrentQueue<string> ()
    let streams = ConcurrentDictionary<Guid, HttpListenerResponse> ()
    let mutable sessionRequests = 0
    let mutable eventConnections = 0
    let mutable sessions = """{"sessions":[]}"""
    let mutable exec = 200, fixture "exec-success.json"
    let mutable execFor: (string -> int * string) option = None
    let mutable onExec: unit -> unit = ignore
    let mutable eventDelay = TimeSpan.Zero

    let respond (context: HttpListenerContext) (status: int) (contentType: string) (body: string) =
        let bytes = Encoding.UTF8.GetBytes body
        context.Response.StatusCode <- status
        context.Response.ContentType <- contentType
        context.Response.ContentLength64 <- int64 bytes.Length
        context.Response.OutputStream.Write (bytes, 0, bytes.Length)
        context.Response.Close ()

    let write (id: Guid) (response: HttpListenerResponse) (text: string) =
        try
            let bytes = Encoding.UTF8.GetBytes text

            lock response (fun () ->
                response.OutputStream.Write (bytes, 0, bytes.Length)
                response.OutputStream.Flush ())
        with _ ->
            streams.TryRemove id |> ignore

    let handle (context: HttpListenerContext) =
        match context.Request.HttpMethod, context.Request.Url.AbsolutePath with
        | "GET", "/api/sessions" ->
            Interlocked.Increment &sessionRequests |> ignore
            respond context 200 "application/json" sessions
        | "GET", "/events" ->
            let openStream () =
                let response = context.Response
                response.StatusCode <- 200
                response.ContentType <- "text/event-stream"
                response.SendChunked <- true
                let id = Guid.NewGuid ()
                streams[id] <- response
                write id response "retry: 500\n\n"
                Interlocked.Increment &eventConnections |> ignore

            let delay = eventDelay

            if delay > TimeSpan.Zero then
                let late =
                    Thread (
                        (fun () ->
                            Thread.Sleep delay

                            try
                                openStream ()
                            with _ ->
                                ()),
                        IsBackground = true
                    )

                late.Start ()
            else
                openStream ()
        | "POST", "/exec" ->
            let reader = new StreamReader (context.Request.InputStream, Encoding.UTF8)

            let body =
                try
                    reader.ReadToEnd ()
                finally
                    reader.Dispose ()

            execs.Enqueue body
            onExec ()

            let status, reply =
                match execFor with
                | Some answer -> answer body
                | None -> exec

            respond context status "application/json; charset=utf-8" reply
        | _ -> respond context 404 "text/plain" ""

    let handleDashboard (context: HttpListenerContext) =
        let path = context.Request.Url.AbsolutePath

        if
            context.Request.HttpMethod = "POST"
            && path.EndsWith "/hotreload/watch-all"
        then
            watchAlls.Enqueue path
            respond context 200 "application/json" (fixture "watch-all.json")
        else
            respond context 404 "text/plain" ""

    do
        serveOn "Fake SageFs daemon" listener handle
        serveOn "Fake SageFs dashboard" dashboard handleDashboard

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

    /// The status code and body of POST /exec by the request body, in place of Exec when set.
    member _.ExecFor
        with get () = execFor
        and set value = execFor <- value

    /// Runs on each POST /exec after the body is recorded and before the reply, as the evaluation of the script.
    member _.OnExec
        with get () = onExec
        and set value = onExec <- value

    /// The time GET /events waits before it answers; other requests are answered meanwhile.
    member _.EventDelay
        with get () = eventDelay
        and set value = eventDelay <- value

    /// The bodies of the POST /exec requests, in arrival order.
    member _.Execs = List.ofSeq execs

    /// The number of GET /api/sessions requests.
    member _.SessionRequests = sessionRequests

    /// The paths of the watch-all requests to the dashboard, in arrival order.
    member _.WatchAlls = List.ofSeq watchAlls

    /// The number of GET /events requests.
    member _.EventConnections = eventConnections

    /// Writes an event to every open event stream.
    member _.Push(name: string, data: string) =
        for KeyValue (id, response) in streams do
            write id response $"event: {name}\ndata: {data}\n\n"

    /// Ends every open event stream.
    member _.DropStreams() =
        for KeyValue (id, response) in streams do
            if streams.TryRemove id |> fst then
                try
                    response.Abort ()
                with _ ->
                    ()

    /// Stops answering; later requests fail to connect.
    member daemon.Stop() =
        daemon.DropStreams ()

        for listener in [ listener; dashboard ] do
            try
                listener.Stop ()
                listener.Close ()
            with _ ->
                ()

    interface IDisposable with
        member daemon.Dispose() =
            daemon.Stop ()
