namespace QuestPDF.FSharp.PreviewServer

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

/// The session list of the SageFs daemon, as served by SageFs 0.6.828.
[<RequireQualifiedAccess>]
module internal SageFsProtocol =
    /// A string property of a JSON object; empty when missing.
    let private text (name: string) (element: JsonElement) =
        match element.TryGetProperty name with
        | true, value when value.ValueKind = JsonValueKind.String -> value.GetString ()
        | _ -> ""

    /// The sessions of a GET /api/sessions response; empty for a body that is no session list.
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
