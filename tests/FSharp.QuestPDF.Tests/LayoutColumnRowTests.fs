module FSharp.QuestPDF.Tests.LayoutColumnRowTests

open Expecto
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open FSharp.QuestPDF
open FSharp.QuestPDF.Tests.Support

/// A grey cell, so the extent of each column item and row item is visible.
let private cell (label: string) : Content =
    background Colors.Grey.Lighten3 >> text label

let private rawCell (label: string) (c: IContainer) =
    c.Background(Colors.Grey.Lighten3).Text (label)
    |> ignore

let private rawColumn (before: ColumnDescriptor -> unit) (c: IContainer) =
    c.Column (fun col ->
        before col

        for label in [ "a"; "b"; "c" ] do
            rawCell label (col.Item ()))

let private items = [ cell "a"; cell "b"; cell "c" ]

let private rawRow (build: RowDescriptor -> unit) (c: IContainer) =
    c.Row (fun r -> build r)

/// The row forms, each with its raw counterpart, between a 40 pt item and a relative item of weight 1.
let private rowForms: (string * RowPart * (RowDescriptor -> IContainer)) list =
    [ "fill", Row.fill (cell "x"), (fun r -> r.RelativeItem ())
      "relative int", Row.relative 3 (cell "x"), (fun r -> r.RelativeItem (3f))
      "relative float", Row.relative 0.5 (cell "x"), (fun r -> r.RelativeItem (0.5f))
      "constant int", Row.constant 50 (cell "x"), (fun r -> r.ConstantItem (50f))
      "constant float", Row.constant 60.5 (cell "x"), (fun r -> r.ConstantItem (60.5f))
      "constant mm", Row.constant (30 * mm) (cell "x"), (fun r -> r.ConstantItem (30f, Unit.Millimetre))
      "auto", Row.auto (cell "x"), (fun r -> r.AutoItem ()) ]

[<Tests>]
let tests =
    testList
        "Column and row"
        [ equivalent "column" (column items) (rawColumn ignore)
          equivalent "empty column" (column []) (fun c -> c.Column ignore)
          equivalent "columnSpaced int" (columnSpaced 10 items) (rawColumn (fun col -> col.Spacing (10f)))
          equivalent "columnSpaced float" (columnSpaced 7.5 items) (rawColumn (fun col -> col.Spacing (7.5f)))
          equivalent "columnSpaced mm" (columnSpaced (3 * mm) items) (rawColumn (fun col -> col.Spacing (3f, Unit.Millimetre)))
          equivalent "columnSpaced 0 is a column without spacing" (columnSpaced 0 items) (rawColumn ignore)
          distinct "columnSpaced changes the output" (columnSpaced 10 items) (rawColumn ignore)
          equivalent
              "column items from a list expression"
              (column
                  [ for label in [ "a"; "b"; "c" ] do
                        if label <> "b" then
                            cell label ])
              (fun c ->
                  c.Column (fun col ->
                      rawCell "a" (col.Item ())
                      rawCell "c" (col.Item ())))
          testList
              "row"
              [ for name, part, rawItem in rowForms do
                    equivalent
                        name
                        (row [ Row.constant 40 (cell "k"); part; Row.fill (cell "y") ])
                        (rawRow (fun r ->
                            rawCell "k" (r.ConstantItem (40f))
                            rawCell "x" (rawItem r)
                            rawCell "y" (r.RelativeItem ())))
                test "the row forms render differently" {
                    configure ()

                    let rendered =
                        [ for name, _, rawItem in rowForms ->
                              name,
                              (rawContent (
                                  rawRow (fun r ->
                                      rawCell "k" (r.ConstantItem (40f))
                                      rawCell "x" (rawItem r)
                                      rawCell "y" (r.RelativeItem ()))
                              ))
                                  .GeneratePdf () ]

                    let collisions =
                        [ for a, x in rendered do
                              for b, y in rendered do
                                  if a < b && x = y then
                                      yield a, b ]

                    Expect.isEmpty collisions "every row form differs from every other"
                }
                equivalent "fill is relative 1" (row [ Row.fill (cell "a"); Row.relative 2 (cell "b") ]) (fun c ->
                    c.Row (fun r ->
                        rawCell "a" (r.RelativeItem (1f))
                        rawCell "b" (r.RelativeItem (2f))))
                equivalent "spacing int" (row [ Row.spacing 12; Row.fill (cell "a"); Row.fill (cell "b") ]) (fun c ->
                    c.Row (fun r ->
                        r.Spacing (12f)
                        rawCell "a" (r.RelativeItem ())
                        rawCell "b" (r.RelativeItem ())))
                equivalent "spacing float" (row [ Row.spacing 8.5; Row.fill (cell "a"); Row.fill (cell "b") ]) (fun c ->
                    c.Row (fun r ->
                        r.Spacing (8.5f)
                        rawCell "a" (r.RelativeItem ())
                        rawCell "b" (r.RelativeItem ())))
                equivalent "spacing mm" (row [ Row.spacing (4 * mm); Row.fill (cell "a"); Row.fill (cell "b") ]) (fun c ->
                    c.Row (fun r ->
                        r.Spacing (4f, Unit.Millimetre)
                        rawCell "a" (r.RelativeItem ())
                        rawCell "b" (r.RelativeItem ())))
                distinct "spacing changes the output" (row [ Row.spacing 12; Row.fill (cell "a"); Row.fill (cell "b") ]) (fun c ->
                    c.Row (fun r ->
                        rawCell "a" (r.RelativeItem ())
                        rawCell "b" (r.RelativeItem ())))
                equivalent "a hand-written part is a row part" (row [ (fun r -> r.AutoItem().Text ("raw") |> ignore); Row.fill (cell "a") ]) (fun c ->
                    c.Row (fun r ->
                        r.AutoItem().Text ("raw") |> ignore
                        rawCell "a" (r.RelativeItem ()))) ] ]
