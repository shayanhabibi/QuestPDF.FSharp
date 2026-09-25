module QuestPDF.FSharp.Preview.Tests.ChangeFilterTests

open System
open System.IO
open Expecto
open QuestPDF.FSharp.PreviewServer

let private root = Path.Combine (Path.GetTempPath (), "invoice")

let private under (relative: string) =
    Path.Combine (root, relative)

let private window = TimeSpan.FromMilliseconds 100.0

let private at (ms: float) =
    (DateTime (2026, 1, 1)).AddMilliseconds ms

/// Runs a fake clock from 0 to 1000 ms in 10 ms ticks. Events arrive at their times; a reload started at a tick
/// finishes after the reload duration. Returns the start times of the reloads.
let private simulate (events: float list) (duration: float) =
    let mutable state = ChangeFilter.idle
    let mutable finishes: float option = None
    let starts = ResizeArray<float> ()

    for tick in 0..100 do
        let now = float tick * 10.0

        match finishes with
        | Some time when time <= now ->
            state <- ChangeFilter.finished window (at now) state
            finishes <- None
        | _ -> ()

        for _ in
            events
            |> List.filter (fun e -> e >= now && e < now + 10.0) do
            state <- ChangeFilter.event window (at now) state

        let next, start = ChangeFilter.tick (at now) state
        state <- next

        if start then
            starts.Add now
            finishes <- Some (now + duration)

    List.ofSeq starts

let private classify =
    testList
        "classify"
        [ test "Changed, Created and Renamed onto .fs and .fsx files count" {
              for change in
                  [ WatcherChangeTypes.Changed
                    WatcherChangeTypes.Created
                    WatcherChangeTypes.Renamed ] do
                  for file in [ "invoice.fsx"; "Parts.fs"; Path.Combine ("parts", "header.fsx") ] do
                      Expect.isTrue (ChangeFilter.classify root change (under file)) $"{change} {file}"
          }
          test "deletions and other extensions don't count" {
              Expect.isFalse (ChangeFilter.classify root WatcherChangeTypes.Deleted (under "invoice.fsx")) "deleted"
              Expect.isFalse (ChangeFilter.classify root WatcherChangeTypes.Changed (under "notes.md")) "markdown"
              Expect.isFalse (ChangeFilter.classify root WatcherChangeTypes.Changed (under "App.fsproj")) "project"
          }
          test "editor temporaries and build folders don't count" {
              for file in
                  [ "x.fsx~"
                    "x.fsx___jb_tmp___"
                    "x.fsx___jb_old___"
                    ".#x.fsx"
                    "4913"
                    Path.Combine ("bin", "Debug", "x.fs")
                    Path.Combine ("obj", "x.fs")
                    Path.Combine (".git", "x.fs")
                    Path.Combine ("parts", "bin", "x.fsx") ] do
                  Expect.isFalse (ChangeFilter.classify root WatcherChangeTypes.Changed (under file)) file
          }
          test "the build-folder names count only as folders under the root" {
              let binRoot = Path.Combine (Path.GetTempPath (), "bin", "invoice")

              Expect.isTrue
                  (ChangeFilter.classify binRoot WatcherChangeTypes.Changed (Path.Combine (binRoot, "invoice.fsx")))
                  "a root inside a bin folder"

              Expect.isTrue (ChangeFilter.classify root WatcherChangeTypes.Changed (under "binary.fsx")) "a file named bin*"
          } ]

let private debounce =
    testList
        "debounce"
        [ test "5 events in 80 ms give 1 reload, 100 ms after the last" {
              Expect.equal (simulate [ 0.0; 20.0; 40.0; 60.0; 80.0 ] 50.0) [ 180.0 ] "one reload"
          }
          test "events during an in-flight reload give exactly 1 follow-up" {
              let starts = simulate [ 0.0; 150.0; 200.0; 250.0 ] 200.0
              Expect.equal starts.Length 2 $"two reloads, got %A{starts}"
              Expect.isGreaterThanOrEqual starts[1] 300.0 "the follow-up starts after the first finishes"
          }
          test "no events give no reloads" { Expect.isEmpty (simulate [] 50.0) "none" }
          test "wait is the time to the due reload, or None" {
              Expect.isNone (ChangeFilter.wait (at 0.0) ChangeFilter.idle) "idle"
              let due = ChangeFilter.event window (at 0.0) ChangeFilter.idle
              Expect.equal (ChangeFilter.wait (at 30.0) due) (Some (TimeSpan.FromMilliseconds 70.0)) "70 ms"
              Expect.equal (ChangeFilter.wait (at 300.0) due) (Some TimeSpan.Zero) "overdue"
          } ]

[<Tests>]
let tests = testList "ChangeFilter" [ classify; debounce ]
