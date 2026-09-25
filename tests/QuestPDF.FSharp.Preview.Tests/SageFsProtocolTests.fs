module QuestPDF.FSharp.Preview.Tests.SageFsProtocolTests

open Expecto
open QuestPDF.FSharp.PreviewServer
open QuestPDF.FSharp.Preview.Tests.Support

/// The three sessions of the recorded /api/sessions reply: 96ed1036 on a test project in C:/src/QuestPDF.FSharp,
/// c1ae4696 with no projects and ca532c74 on P.fsproj, both in C:/work/invoice.
let private recorded () =
    SageFsProtocol.sessions (fixture "api-sessions.json")

let private matched (result: Result<SessionInfo, string>) =
    match result with
    | Ok session -> session.Id
    | Error reason -> $"error: {reason}"

let private session (id: string) (workingDirectory: string) (projects: string list) =
    { Id = id
      WorkingDirectory = workingDirectory
      Projects = projects }

[<Tests>]
let tests =
    testList
        "SageFsProtocol"
        [ test "sessions reads the id, working directory and projects of the recorded reply" {
              Expect.equal
                  (recorded ())
                  [ session "96ed1036" "C:/src/QuestPDF.FSharp" [ "C:/src/QuestPDF.FSharp/tests/QuestPDF.FSharp.Tests/QuestPDF.FSharp.Tests.fsproj" ]
                    session "c1ae4696" "C:/work/invoice" []
                    session "ca532c74" "C:/work/invoice" [ "C:/work/invoice/p/P.fsproj" ] ]
                  "sessions"
          }
          test "sessions of an unreadable reply is empty" {
              Expect.isEmpty (SageFsProtocol.sessions "<html>") "none"
              Expect.isEmpty (SageFsProtocol.sessions "") "none"
          }
          test "one session by projects" {
              let projects =
                  "C:/src/QuestPDF.FSharp/tests/QuestPDF.FSharp.Tests/QuestPDF.FSharp.Tests.fsproj"

              Expect.equal
                  (matched (SageFsProtocol.matchSession true projects @"C:\src\QuestPDF.FSharp\tests\QuestPDF.FSharp.Tests" (recorded ())))
                  "96ed1036"
                  "the project session, whatever the current directory"
          }
          test "zero and two sessions by projects" {
              let twins =
                  [ session "a" "C:/one" [ "C:/p/A.fsproj"; "C:/p/B.fsproj" ]
                    session "b" "C:/two" [ "C:/p/B.fsproj"; "C:/p/A.fsproj" ] ]

              Expect.equal
                  (matched (SageFsProtocol.matchSession true "C:/p/A.fsproj;C:/p/B.fsproj" "C:/one" twins))
                  "error: SageFs session not identified (2 matches)"
                  "two, in any order"

              Expect.equal
                  (matched (SageFsProtocol.matchSession true "C:/p/Other.fsproj" "C:/one" twins))
                  "error: SageFs session not identified (0 matches)"
                  "none"
          }
          test "one session by working directory" {
              let sessions =
                  recorded ()
                  |> List.filter (fun s -> s.Id <> "ca532c74")

              Expect.equal (matched (SageFsProtocol.matchSession true "" "C:/work/invoice" sessions)) "c1ae4696" "the project-less session"
          }
          test "two and zero sessions by working directory" {
              Expect.equal
                  (matched (SageFsProtocol.matchSession true "" "C:/work/invoice" (recorded ())))
                  "error: SageFs session not identified (2 matches)"
                  "two"

              Expect.equal
                  (matched (SageFsProtocol.matchSession true "" "C:/elsewhere" (recorded ())))
                  "error: SageFs session not identified (0 matches)"
                  "none"
          }
          test "Windows paths compare case-insensitively with normalised separators" {
              Expect.equal (matched (SageFsProtocol.matchSession true "" @"c:\SRC\questpdf.fsharp\" (recorded ()))) "96ed1036" "working directory"

              Expect.equal
                  (matched (
                      SageFsProtocol.matchSession
                          true
                          @"c:\src\QUESTPDF.FSharp\tests\QuestPDF.FSharp.Tests\QuestPDF.FSharp.Tests.fsproj"
                          "C:/x"
                          (recorded ())
                  ))
                  "96ed1036"
                  "projects"
          }
          test "other systems compare paths exactly" {
              let sessions = [ session "a" "/home/me/Invoice" [] ]

              Expect.equal
                  (matched (SageFsProtocol.matchSession false "" "/home/me/invoice" sessions))
                  "error: SageFs session not identified (0 matches)"
                  "case differs"

              Expect.equal (matched (SageFsProtocol.matchSession false "" "/home/me/Invoice/" sessions)) "a" "trailing separator"
          } ]
