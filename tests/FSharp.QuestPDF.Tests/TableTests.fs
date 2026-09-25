module FSharp.QuestPDF.Tests.TableTests

open Expecto
open QuestPDF.Fluent
open QuestPDF.Infrastructure
open FSharp.QuestPDF
open FSharp.QuestPDF.Tests.Support

/// A bordered cell body, so the extent of every cell is visible.
let private boxed (label: string) : Content =
    border 0.5 >> padding 2 >> text label

let private rawBoxed (label: string) (c: IContainer) =
    c.Border(0.5f).Padding(2f).Text (label) |> ignore

/// Two relative columns, the table part under test and four body cells.
let private tableWith (part: TablePart) : Content =
    table
        [ Table.columns [ Table.relative 1; Table.relative 1 ]
          part
          Table.cells [ for label in [ "a"; "b"; "c"; "d" ] -> Table.cell (boxed label) ] ]

let private rawTableWith (part: TableDescriptor -> unit) (c: IContainer) =
    c.Table (fun t ->
        t.ColumnsDefinition (fun d ->
            d.RelativeColumn (1f)
            d.RelativeColumn (1f))

        part t

        for label in [ "a"; "b"; "c"; "d" ] do
            rawBoxed label (t.Cell ()))

/// A tall cell spanning three rows beside two short cells, so the second column ends above the first.
let private spannedWith (part: TablePart) : Content =
    table
        [ Table.columns [ Table.relative 1; Table.relative 1 ]
          part
          Table.cells
              [ Table.cellWith [ Cell.rowSpan 3 ] (border 0.5 >> height 90 >> text "tall")
                Table.cell (boxed "b")
                Table.cell (boxed "c") ] ]

let private rawSpannedWith (part: TableDescriptor -> unit) (c: IContainer) =
    c.Table (fun t ->
        t.ColumnsDefinition (fun d ->
            d.RelativeColumn (1f)
            d.RelativeColumn (1f))

        part t

        t.Cell().RowSpan(3u).Border(0.5f).Height(90f).Text ("tall")
        |> ignore

        rawBoxed "b" (t.Cell ())
        rawBoxed "c" (t.Cell ()))

/// The column forms, each with its raw counterpart, beside a relative column of weight 1.
let private columnForms: (string * ColumnDef * (TableColumnsDefinitionDescriptor -> unit)) list =
    [ "relative int", Table.relative 3, (fun d -> d.RelativeColumn (3f))
      "relative float", Table.relative 0.5, (fun d -> d.RelativeColumn (0.5f))
      "constant int", Table.constant 50, (fun d -> d.ConstantColumn (50f))
      "constant float", Table.constant 60.5, (fun d -> d.ConstantColumn (60.5f))
      "constant mm", Table.constant (30 * mm), (fun d -> d.ConstantColumn (30f, Unit.Millimetre)) ]

[<Tests>]
let tests =
    testList
        "Table"
        [ testList
              "columns"
              [ for name, def, rawDef in columnForms do
                    equivalent
                        name
                        (table
                            [ Table.columns [ def; Table.relative 1 ]
                              Table.cells [ Table.cell (boxed "x"); Table.cell (boxed "y") ] ])
                        (fun c ->
                            c.Table (fun t ->
                                t.ColumnsDefinition (fun d ->
                                    rawDef d
                                    d.RelativeColumn (1f))

                                rawBoxed "x" (t.Cell ())
                                rawBoxed "y" (t.Cell ()))) ]
          distinct
              "column widths change the output"
              (table
                  [ Table.columns [ Table.relative 3; Table.relative 1 ]
                    Table.cells [ Table.cell (boxed "x"); Table.cell (boxed "y") ] ])
              (rawTableWith ignore)
          equivalent "body cells are placed in order" (tableWith ignore) (rawTableWith ignore)
          equivalent
              "one cell value in the header, body and footer"
              (let shared = Table.cell (boxed "same")

               table
                   [ Table.columns [ Table.relative 1 ]
                     Table.header [ shared ]
                     Table.cells [ shared; shared ]
                     Table.footer [ shared ] ])
              (fun c ->
                  c.Table (fun t ->
                      t.ColumnsDefinition (fun d -> d.RelativeColumn (1f))
                      t.Header (fun h -> rawBoxed "same" (h.Cell ()))
                      rawBoxed "same" (t.Cell ())
                      rawBoxed "same" (t.Cell ())
                      t.Footer (fun f -> rawBoxed "same" (f.Cell ()))))
          equivalent
              "columnSpan"
              (tableWith (Table.cells [ Table.cellWith [ Cell.columnSpan 2 ] (boxed "wide") ]))
              (rawTableWith (fun t -> rawBoxed "wide" (t.Cell().ColumnSpan (2u))))
          equivalent
              "rowSpan"
              (tableWith (Table.cells [ Table.cellWith [ Cell.rowSpan 2 ] (boxed "tall") ]))
              (rawTableWith (fun t -> rawBoxed "tall" (t.Cell().RowSpan (2u))))
          equivalent
              "at"
              (tableWith (Table.cells [ Table.cellWith [ Cell.at 3 2 ] (boxed "placed") ]))
              (rawTableWith (fun t -> rawBoxed "placed" (t.Cell().Row(3u).Column (2u))))
          equivalent
              "options combine"
              (tableWith (Table.cells [ Table.cellWith [ Cell.at 3 1; Cell.columnSpan 2; Cell.rowSpan 2 ] (boxed "block") ]))
              (rawTableWith (fun t -> rawBoxed "block" (t.Cell().Row(3u).Column(1u).ColumnSpan(2u).RowSpan (2u))))
          distinct
              "cell options change the output"
              (tableWith (Table.cells [ Table.cellWith [ Cell.columnSpan 2 ] (boxed "wide") ]))
              (rawTableWith (fun t -> rawBoxed "wide" (t.Cell ())))
          test "Cell options are data" {
              Expect.equal (Cell.columnSpan 2) (CellOption.ColumnSpan 2) "columnSpan"
              Expect.equal (Cell.rowSpan 3) (CellOption.RowSpan 3) "rowSpan"
              Expect.equal (Cell.at 1 2) (CellOption.At (1, 2)) "at"
              Expect.equal (Table.cellWith [ Cell.rowSpan 2 ] empty).Options [ CellOption.RowSpan 2 ] "cellWith keeps the options"
              Expect.isEmpty (Table.cell empty).Options "cell has no options"
          }
          test "Cell options are written through the Cell module" {
              Expect.isTrue
                  (typeof<CellOption>.IsDefined (typeof<RequireQualifiedAccessAttribute>, false))
                  "the CellOption cases need the CellOption qualifier"
          }
          equivalent
              "extendLastCellsToBottom"
              (spannedWith Table.extendLastCellsToBottom)
              (rawSpannedWith (fun t -> t.ExtendLastCellsToTableBottom ()))
          distinct "extendLastCellsToBottom changes the output" (spannedWith Table.extendLastCellsToBottom) (rawSpannedWith ignore)
          test "the header repeats on every page" {
              configure ()

              let pdf =
                  Pdf.bytes (
                      wrapContent (
                          table
                              [ Table.columns [ Table.relative 1 ]
                                Table.header [ Table.cell (text "HEAD") ]
                                Table.cells [ for i in 1..80 -> Table.cell (text $"row {i}") ] ]
                      )
                  )

              let texts = pageTexts pdf
              Expect.isGreaterThan texts.Length 1 "the table spans pages"
              Expect.all texts (fun t -> t.Contains "HEAD") "every page starts with the header"
          } ]
