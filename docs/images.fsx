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
# Images

## Raster images

`Image.file path` and `Image.bytes data` draw a PNG, JPEG, WebP or other raster image; `Image.fileWith` and
`Image.bytesWith` take an option such as `Image.fitWidth`, `Image.fitHeight`, `Image.fitArea`,
`Image.fitUnproportionally`, `Image.dpi`, `Image.quality` or `Image.original`. A modifier sets the box the image
fills.

The logo here is itself a QuestPDF.FSharp page, rendered with `Pdf.images`:
*)

open QuestPDF.FSharp

let logo: byte[] =
    document [
        page [
            Page.sizeOf 120 60
            Page.content (background Colors.Teal.Medium >> alignCenter >> alignMiddle >> styledText (Style.size 20 >> Style.bold >> Style.color Colors.White) "LOGO")
        ]
    ]
    |> Pdf.images ImageFormat.Png 144
    |> List.head

let pictures =
    row [
        Row.spacing 10
        Row.constant 60 (Image.bytes logo)
        Row.constant 120 (Image.bytes logo)
        Row.constant 60 (height 60 >> Image.bytesWith Image.fitUnproportionally logo)
    ]

let imageDemo = document [ page [ Page.sizeOf 290 80; Page.margin 10; Page.content pictures ] ]

(*** hide ***)
render imageDemo
(*** include-it-raw ***)

(**
`Image.shared` draws a `QuestPDF.Infrastructure.Image` loaded once, for an image repeated on many pages. The name
`Image` is the wrapper module; the QuestPDF class keeps its full name.

## SVG

`Svg.text markup` draws SVG markup as vector graphics; `Svg.textWith` takes `Svg.fitWidth`, `Svg.fitHeight` or
`Svg.fitArea`.
*)

let chart =
    """<svg xmlns="http://www.w3.org/2000/svg" width="120" height="60" viewBox="0 0 120 60">
         <rect x="5" y="30" width="20" height="25" fill="#26a69a"/>
         <rect x="35" y="15" width="20" height="40" fill="#26a69a"/>
         <rect x="65" y="5" width="20" height="50" fill="#26a69a"/>
         <rect x="95" y="22" width="20" height="33" fill="#26a69a"/>
       </svg>"""

let svgDemo = document [ page [ Page.sizeOf 160 100; Page.margin 10; Page.content (Svg.text chart) ] ]

(*** hide ***)
render svgDemo
(*** include-it-raw ***)

(**
## Lines

`lineH thickness color` and `lineV thickness color` draw a horizontal or vertical line; `lineHWith` and
`lineVWith` take QuestPDF's line options, for example a dash pattern.
*)

let lines =
    columnSpaced 8 [
        lineH 1 Colors.Grey.Darken2
        lineH (1 * mm) Colors.Teal.Medium
        lineHWith 2 (fun line -> line.LineColor(Colors.Red.Medium).LineDashPattern [| 6f; 3f |])
        height 30 >> row [ Row.auto (lineV 1 Colors.Grey.Darken2); Row.fill (paddingLeft 6 >> text "beside a vertical line") ]
    ]

let lineDemo = document [ page [ Page.sizeOf 220 100; Page.margin 10; Page.content lines ] ]

(*** hide ***)
render lineDemo
(*** include-it-raw ***)
