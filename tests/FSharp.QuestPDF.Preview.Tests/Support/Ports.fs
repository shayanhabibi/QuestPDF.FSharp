/// Loopback ports, test seams and HTTP and SSE clients for the server tests.
[<AutoOpen>]
module FSharp.QuestPDF.Preview.Tests.Support.Ports

open System
open System.Collections.Concurrent
open System.IO
open System.Net
open System.Net.Http
open System.Net.Sockets
open System.Threading
open QuestPDF.Fluent
open FSharp.QuestPDF
open FSharp.QuestPDF.PreviewServer

/// A loopback port that was free when the call returned.
let freePort () =
    let listener = new TcpListener (IPAddress.Loopback, 0)
    listener.Start ()

    try
        (listener.LocalEndpoint :?> IPEndPoint).Port
    finally
        listener.Stop ()

/// The seams of the tests: a 2 s bind timeout, a 100 ms keep-alive, no browser and no environment variables.
let internal testEnv: ServeEnv =
    { Daemon = Uri "http://localhost:1/"
      OpenBrowser = ignore
      Now = fun () -> DateTime.UtcNow
      BindTimeout = TimeSpan.FromSeconds 2.0
      KeepAlive = TimeSpan.FromMilliseconds 100.0
      Variable = fun _ -> None
      CurrentDirectory = fun () -> Environment.CurrentDirectory }

/// Options on a port with no polling and manual reload.
let quiet (port: int) =
    { Preview.defaults with
        Port = port
        Poll = None
        Reload = Preview.Manual }

/// A server of a document function on a free port, retried on three ports when the bind races.
let serveFree (document: unit -> Document) : Preview.Server =
    let rec attempt (tries: int) =
        let port = freePort ()

        try
            Preview.serveWith
                { testEnv with
                    BindTimeout = TimeSpan.FromMilliseconds 300.0 }
                (quiet port)
                document
        with :? InvalidOperationException when tries > 1 ->
            attempt (tries - 1)

    attempt 3

/// Runs a test body with a server, which is stopped afterwards.
let withServer (document: unit -> Document) (body: Preview.Server -> unit) =
    let server = serveFree document

    try
        body server
    finally
        Preview.stop server.Port

let private http = new HttpClient (Timeout = TimeSpan.FromSeconds 10.0)

/// A GET response: status code, content type, cache control and body.
type Reply =
    { Status: int
      ContentType: string
      CacheControl: string
      Body: byte[] }

    /// The body as UTF-8 text.
    member reply.Text = Text.Encoding.UTF8.GetString reply.Body

/// GETs a path of a server.
let get (server: Preview.Server) (path: string) : Reply =
    let response = http.GetAsync(Uri (server.Url, path)).GetAwaiter().GetResult ()

    try
        { Status = int response.StatusCode
          ContentType =
            (match response.Content.Headers.ContentType with
             | null -> ""
             | value ->
                 value.MediaType
                 |> Option.ofObj
                 |> Option.defaultValue "")
          CacheControl =
            (match response.Headers.CacheControl with
             | null -> ""
             | value -> string value)
          Body = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult () }
    finally
        response.Dispose ()

/// An open /events stream whose event names are read on a background thread.
type EventStream(server: Preview.Server) =
    let client = new HttpClient (Timeout = Timeout.InfiniteTimeSpan)
    let events = new BlockingCollection<string> ()

    let response =
        client.GetAsync(Uri (server.Url, "/events"), HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult ()

    let reader =
        new StreamReader (response.Content.ReadAsStreamAsync().GetAwaiter().GetResult ())

    let thread =
        Thread (
            (fun () ->
                try
                    let mutable line = reader.ReadLine ()

                    while not (isNull line) do
                        if line.StartsWith "event: " then
                            events.Add (line.Substring 7)

                        line <- reader.ReadLine ()
                with _ ->
                    ()),
            IsBackground = true
        )

    do thread.Start ()

    /// The content type of the stream.
    member _.ContentType = response.Content.Headers.ContentType.MediaType

    /// The next event name within a timeout.
    member _.Next(timeout: TimeSpan) : string option =
        let mutable name = null
        if events.TryTake (&name, timeout) then Some name else None

    interface IDisposable with
        member _.Dispose() =
            reader.Dispose ()
            response.Dispose ()
            client.Dispose ()
