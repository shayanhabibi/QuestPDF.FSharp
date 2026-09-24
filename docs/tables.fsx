(**
---
category: Guide
categoryindex: 1
index: 5
---
*)
(*** hide ***)
#r "nuget: QuestPDF, 2026.9.0"
#r "../src/QuestPDF.FSharp/bin/Release/net10.0/QuestPDF.FSharp.dll"

open System
open QuestPDF.FSharp

License.community ()
Font.useSystemFonts false
Font.strict true
Font.registerDirectory (IO.Path.Combine (__SOURCE_DIRECTORY__, "..", "fonts"))

/// The pages of a document as PNG images at 96 dpi, in HTML img elements.
let render (document: QuestPDF.Infrastructure.IDocument) =
    Pdf.images ImageFormat.Png 96 document
    |> List.map (fun png ->
        "<img class=\"page-render\" alt=\"A rendered page\" style=\"max-width: 100%; border: 1px solid #ccc; margin: 4px;\" src=\"data:image/png;base64,"
        + Convert.ToBase64String png
        + "\" />")
    |> String.concat "\n"

(**
# Tables

A `table` list holds the column definitions, an optional header and footer, and the cells.

## Columns and cells

`Table.columns` takes `Table.constant` and `Table.relative` columns. Cells fill the columns left to right, top to
bottom; `Table.cells` adds a list of them, and `Table.cell` makes one.
*)

open QuestPDF.FSharp

let cellBox = borderBottom 0.5 >> borderColor Colors.Grey.Lighten1 >> padding 4
let head label = Table.cell (background Colors.Grey.Lighten3 >> padding 4 >> styledText Style.semiBold label)
let cell label = Table.cell (cellBox >> text label)
let number (value: decimal) = Table.cell (cellBox >> alignRight >> text (value.ToString "N2"))

let prices = [ "Tea", 3.50m; "Coffee", 4.20m; "Cake", 5.00m ]

let priceTable =
    table [
        Table.columns [ Table.relative 3; Table.constant (25 * mm) ]
        Table.header [ head "Item"; head "Price" ]
        for name, price in prices do
            Table.cells [ cell name; number price ]
        Table.footer [ cell "Total"; number (List.sumBy snd prices) ]
    ]

let priceDemo = document [ page [ Page.sizeOf 260 140; Page.margin 10; Page.content priceTable ] ]

(*** hide ***)
render priceDemo
(*** include-it-raw ***)

(**
The header and footer repeat on every page the table spans. A cell value is plain data, so the same `head` or
`cell` can be used in the header, the body and the footer.

## Spans and placement

`Table.cellWith` takes cell options: `Cell.columnSpan`, `Cell.rowSpan`, and `Cell.at row column` to place a cell
explicitly (both 1-based).
*)

let spans =
    table [
        Table.columns [ Table.relative 1; Table.relative 1; Table.relative 1 ]
        Table.cells [
            Table.cellWith [ Cell.columnSpan 2 ] (background Colors.Green.Lighten3 >> padding 4 >> text "two columns")
            Table.cellWith [ Cell.rowSpan 2 ] (background Colors.Green.Lighten2 >> padding 4 >> text "two rows")
            cell "a"
            cell "b"
            Table.cellWith [ Cell.at 3 2 ] (background Colors.Green.Lighten4 >> padding 4 >> text "row 3, column 2")
        ]
    ]

let spanDemo = document [ page [ Page.sizeOf 300 110; Page.margin 10; Page.content spans ] ]

(*** hide ***)
render spanDemo
(*** include-it-raw ***)

(**
A cell placed where it cannot fit fails when the document is generated. `Table.extendLastCellsToBottom` stretches
the last row to the bottom of the page. The [invoice recipe](recipes/invoice.html) is a complete multi-page table.
*)
