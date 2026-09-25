namespace FSharp.QuestPDF.PreviewServer

open System
open System.Text.Json

/// A session of the SageFs daemon.
type internal SessionInfo =
    {
        /// The session id.
        Id: string
        /// The directory that /exec routes on.
        WorkingDirectory: string
        /// The projects requested for the session, as listed in SAGEFS_SESSION_PROJECTS.
        Projects: string list
    }

/// A Server-Sent Event.
type internal SseEvent =
    {
        /// The event name; message for an event without an event field.
        Name: string
        /// The data lines of the event, joined with line feeds.
        Data: string
    }

/// The lines of a Server-Sent Event read so far.
type internal SsePending =
    {
        /// The event name; empty until an event field is read.
        Name: string
        /// The data lines, last first.
        Lines: string list
    }

/// The session list, the event stream and the hot reload routes of the SageFs daemon, as served by SageFs 0.6.828.
[<RequireQualifiedAccess>]
module internal SageFsProtocol =
    /// A string property of a JSON object; empty when missing.
    let private text (name: string) (element: JsonElement) =
        match element.TryGetProperty name with
        | true, value when value.ValueKind = JsonValueKind.String -> value.GetString ()
        | _ -> ""

    /// The sessions of a GET /api/sessions response; empty for a body other than a session list.
    let sessions (json: string) : SessionInfo list =
        try
            let document = JsonDocument.Parse json

            try
                match document.RootElement.TryGetProperty "sessions" with
                | true, list when list.ValueKind = JsonValueKind.Array ->
                    [ for session in list.EnumerateArray () do
                          { Id = text "id" session
                            WorkingDirectory = text "workingDirectory" session
                            Projects =
                              match session.TryGetProperty "projects" with
                              | true, projects when projects.ValueKind = JsonValueKind.Array ->
                                  [ for project in projects.EnumerateArray () do
                                        if project.ValueKind = JsonValueKind.String then
                                            project.GetString () ]
                              | _ -> [] } ]
                | _ -> []
            finally
                document.Dispose ()
        with _ ->
            []

    /// A path in comparable form: without a trailing separator and, on Windows, with forward slashes in lower case.
    let private normalise (windows: bool) (path: string) =
        let path =
            if windows then
                path.Replace('\\', '/').ToLowerInvariant ()
            else
                path

        path.TrimEnd '/'

    /// The session of the current process: the session whose projects are those of SAGEFS_SESSION_PROJECTS (a
    /// semicolon-separated list) when it is set, and otherwise the session whose working directory is the current
    /// directory. Paths compare case-insensitively with either separator on Windows. The error holds the number of
    /// matches when it is not one.
    let matchSession (windows: bool) (projects: string) (currentDirectory: string) (sessions: SessionInfo list) : Result<SessionInfo, string> =
        let paths (list: string seq) =
            list
            |> Seq.filter (String.IsNullOrWhiteSpace >> not)
            |> Seq.map (fun path -> normalise windows (path.Trim ()))
            |> Set.ofSeq

        let wanted = paths (projects.Split ';')

        let matches =
            if wanted.IsEmpty then
                let directory = normalise windows currentDirectory

                sessions
                |> List.filter (fun session -> normalise windows session.WorkingDirectory = directory)
            else
                sessions
                |> List.filter (fun session -> paths session.Projects = wanted)

        match matches with
        | [ session ] -> Ok session
        | _ -> Error $"SageFs session not identified ({matches.Length} matches)"

    /// The state of a stream before its first line.
    let sseStart: SsePending = { Name = ""; Lines = [] }

    /// The event under construction after a line of a stream, and the completed event when the line is blank and the
    /// event has data.
    let sseLine (pending: SsePending) (line: string) : SsePending * SseEvent option =
        let value (field: string) =
            if line.Length = field.Length then
                ""
            else
                let value = line.Substring (field.Length + 1)

                if value.StartsWith ' ' then value.Substring 1 else value

        if line.Length = 0 then
            match pending.Lines with
            | [] -> sseStart, None
            | lines ->
                sseStart,
                Some
                    { Name = (if pending.Name = "" then "message" else pending.Name)
                      Data = lines |> List.rev |> String.concat "\n" }
        elif line = "data" || line.StartsWith "data:" then
            { pending with
                Lines = value "data" :: pending.Lines },
            None
        elif line = "event" || line.StartsWith "event:" then
            { pending with Name = value "event" }, None
        else
            pending, None

    /// Whether a daemon event reports new code in a session: a state event with fileReloaded, or hotReloadChanged set
    /// to true, for the session id.
    let nudges (sessionId: string) (event: SseEvent) : bool =
        event.Name = "state"
        && (try
                let document = JsonDocument.Parse event.Data

                try
                    let root = document.RootElement

                    let property (name: string) =
                        match root.TryGetProperty name with
                        | true, value -> Some value
                        | _ -> None

                    root.ValueKind = JsonValueKind.Object
                    && (match property "sessionId" with
                        | Some id when id.ValueKind = JsonValueKind.String -> id.GetString () = sessionId
                        | _ -> false)
                    && ((match property "hotReloadChanged" with
                         | Some changed -> changed.ValueKind = JsonValueKind.True
                         | None -> false)
                        || (match property "fileReloaded" with
                            | Some path -> path.ValueKind = JsonValueKind.String
                            | None -> false))
                finally
                    document.Dispose ()
            with _ ->
                false)

    /// The dashboard of a daemon: the same host on the next port.
    let dashboard (daemon: Uri) : Uri =
        UriBuilder(daemon, Port = daemon.Port + 1).Uri

    /// The watchedCount of a watch-all reply.
    let watchedCount (json: string) : int option =
        try
            let document = JsonDocument.Parse json

            try
                match document.RootElement.TryGetProperty "watchedCount" with
                | true, count when count.ValueKind = JsonValueKind.Number -> Some (count.GetInt32 ())
                | _ -> None
            finally
                document.Dispose ()
        with _ ->
            None

    /// The wait before a reconnect attempt, counted from 0: 0.5 s, doubled per attempt, at most 10 s.
    let reconnectDelay (attempt: int) : TimeSpan =
        TimeSpan.FromSeconds (
            0.5
            * Math.Pow (2.0, float (attempt |> min 5 |> max 0))
            |> min 10.0
        )
