namespace QuestPDF.FSharp.PreviewServer

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

    /// The status shown for an outcome until the next reload; None for a script that loaded.
    let issue (outcome: ReloadOutcome) : RenderStatus option =
        match outcome with
        | Loaded -> None
        | Compile errors -> Some (CompileFailed errors)
        | Runtime message
        | Routing message -> Some (ReloadFailed message)
