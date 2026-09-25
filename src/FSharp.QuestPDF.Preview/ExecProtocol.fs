namespace FSharp.QuestPDF.PreviewServer

open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Text.Json
open System.Text.RegularExpressions
open System.Threading

/// The result of asking the SageFs daemon to load a saved script again.
type internal ReloadOutcome =
    /// The script loaded.
    | Loaded
    /// The script failed to compile, with the compiler errors.
    | Compile of DiagnosticInfo list
    /// The top level of the script raised.
    | Runtime of message: string
    /// The script was left unevaluated: zero or several matching sessions, a failed connection, or an unreadable answer.
    | Routing of reason: string

/// The /exec protocol of the SageFs daemon, as served by SageFs 0.6.828.
[<RequireQualifiedAccess>]
module internal ExecProtocol =
    /// The number of requests made by the process.
    let private requests = ref 0L

    /// The JSON body of a POST /exec that loads a script into the session of a working directory. The code ends in a
    /// comment holding a number unique to the call.
    let request (script: string) (workingDirectory: string) : string =
        let number = Interlocked.Increment &requests.contents
        let quote = string '"'

        let code =
            $"#load @{quote}{script.Replace (quote, quote + quote)}{quote} // qpdf-preview {number}"

        JsonSerializer.Serialize
            {| code = code
               working_directory = workingDirectory |}

    /// A line of the Diagnostics block: severity, line, column from 0, and the start of the message.
    let private entry =
        Regex (@"^\s*\[(\w+)\] \((\d+),(\d+)\) (.*)$", RegexOptions.CultureInvariant)

    /// The compiler errors of the Diagnostics block of an /exec result, attributed to a script, with columns from 1.
    /// A message continues over the lines up to the next entry.
    let diagnostics (script: string) (text: string) : DiagnosticInfo list =
        let lines = text.Replace("\r\n", "\n").Split '\n'

        match
            lines
            |> Array.tryFindIndex (fun line -> line.Trim () = "Diagnostics:")
        with
        | None -> []
        | Some start ->
            let entries = ResizeArray<string * DiagnosticInfo> ()

            for line in lines[start + 1 ..] do
                let found = entry.Match line

                if found.Success then
                    entries.Add (
                        found.Groups[1].Value,
                        { File = script
                          Line = int found.Groups[2].Value
                          Column = int found.Groups[3].Value + 1
                          Message = found.Groups[4].Value }
                    )
                elif entries.Count > 0 then
                    let severity, last = entries[entries.Count - 1]

                    entries[entries.Count - 1] <-
                        severity,
                        { last with
                            Message = last.Message + "\n" + line }

            entries
            |> Seq.filter (fun (severity, _) -> severity = "error")
            |> Seq.map (fun (_, d) ->
                { d with
                    Message = d.Message.TrimEnd () })
            |> List.ofSeq

    /// A string property of a JSON object.
    let private property (name: string) (element: JsonElement) : string option =
        if element.ValueKind = JsonValueKind.Object then
            match element.TryGetProperty name with
            | true, value when value.ValueKind = JsonValueKind.String -> Some (value.GetString ())
            | _ -> None
        else
            None

    /// An object property of a JSON object.
    let private child (name: string) (element: JsonElement) : JsonElement option =
        if element.ValueKind = JsonValueKind.Object then
            match element.TryGetProperty name with
            | true, value when value.ValueKind = JsonValueKind.Object -> Some value
            | _ -> None
        else
            None

    /// The first paragraph of an evaluation error, without the "Error: Evaluation failed: " prefix.
    let private headline (error: string) =
        let first = error.Replace("\r\n", "\n").Split("\n\n").[0].Trim ()
        let prefix = "Error: Evaluation failed: "

        if first.StartsWith prefix then
            first.Substring prefix.Length
        else
            first

    /// The root element of a JSON document; None for text that fails to parse as JSON.
    let private json (body: string) : JsonElement option =
        try
            let document = JsonDocument.Parse body

            try
                Some (document.RootElement.Clone ())
            finally
                document.Dispose ()
        with _ ->
            None

    /// The outcome of an /exec response with a status code and a body.
    let parse (script: string) (status: int) (body: string) : ReloadOutcome =
        let unreadable =
            Routing $"The SageFs daemon answered HTTP {status} without a readable body."

        match json body with
        | None -> unreadable
        | Some root when
            root.ValueKind <> JsonValueKind.Object
            && status >= 200
            && status < 300
            ->
            unreadable
        | Some root when status >= 200 && status < 300 ->
            let succeeded =
                match root.TryGetProperty "success" with
                | true, value -> value.ValueKind <> JsonValueKind.False
                | false, _ -> true

            if succeeded then
                Loaded
            else
                let error =
                    property "error" root
                    |> Option.orElse (property "result" root)
                    |> Option.defaultValue ""

                match diagnostics script error with
                | [] -> Runtime (headline error)
                | errors -> Compile errors
        | Some root ->
            child "errorDetails" root
            |> Option.bind (child "fields")
            |> Option.bind (property "reason")
            |> Option.orElse (property "error" root)
            |> Option.defaultValue $"The SageFs daemon answered HTTP {status}."
            |> Routing

    /// A string literal of F# source at an index: its value and the index after it; None when no literal starts there
    /// or it does not end. Regular strings unescape backslash escapes, verbatim strings doubled quotes, and
    /// triple-quoted strings nothing.
    let private literal (text: string) (start: int) : (string * int) option =
        let n = text.Length

        let at (i: int) (token: string) =
            String.CompareOrdinal (text, i, token, 0, token.Length) = 0

        if at start "\"\"\"" then
            let close = text.IndexOf ("\"\"\"", start + 3, StringComparison.Ordinal)

            if close < 0 then
                None
            else
                Some (text.Substring (start + 3, close - start - 3), close + 3)
        elif at start "@\"" || at start "\"" then
            let verbatim = text[start] = '@'
            let value = StringBuilder ()
            let mutable i = if verbatim then start + 2 else start + 1
            let mutable closed = None

            while closed.IsNone && i < n do
                let c = text[i]

                if
                    c = '"'
                    && verbatim
                    && i + 1 < n
                    && text[i + 1] = '"'
                then
                    value.Append '"' |> ignore
                    i <- i + 2
                elif c = '"' then
                    closed <- Some (i + 1)
                elif c = '\\' && not verbatim && i + 1 < n then
                    value.Append (
                        match text[i + 1] with
                        | 'n' -> '\n'
                        | 't' -> '\t'
                        | 'r' -> '\r'
                        | other -> other
                    )
                    |> ignore

                    i <- i + 2
                else
                    value.Append c |> ignore
                    i <- i + 1

            closed
            |> Option.map (fun next -> string value, next)
        else
            None

    /// The paths named by the #load directives of F# script text, in order, as written. Directives inside comments and
    /// strings are left out.
    let loads (text: string) : string list =
        let n = text.Length

        let at (i: int) (token: string) =
            String.CompareOrdinal (text, i, token, 0, token.Length) = 0

        let paths = ResizeArray<string> ()
        let mutable i = 0
        let mutable depth = 0
        let mutable lineStart = true

        while i < n do
            let c = text[i]

            if c = '\n' then
                lineStart <- true
                i <- i + 1
            elif depth > 0 then
                if at i "(*" then
                    depth <- depth + 1
                    i <- i + 2
                elif at i "*)" then
                    depth <- depth - 1
                    i <- i + 2
                else
                    i <- i + 1
            elif c = ' ' || c = '\t' || c = '\r' then
                i <- i + 1
            elif at i "//" then
                let close = text.IndexOf ('\n', i)
                i <- if close < 0 then n else close
            elif at i "(*" && not (at i "(*)") then
                depth <- 1
                lineStart <- false
                i <- i + 2
            elif
                lineStart
                && at i "#load"
                && (i + 5 = n || Char.IsWhiteSpace text[i + 5])
            then
                lineStart <- false
                i <- i + 5
                let mutable reading = true

                while reading do
                    while i < n && (text[i] = ' ' || text[i] = '\t') do
                        i <- i + 1

                    match literal text i with
                    | Some (path, next) ->
                        paths.Add path
                        i <- next
                    | None -> reading <- false
            elif c = '"' || at i "@\"" then
                lineStart <- false

                i <-
                    match literal text i with
                    | Some (_, next) -> next
                    | None -> n
            elif c = '\'' && i + 2 < n && text[i + 2] = '\'' then
                lineStart <- false
                i <- i + 3
            elif
                c = '\''
                && i + 3 < n
                && text[i + 1] = '\\'
                && text[i + 3] = '\''
            then
                lineStart <- false
                i <- i + 4
            else
                lineStart <- false
                i <- i + 1

        List.ofSeq paths

    /// The full paths of the files a script loads, directly or through other loaded files, each once and after the
    /// files it loads; the script itself and files that cannot be read are left out. read returns the text of a file,
    /// or None for a file that cannot be read.
    let loadOrder (read: string -> string option) (script: string) : string list =
        let comparer =
            if OperatingSystem.IsWindows () then
                StringComparer.OrdinalIgnoreCase
            else
                StringComparer.Ordinal

        let root = Path.GetFullPath script
        let visited = HashSet<string> ([ root ], comparer)
        let order = ResizeArray<string> ()

        let rec visit (file: string) : bool =
            match read file with
            | None -> false
            | Some text ->
                for path in loads text do
                    let full = Path.GetFullPath (Path.Combine (Path.GetDirectoryName file, path))

                    if visited.Add full && visit full then
                        order.Add full

                true

        visit root |> ignore
        List.ofSeq order

    /// The outcome of a reload with its compile errors attributed to the file that holds them: the compile errors of
    /// the first loaded file whose own load, by probe, fails to compile, or the outcome itself when every loaded file
    /// compiles. A probe that cannot reach the session ends the search with the outcome itself. Other outcomes are
    /// returned without a probe.
    let attribute (probe: string -> ReloadOutcome) (files: string list) (outcome: ReloadOutcome) : ReloadOutcome =
        let rec search (files: string list) =
            match files with
            | [] -> outcome
            | file :: rest ->
                match probe file with
                | Compile errors -> Compile errors
                | Routing _ -> outcome
                | Loaded
                | Runtime _ -> search rest

        match outcome with
        | Compile _ -> search files
        | other -> other

    /// The status shown for an outcome until the next reload; None for a script that loaded.
    let issue (outcome: ReloadOutcome) : RenderStatus option =
        match outcome with
        | Loaded -> None
        | Compile errors -> Some (CompileFailed errors)
        | Runtime message
        | Routing message -> Some (ReloadFailed message)
