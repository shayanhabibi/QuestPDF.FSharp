module QuestPDF.FSharp.Preview.Tests.SnapshotTests

open System
open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open Expecto
open QuestPDF.FSharp.PreviewServer

let private ms (n: float) =
    TimeSpan.FromMilliseconds n

let private rendered (pages: string list) =
    Pages (pages, ms 10.0)

let private start () =
    Snapshot.initial "abc" "manual"

/// The Shell.html embedded in the preview assembly.
let private shell () =
    let assembly = typeof<Snapshot>.Assembly
    let stream = assembly.GetManifestResourceStream "Shell.html"
    let reader = new StreamReader (stream)

    try
        reader.ReadToEnd ()
    finally
        reader.Dispose ()

/// The property names of a JSON object.
let private names (element: JsonElement) =
    element.EnumerateObject ()
    |> Seq.map _.Name
    |> Set.ofSeq

[<Tests>]
let tests =
    testList
        "Snapshot"
        [ test "the initial state is Starting at version 0" {
              let start = start ()
              Expect.equal (start.Version, start.Status, start.Pages) (0L, Starting, []) "initial"
          }
          test "equal SVG lists give equal hashes and no version bump" {
              let first =
                  start ()
                  |> Snapshot.render (rendered [ "<svg>a</svg>"; "<svg>b</svg>" ]) None

              let second =
                  first
                  |> Snapshot.render (Pages ([ "<svg>a</svg>"; "<svg>b</svg>" ], ms 20.0)) None

              Expect.equal first.Version 1L "the first render bumps"
              Expect.equal second.Hashes first.Hashes "hashes"
              Expect.equal second.Version first.Version "no bump"
          }
          test "a change to page 2 of 3 changes only hash 2" {
              let first =
                  start ()
                  |> Snapshot.render (rendered [ "a"; "b"; "c" ]) None

              let second =
                  first
                  |> Snapshot.render (rendered [ "a"; "B"; "c" ]) None

              let changed = List.map2 (<>) first.Hashes second.Hashes
              Expect.equal changed [ false; true; false ] "changed hashes"
              Expect.equal second.Version 2L "bump"
          }
          test "an error then the same pages bumps the version twice" {
              let first =
                  start ()
                  |> Snapshot.render (rendered [ "a" ]) None

              let failed = first |> Snapshot.render (Failure "boom") None
              let recovered = failed |> Snapshot.render (rendered [ "a" ]) None
              Expect.equal failed.Status (RenderFailed "boom") "failed"
              Expect.equal failed.Pages first.Pages "the failure keeps the pages"
              Expect.equal (failed.Version, recovered.Version) (2L, 3L) "versions"
              Expect.equal recovered.Status (Rendered (1, ms 10.0)) "recovered"
          }
          test "an unchanged error does not bump" {
              let failed = start () |> Snapshot.render (Failure "boom") None
              let again = failed |> Snapshot.render (Failure "boom") None
              Expect.equal again.Version failed.Version "no bump"
          }
          test "a new hint bumps" {
              let first =
                  start ()
                  |> Snapshot.render (rendered [ "a" ]) None

              let hinted =
                  first
                  |> Snapshot.render (rendered [ "a" ]) (Some "hint")

              Expect.equal (hinted.Version, hinted.Hint) (2L, Some "hint") "hint"
          }
          test "withStatus bumps on a change only" {
              let failed =
                  start ()
                  |> Snapshot.withStatus (ReloadFailed "down")

              let again =
                  failed
                  |> Snapshot.withStatus (ReloadFailed "down")

              Expect.equal (failed.Version, again.Version) (1L, 1L) "versions"
          }
          test "the hash is 16 hexadecimal digits" { Expect.isMatch (Snapshot.hash "page") "^[0-9a-f]{16}$" "hash" }
          test "the JSON has the fields Shell.html reads" {
              let shell = shell ()

              let read (prefix: string) =
                  [ for m in Regex.Matches (shell, $@"\b{prefix}\.(\w+)") -> m.Groups[1].Value ]
                  |> Set.ofList

              let snapshotFields = read "snap"
              let statusFields = read "status"
              Expect.isNonEmpty snapshotFields "the shell reads snapshot fields"
              Expect.isNonEmpty statusFields "the shell reads status fields"

              let diagnostic =
                  { File = "a.fsx"
                    Line = 1
                    Column = 2
                    Message = "m" }

              let statuses =
                  [ Starting
                    Rendered (2, ms 5.0)
                    RenderFailed "e"
                    CompileFailed [ diagnostic ]
                    ReloadFailed "r" ]

              let documents =
                  [ for status in statuses ->
                        let snapshot =
                            { start () with
                                Status = status
                                Hint = Some "h"
                                Pages = [ "a" ]
                                Hashes = [ Snapshot.hash "a" ] }

                        (JsonDocument.Parse (Snapshot.json snapshot)).RootElement ]

              for root in documents do
                  Expect.equal (Set.difference snapshotFields (names root)) Set.empty "snapshot fields"

              let statusNames =
                  documents
                  |> List.map (fun root -> names (root.GetProperty "status"))
                  |> Set.unionMany

              Expect.equal (Set.difference statusFields statusNames) Set.empty "status fields"
              let root = documents[3]

              let firstPage =
                  (root.GetProperty "pages").EnumerateArray ()
                  |> Seq.head

              Expect.equal (firstPage.GetString ()) (Snapshot.hash "a") "pages are hashes"

              let diagnostic =
                  (root.GetProperty("status").GetProperty "diagnostics").EnumerateArray ()
                  |> Seq.head

              Expect.equal ((diagnostic.GetProperty "line").GetInt32 ()) 1 "line"

              Expect.equal (root.GetProperty("instance").GetString ()) "abc" "instance"
              Expect.equal (root.GetProperty("reload").GetString ()) "manual" "reload"
          } ]
