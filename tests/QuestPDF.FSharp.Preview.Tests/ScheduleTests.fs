module QuestPDF.FSharp.Preview.Tests.ScheduleTests

open System
open Expecto
open QuestPDF.FSharp.PreviewServer

let private ms (n: float) =
    TimeSpan.FromMilliseconds n

/// The number of renders started by a trigger on an idle gate followed by more triggers during the render, and the
/// final gate.
let private rendersFor (triggers: int) =
    let first, started = Schedule.trigger Idle
    let mutable gate = first
    let mutable renders = if started then 1 else 0

    for _ in 1..triggers do
        let next, start = Schedule.trigger gate
        gate <- next

        if start then
            renders <- renders + 1

    let mutable running = true

    while running do
        let next, start = Schedule.finish gate
        gate <- next

        if start then renders <- renders + 1 else running <- false

    renders, gate

[<Tests>]
let tests =
    testList
        "Schedule"
        [ test "Idle -> trigger -> Rendering" { Expect.equal (Schedule.trigger Idle) (Rendering false, true) "trigger" }
          test "a trigger during a render sets pending" { Expect.equal (Schedule.trigger (Rendering false)) (Rendering true, false) "pending" }
          test "finishing with pending starts the follow-up" {
              Expect.equal (Schedule.finish (Rendering true)) (Rendering false, true) "follow-up"
              Expect.equal (Schedule.finish (Rendering false)) (Idle, false) "idle"
          }
          testProperty "N triggers during a render give exactly one follow-up" (fun (n: byte) ->
              let renders, gate = rendersFor (int n)

              renders = (if n = 0uy then 1 else 2)
              && gate = Idle)
          test "nextPoll is the poll interval or four times the last render" {
              Expect.equal (Schedule.nextPoll (ms 500.0) (ms 100.0)) (ms 500.0) "poll"
              Expect.equal (Schedule.nextPoll (ms 500.0) (ms 200.0)) (ms 800.0) "4 x render"
          }
          test "None produces no ticks" { Expect.isNone (Schedule.pollDelay None 3 (ms 10.0)) "no poll" }
          test "no ticks with zero clients" {
              Expect.isNone (Schedule.pollDelay (Some (ms 500.0)) 0 (ms 10.0)) "no clients"
              Expect.equal (Schedule.pollDelay (Some (ms 500.0)) 1 (ms 10.0)) (Some (ms 500.0)) "one client"
              Expect.equal (Schedule.pollDelay (Some (ms 500.0)) 2 (ms 300.0)) (Some (ms 1200.0)) "a slow render"
          }
          test "the same document twice gives the frozen hint" {
              let document = obj ()
              Expect.equal (Schedule.frozen document document) (Some Schedule.frozenHint) "same reference"
          }
          test "fresh documents give no hint" {
              Expect.isNone (Schedule.frozen (obj ()) (obj ())) "fresh objects"
              Expect.isNone (Schedule.frozen null (obj ())) "the first render"
          } ]
