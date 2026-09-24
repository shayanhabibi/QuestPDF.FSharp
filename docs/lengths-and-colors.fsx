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
# Lengths and colours

## Bare numbers and units

Every length argument accepts an `int`, a `float` or a `float32`, read as points (1/72 inch), or a `Length` made
by multiplying a number by a unit: `pt`, `mm`, `cm`, `inch`, `mil` or `feet`.
*)

open QuestPDF.FSharp

let a = padding 10          // 10 pt
let b = padding 2.5         // 2.5 pt
let c = padding (5 * mm)    // 5 mm
let d = padding (0.5 * cm)  // 0.5 cm

(**
The unit passes through to QuestPDF unconverted: `padding (5 * mm)` calls `Padding(5f, Unit.Millimetre)`, so the
output is byte-identical to the fluent call. `Length.points` converts a length to points with QuestPDF's own factors
when a computation needs one number:
*)

let twoCentimetres = Length.points (2 * cm)

(*** include-value: twoCentimetres ***)

(**
`len` turns any accepted value into a `Length`, for functions of your own that take lengths:
*)

let gap = len 4
let indent = len (1 * cm)

(**
### Why not units of measure

Units of measure (`5.0<mm>`) exist only at compile time. QuestPDF takes a runtime `Unit`, and the conversion to
points is QuestPDF's job, so a `Length` record carries the value and the unit to the call. Bare numbers stay
bare: a length never needs a `<pt>` annotation.

## Colours

`Colors` is QuestPDF's Material palette. `Color.hex`, `Color.rgb`, `Color.argb` and `Color.withAlpha` build other
colours.
*)

let swatch (color: Color) (label: string) =
    width 60 >> height 40 >> background color >> alignCenter >> alignMiddle >> styledText (Style.size 8) label

let swatches =
    row [
        Row.spacing 6
        Row.auto (swatch Colors.Teal.Medium "Teal.Medium")
        Row.auto (swatch Colors.Amber.Lighten2 "Amber.Lighten2")
        Row.auto (swatch (Color.hex "#3366CC") "#3366CC")
        Row.auto (swatch (Color.rgb 200uy 60uy 90uy) "rgb")
        Row.auto (swatch (Color.withAlpha 0.25 Colors.Red.Medium) "alpha 0.25")
    ]

let colorDemo = document [ page [ Page.sizeOf 360 70; Page.margin 10; Page.content swatches ] ]

(*** hide ***)
render colorDemo
(*** include-it-raw ***)

(**
## Page sizes

`PageSizes` has the ISO, US and other standard sizes. `PageSize.landscape` and `PageSize.portrait` turn one,
`PageSize.custom` makes one from two lengths, and `Page.sizeOf width height` sets a size directly:
*)

let landscapeA5 = page [ Page.size (PageSize.landscape PageSizes.A5); Page.content (text "A5, landscape") ]
let postcard = page [ Page.size (PageSize.custom (148 * mm) (105 * mm)); Page.content (text "A postcard") ]
let banner = page [ Page.sizeOf (20 * cm) (5 * cm); Page.content (text "A banner") ]
