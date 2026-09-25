module FSharp.QuestPDF.Tests.LayersDecorationTests

open Expecto
open QuestPDF.Drawing.Exceptions
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open FSharp.QuestPDF
open FSharp.QuestPDF.Tests.Support

/// Eighty numbered lines: enough to fill more than one A5 page.
let private longColumn: Content = column [ for i in 1..80 -> text $"line {i}" ]

let private rawLongColumn (c: IContainer) =
    c.Column (fun col ->
        for i in 1..80 do
            col.Item().Text ($"line {i}") |> ignore)

[<Tests>]
let tests =
    testList
        "Layers and decoration"
        [ equivalent
              "layers"
              (layers
                  [ Layers.layer (background Colors.Grey.Lighten3 >> text "under")
                    Layers.primary (padding 10 >> text "main")
                    Layers.layer (alignRight >> text "over") ])
              (fun c ->
                  c.Layers (fun l ->
                      l.Layer().Background(Colors.Grey.Lighten3).Text ("under")
                      |> ignore

                      l.PrimaryLayer().Padding(10f).Text ("main")
                      |> ignore

                      l.Layer().AlignRight().Text ("over") |> ignore))
          distinct
              "layer order changes the output"
              (layers
                  [ Layers.primary (padding 10 >> text "main")
                    Layers.layer (background Colors.Grey.Lighten3 >> text "under") ])
              (fun c ->
                  c.Layers (fun l ->
                      l.Layer().Background(Colors.Grey.Lighten3).Text ("under")
                      |> ignore

                      l.PrimaryLayer().Padding(10f).Text ("main")
                      |> ignore))
          test "layers without a primary layer throw at generation" {
              configure ()
              let document = wrapContent (layers [ Layers.layer (text "only") ])

              Expect.throwsT<DocumentComposeException> (fun () -> Pdf.bytes document |> ignore) "a primary layer is required"
          }
          equivalent
              "decoration"
              (decoration
                  [ Decoration.before (text "before")
                    Decoration.content (padding 5 >> text "content")
                    Decoration.after (text "after") ])
              (fun c ->
                  c.Decoration (fun d ->
                      d.Before().Text ("before") |> ignore
                      d.Content().Padding(5f).Text ("content") |> ignore
                      d.After().Text ("after") |> ignore))
          distinct "decoration slots differ" (decoration [ Decoration.after (text "x"); Decoration.content (text "content") ]) (fun c ->
              c.Decoration (fun d ->
                  d.Before().Text ("x") |> ignore
                  d.Content().Text ("content") |> ignore))
          test "decoration repeats before and after on every page" {
              configure ()

              let pdf =
                  Pdf.bytes (
                      wrapContent (
                          decoration
                              [ Decoration.before (text "BEFORE")
                                Decoration.content longColumn
                                Decoration.after (text "AFTER") ]
                      )
                  )

              let texts = pageTexts pdf
              Expect.isGreaterThan texts.Length 1 "the content spans pages"
              Expect.all texts (fun t -> t.StartsWith "BEFORE" && t.EndsWith "AFTER") "before and after frame every page"
          }
          equivalent "decoration across pages" (decoration [ Decoration.before (text "BEFORE"); Decoration.content longColumn ]) (fun c ->
              c.Decoration (fun d ->
                  d.Before().Text ("BEFORE") |> ignore
                  rawLongColumn (d.Content ()))) ]
