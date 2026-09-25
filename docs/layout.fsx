(**
---
category: Guide
categoryindex: 1
index: 4
---
*)
(*** hide ***)
#r "nuget: QuestPDF, 2026.9.0"
#r "../src/FSharp.QuestPDF/bin/Release/net10.0/FSharp.QuestPDF.dll"

open System
open FSharp.QuestPDF

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
# Layout

## Columns

`column` stacks content vertically; `columnSpaced` adds a gap between items.
*)

open FSharp.QuestPDF

let box (color: Color) (label: string) = background color >> padding 6 >> text label

let stacked =
    columnSpaced 4 [
        box Colors.Blue.Lighten3 "first"
        box Colors.Blue.Lighten2 "second"
        box Colors.Blue.Lighten1 "third"
    ]

let columnDemo = document [ page [ Page.sizeOf 200 110; Page.margin 10; Page.content stacked ] ]

(*** hide ***)
render columnDemo
(*** include-it-raw ***)

(**
## Rows

`row` places items side by side. Each item says how it takes width:

| Part | Width |
|------|-------|
| `Row.constant w c` | a fixed length |
| `Row.auto c` | the width of its content |
| `Row.relative n c` | `n` shares of the remaining width |
| `Row.fill c` | one share of the remaining width (`Row.relative 1`) |
| `Row.spacing s` | the gap between items |
*)

let widths =
    row [
        Row.spacing 4
        Row.constant (20 * mm) (box Colors.Amber.Lighten3 "20 mm")
        Row.auto (box Colors.Amber.Lighten2 "auto")
        Row.fill (box Colors.Amber.Lighten1 "fill")
        Row.relative 2 (box Colors.Amber.Medium "relative 2")
    ]

let rowDemo = document [ page [ Page.sizeOf 360 60; Page.margin 10; Page.content widths ] ]

(*** hide ***)
render rowDemo
(*** include-it-raw ***)

(**
## Layers

`layers` draws content on top of each other, in list order: a later layer draws over an earlier one. Exactly one
layer is `Layers.primary`: it sets the size and takes part in paging.
*)

let stamped =
    layers [
        Layers.primary (padding 10 >> text (String.replicate 8 "Layered content under a stamp. "))
        Layers.layer (alignCenter >> alignMiddle >> rotate (-20) >> styledText (Style.size 28 >> Style.bold >> Style.color Colors.Red.Lighten1) "DRAFT")
    ]

let layersDemo = document [ page [ Page.sizeOf 300 130; Page.margin 10; Page.content stamped ] ]

(*** hide ***)
render layersDemo
(*** include-it-raw ***)

(**
A `layers` list without `Layers.primary` fails when the document is generated, not when it compiles.

## Decoration

`decoration` puts content before and after a main body. When the body spans pages, the before and after parts
repeat on each page, so a table heading or a running caption stays with its content.
*)

let decorated =
    decoration [
        Decoration.before (background Colors.Grey.Lighten2 >> padding 4 >> styledText Style.semiBold "Before")
        Decoration.content (padding 4 >> column [ for i in 1..3 do text $"Body line {i}" ])
        Decoration.after (borderTop 0.5 >> padding 4 >> styledText (Style.size 8) "After")
    ]

let decorationDemo = document [ page [ Page.sizeOf 220 130; Page.margin 10; Page.content decorated ] ]

(*** hide ***)
render decorationDemo
(*** include-it-raw ***)

(**
## Inlined items

`inlined` places items in a line, each as wide as its content, and wraps to a new line when the width runs out,
like words in a paragraph. `Inlined.spacing` sets both gaps (`spacingH` and `spacingV` set one each),
`Inlined.align*` distributes the items along a line, and `Inlined.baseline*` aligns them across it.
*)

let tags =
    inlined [
        Inlined.spacing 4
        Inlined.alignCenter
        for tag in [ "F#"; "QuestPDF"; "PDF"; "layout"; "functional"; "documents"; "wrapping" ] do
            Inlined.item (background Colors.Indigo.Lighten4 >> paddingH 6 >> paddingV 2 >> text tag)
    ]

let inlinedDemo = document [ page [ Page.sizeOf 200 90; Page.margin 10; Page.content tags ] ]

(*** hide ***)
render inlinedDemo
(*** include-it-raw ***)

(**
## Multiple columns

`multiColumn` flows its content through side-by-side columns, like a newspaper page: the content fills the first
column, then continues in the next. `MultiColumn.columns` sets the count (2 by default), `MultiColumn.spacing` the
gap, `MultiColumn.spacer` draws content in each gap, and `MultiColumn.balanceHeight` evens out the column heights.
*)

let article =
    multiColumn [
        MultiColumn.columns 2
        MultiColumn.spacing 12
        MultiColumn.balanceHeight
        MultiColumn.spacer (lineV 0.5 Colors.Grey.Medium)
        MultiColumn.content (
            columnSpaced 4 [ for i in 1..4 do text $"Paragraph {i}. The text flows down one column and on into the next." ]
        )
    ]

let multiColumnDemo = document [ page [ Page.sizeOf 300 120; Page.margin 10; Page.content article ] ]

(*** hide ***)
render multiColumnDemo
(*** include-it-raw ***)

(**
## Box modifiers

Beside padding and background, modifiers constrain and decorate a box: `width`, `height`, `minWidth`, `maxHeight`
and the rest; `alignLeft` ... `alignBottom`; `extend` and `shrink`; `aspectRatio`; `border` and its sides with
`borderColor`; `cornerRadius`; `backgroundGradient` and `borderGradient` (an angle in degrees and a list of
colours); `shadow` with `Shadow.offset`, `blur`, `spread` and `color`; and the transforms `rotate`, `scale`, `flipH`,
`flipV`, `offsetX` and `offsetY`.
*)

let boxes =
    row [
        Row.spacing 8
        Row.auto (width 60 >> height 40 >> border 1 >> borderColor Colors.Teal.Medium >> alignCenter >> alignMiddle >> text "border")
        Row.auto (width 60 >> height 40 >> cornerRadius 8 >> background Colors.Teal.Lighten3 >> alignCenter >> alignMiddle >> text "radius")
        Row.auto (width 60 >> height 40 >> background Colors.Teal.Lighten4 >> alignBottom >> alignRight >> text "corner")
        Row.auto (
            width 60
            >> height 40
            >> shadow [ Shadow.offset 2 2; Shadow.blur 4 ]
            >> backgroundGradient 90 [ Colors.Teal.Lighten4; Colors.Teal.Lighten1 ]
            >> alignCenter
            >> alignMiddle
            >> text "gradient"
        )
    ]

let boxDemo = document [ page [ Page.sizeOf 300 64; Page.margin 10; Page.content boxes ] ]

(*** hide ***)
render boxDemo
(*** include-it-raw ***)
