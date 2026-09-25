module FSharp.QuestPDF.Preview.Tests.SageFsProtocolTests

open Expecto
open FSharp.QuestPDF.PreviewServer
open FSharp.QuestPDF.Preview.Tests.Support

/// The three sessions of the recorded /api/sessions reply: 96ed1036 on a test project in C:/src/FSharp.QuestPDF,
/// c1ae4696 with no projects and ca532c74 on P.fsproj, both in C:/work/invoice.
let private recorded () =
    SageFsProtocol.sessions (fixture "api-sessions.json")

let private matched (result: Result<SessionInfo, string>) =
    match result with
    | Ok session -> session.Id
    | Error reason -> $"error: {reason}"

/// The events of a recorded daemon stream, read line by line.
let private events (text: string) : SseEvent list =
    let lines = text.Replace("\r\n", "\n").Split '\n'

    lines
    |> Array.fold
        (fun (pending, read) line ->
            match SageFsProtocol.sseLine pending line with
            | next, Some event -> next, event :: read
            | next, None -> next, read)
        (SageFsProtocol.sseStart, [])
    |> snd
    |> List.rev

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
                  [ session "96ed1036" "C:/src/FSharp.QuestPDF" [ "C:/src/FSharp.QuestPDF/tests/FSharp.QuestPDF.Tests/FSharp.QuestPDF.Tests.fsproj" ]
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
                  "C:/src/FSharp.QuestPDF/tests/FSharp.QuestPDF.Tests/FSharp.QuestPDF.Tests.fsproj"

              Expect.equal
                  (matched (SageFsProtocol.matchSession true projects @"C:\src\FSharp.QuestPDF\tests\FSharp.QuestPDF.Tests" (recorded ())))
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
              Expect.equal (matched (SageFsProtocol.matchSession true "" @"c:\SRC\fsharp.questpdf\" (recorded ()))) "96ed1036" "working directory"

              Expect.equal
                  (matched (
                      SageFsProtocol.matchSession
                          true
                          @"c:\src\FSHARP.QuestPDF\tests\FSharp.QuestPDF.Tests\FSharp.QuestPDF.Tests.fsproj"
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
          }
          test "sseLine completes an event at a blank line with its name and data" {
              Expect.equal (events "event: state\ndata: {\"a\":1}\n\n") [ { Name = "state"; Data = "{\"a\":1}" } ] "one event"
          }
          test "sseLine joins data lines, names an unnamed event message and skips comments and retry" {
              Expect.equal
                  (events "retry: 500\n\n: keepalive\n\ndata: a\ndata: b\n\nevent: x\ndata:c\n\n")
                  [ { Name = "message"; Data = "a\nb" }; { Name = "x"; Data = "c" } ]
                  "two events"
          }
          test "sseLine completes no event for a blank line without data" { Expect.isEmpty (events "event: state\n\n\n") "no event" }
          test "the recorded daemon stream nudges each session for its fileReloaded and hotReloadChanged state events" {
              let recorded = events (fixture "events-hotreload.txt")

              let count (id: string) =
                  recorded
                  |> List.filter (SageFsProtocol.nudges id)
                  |> List.length

              Expect.equal
                  (recorded
                   |> List.filter (fun e -> e.Name = "state")
                   |> List.length)
                  13
                  "state events"

              Expect.equal (count "fa10ba0d") 3 "fa10ba0d: one fileReloaded and two hotReloadChanged"
              Expect.equal (count "9b7f5d5e") 2 "9b7f5d5e: two hotReloadChanged"
              Expect.equal (count "4758ad63") 0 "another session"
          }
          test "nudges ignores other events, state without a reload and unreadable data" {
              let nudges name data =
                  SageFsProtocol.nudges "s1" { Name = name; Data = data }

              Expect.isFalse (nudges "session" """{"hotReloadChanged":true,"sessionId":"s1"}""") "another event name"
              Expect.isFalse (nudges "state" """{"diagCount":0,"outputCount":13}""") "no reload"
              Expect.isFalse (nudges "state" """{"hotReloadChanged":false,"sessionId":"s1"}""") "not changed"
              Expect.isFalse (nudges "state" "{not json") "unreadable"
              Expect.isTrue (nudges "state" """{"hotReloadChanged":true,"sessionId":"s1"}""") "changed"
          }
          test "dashboard is the daemon host on the next port" {
              Expect.equal (SageFsProtocol.dashboard (System.Uri "http://localhost:37749/")) (System.Uri "http://localhost:37750/") "37750"
          }
          test "watchedCount reads the recorded watch-all reply" {
              Expect.equal (SageFsProtocol.watchedCount (fixture "watch-all.json")) (Some 3) "three files"
              Expect.equal (SageFsProtocol.watchedCount "<html>") None "unreadable"
          }
          test "reconnectDelay doubles from 0.5 s up to 10 s" {
              Expect.equal
                  ([ 0..7 ]
                   |> List.map (SageFsProtocol.reconnectDelay >> _.TotalSeconds))
                  [ 0.5; 1.0; 2.0; 4.0; 8.0; 10.0; 10.0; 10.0 ]
                  "delays"

              Expect.equal (SageFsProtocol.reconnectDelay 1000).TotalSeconds 10.0 "no overflow"
          } ]
