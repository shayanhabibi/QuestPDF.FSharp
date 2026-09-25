namespace QuestPDF.FSharp.PreviewServer

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json

/// A compiler error of a saved script.
type internal DiagnosticInfo =
    { File: string
      Line: int
      Column: int
      Message: string }

/// The state shown above the pages.
type internal RenderStatus =
    | Starting
    | Rendered of pages: int * renderTime: TimeSpan
    | RenderFailed of error: string
    | CompileFailed of DiagnosticInfo list
    | ReloadFailed of reason: string

/// The result of one render.
type internal Outcome =
    | Pages of svgs: string list * elapsed: TimeSpan
    | Failure of error: string

/// The published state of a preview. Pages hold the served SVG documents.
type internal Snapshot =
    {
        Instance: string
        Version: int64
        Pages: string list
        Hashes: string list
        Status: RenderStatus
        /// The status of the last render, shown while ReloadIssue is None.
        LastRender: RenderStatus
        /// The status of the last failed reload, shown over the render status until a reload succeeds.
        ReloadIssue: RenderStatus option
        Hint: string option
        Reload: string
    }

[<RequireQualifiedAccess>]
module internal Snapshot =
    /// The content hash of a page: 16 hexadecimal digits of its SHA-256.
    let hash (page: string) : string =
        Convert.ToHexStringLower(SHA256.HashData (Encoding.UTF8.GetBytes page)).Substring (0, 16)

    /// The state before the first render, at version 0.
    let initial (instance: string) (reload: string) : Snapshot =
        { Instance = instance
          Version = 0L
          Pages = []
          Hashes = []
          Status = Starting
          LastRender = Starting
          ReloadIssue = None
          Hint = None
          Reload = reload }

    /// The status with the render time left out: two renders of the same pages are the same state.
    let private comparable (status: RenderStatus) =
        match status with
        | Rendered (pages, _) -> Rendered (pages, TimeSpan.Zero)
        | other -> other

    /// The next state, with the version increased when the hashes, the status or the hint differ.
    let private publish (next: Snapshot) (previous: Snapshot) =
        if
            next.Hashes = previous.Hashes
            && comparable next.Status = comparable previous.Status
            && next.Hint = previous.Hint
        then
            previous
        else
            { next with
                Version = previous.Version + 1L }

    /// The state after a render. The version increases only when the hashes, the status or the hint change; the
    /// render time alone is no change. A failure keeps the pages, and a reload issue stays the shown status.
    let render (outcome: Outcome) (hint: string option) (snapshot: Snapshot) : Snapshot =
        let next =
            match outcome with
            | Pages (svgs, elapsed) ->
                { snapshot with
                    Pages = svgs
                    Hashes = svgs |> List.map hash
                    LastRender = Rendered (svgs.Length, elapsed)
                    Hint = hint }
            | Failure error ->
                { snapshot with
                    LastRender = RenderFailed error
                    Hint = hint }

        publish
            { next with
                Status = defaultArg next.ReloadIssue next.LastRender }
            snapshot

    /// The state with a reload issue set or cleared, bumping the version when the shown status changes. A cleared
    /// issue shows the status of the last render.
    let reloaded (issue: RenderStatus option) (snapshot: Snapshot) : Snapshot =
        publish
            { snapshot with
                ReloadIssue = issue
                Status = defaultArg issue snapshot.LastRender }
            snapshot

    /// The state with a render status, bumping the version when the shown status changes.
    let withStatus (status: RenderStatus) (snapshot: Snapshot) : Snapshot =
        publish
            { snapshot with
                LastRender = status
                Status = defaultArg snapshot.ReloadIssue status }
            snapshot

    let private writeStatus (writer: Utf8JsonWriter) (status: RenderStatus) =
        writer.WriteStartObject ()

        match status with
        | Starting -> writer.WriteString ("kind", "starting")
        | Rendered (pages, time) ->
            writer.WriteString ("kind", "rendered")
            writer.WriteNumber ("pages", pages)
            writer.WriteNumber ("renderMs", Math.Round (time.TotalMilliseconds, 1))
        | RenderFailed error ->
            writer.WriteString ("kind", "renderFailed")
            writer.WriteString ("error", error)
        | CompileFailed diagnostics ->
            writer.WriteString ("kind", "compileFailed")
            writer.WriteStartArray "diagnostics"

            for d in diagnostics do
                writer.WriteStartObject ()
                writer.WriteString ("file", d.File)
                writer.WriteNumber ("line", d.Line)
                writer.WriteNumber ("column", d.Column)
                writer.WriteString ("message", d.Message)
                writer.WriteEndObject ()

            writer.WriteEndArray ()
        | ReloadFailed reason ->
            writer.WriteString ("kind", "reloadFailed")
            writer.WriteString ("reason", reason)

        writer.WriteEndObject ()

    /// The JSON document of /snapshot and of the SSE version event.
    let json (snapshot: Snapshot) : string =
        let stream = new MemoryStream ()
        let writer = new Utf8JsonWriter (stream)

        try
            writer.WriteStartObject ()
            writer.WriteString ("instance", snapshot.Instance)
            writer.WriteNumber ("version", snapshot.Version)
            writer.WriteStartArray "pages"

            for h in snapshot.Hashes do
                writer.WriteStringValue h

            writer.WriteEndArray ()
            writer.WritePropertyName "status"
            writeStatus writer snapshot.Status

            match snapshot.Hint with
            | Some hint -> writer.WriteString ("hint", hint)
            | None -> writer.WriteNull "hint"

            writer.WriteString ("reload", snapshot.Reload)
            writer.WriteEndObject ()
            writer.Flush ()
            Encoding.UTF8.GetString (stream.ToArray ())
        finally
            writer.Dispose ()
            stream.Dispose ()
