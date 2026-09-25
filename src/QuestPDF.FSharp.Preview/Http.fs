namespace QuestPDF.FSharp.PreviewServer

open System
open System.Collections.Concurrent
open System.Net
open System.Text
open System.Threading

/// A complete HTTP response.
type internal Reply =
    { Status: int
      ContentType: string
      CacheControl: string
      Body: byte[] }

[<RequireQualifiedAccess>]
module internal Reply =
    /// A UTF-8 text response that is never cached.
    let text (status: int) (contentType: string) (body: string) =
        { Status = status
          ContentType = contentType
          CacheControl = "no-store"
          Body = Encoding.UTF8.GetBytes body }

    /// The 404 response.
    let notFound = text 404 "text/plain; charset=utf-8" "Not found"

/// An open event stream.
type private Client =
    { Response: HttpListenerResponse
      Gate: obj }

/// The open Server-Sent Events streams of a server. A comment is written to each stream at every keep-alive
/// interval, and a stream whose write fails is closed and removed.
type internal SseHub(keepAlive: TimeSpan) =
    let clients = ConcurrentDictionary<Guid, Client> ()
    let stopped = new ManualResetEventSlim (false)

    let write (id: Guid) (client: Client) (bytes: byte[]) =
        try
            lock client.Gate (fun () ->
                client.Response.OutputStream.Write (bytes, 0, bytes.Length)
                client.Response.OutputStream.Flush ())
        with _ ->
            if clients.TryRemove id |> fst then
                try
                    client.Response.Abort ()
                with _ ->
                    ()

    let ping = Encoding.UTF8.GetBytes ": ping\n\n"

    let thread =
        Thread (
            (fun () ->
                try
                    while not (stopped.Wait keepAlive) do
                        try
                            for KeyValue (id, client) in clients do
                                write id client ping
                        with _ ->
                            ()
                finally
                    stopped.Set ()),
            IsBackground = true,
            Name = "QuestPDF preview keep-alive"
        )

    do thread.Start ()

    /// The number of open streams.
    member _.Count = clients.Count

    /// Opens a stream on a response and writes its first text.
    member _.Add(response: HttpListenerResponse, first: string) =
        response.StatusCode <- 200
        response.ContentType <- "text/event-stream"
        response.Headers["Cache-Control"] <- "no-store"
        response.SendChunked <- true
        let id = Guid.NewGuid ()
        let client = { Response = response; Gate = obj () }
        clients[id] <- client
        write id client (Encoding.UTF8.GetBytes first)

    /// Writes a keep-alive comment to every open stream, which closes the streams whose pages are gone.
    member _.Ping() =
        for KeyValue (id, client) in clients do
            write id client ping

    /// Writes a text to every open stream.
    member _.Broadcast(text: string) =
        let bytes = Encoding.UTF8.GetBytes text

        for KeyValue (id, client) in clients do
            write id client bytes

    interface IDisposable with
        member _.Dispose() =
            stopped.Set ()

            for KeyValue (id, client) in clients do
                if clients.TryRemove id |> fst then
                    try
                        client.Response.Abort ()
                    with _ ->
                        ()

[<RequireQualifiedAccess>]
module internal Http =
    /// A listener started on http://localhost:<port>/. A port in use is retried every 250 ms until the timeout,
    /// after which InvalidOperationException is raised.
    let bind (port: int) (timeout: TimeSpan) (now: unit -> DateTime) : HttpListener =
        let deadline = now () + timeout

        let rec attempt () =
            let listener = new HttpListener ()
            listener.Prefixes.Add $"http://localhost:{port}/"

            try
                listener.Start ()
                listener
            with :? HttpListenerException as error ->
                listener.Close ()

                if now () < deadline then
                    Thread.Sleep 250
                    attempt ()
                else
                    let seconds = timeout.TotalSeconds.ToString "0.#"

                    raise (InvalidOperationException ($"The preview could not listen on port {port} for {seconds} s: {error.Message}", error))

        attempt ()

    /// Writes a complete response and closes it.
    let reply (context: HttpListenerContext) (reply: Reply) =
        let response = context.Response
        response.StatusCode <- reply.Status
        response.ContentType <- reply.ContentType
        response.Headers["Cache-Control"] <- reply.CacheControl
        response.ContentLength64 <- int64 reply.Body.Length
        response.OutputStream.Write (reply.Body, 0, reply.Body.Length)
        response.Close ()

    /// Accepts requests on a background thread until the listener stops, and handles each on the thread pool.
    let accept (listener: HttpListener) (handle: HttpListenerContext -> unit) : unit =
        let handleSafely (context: HttpListenerContext) =
            try
                handle context
            with _ ->
                try
                    context.Response.Abort ()
                with _ ->
                    ()

        let thread =
            Thread (
                (fun () ->
                    let mutable running = true

                    while running do
                        try
                            let context = listener.GetContext ()

                            ThreadPool.QueueUserWorkItem (fun _ -> handleSafely context)
                            |> ignore
                        with _ ->
                            running <- listener.IsListening),
                IsBackground = true,
                Name = "QuestPDF preview listener"
            )

        thread.Start ()
