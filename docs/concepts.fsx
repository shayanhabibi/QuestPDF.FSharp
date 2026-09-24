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
# Concepts

## Slots, content and modifiers

QuestPDF builds a page by filling containers. QuestPDF.FSharp names the three roles:

| Type | Definition | Role |
|------|------------|------|
| `Slot` | a one-field struct over `IContainer` | a place that content fills |
| `Content` | `Slot -> unit` | fills a slot: `text "x"`, `column [...]`, `lineH 1 c` |
| `Modifier` | `Slot -> Slot` | wraps a slot: `padding 10`, `background c`, `alignCenter` |

`>>` composes them. A chain of modifiers ending in content is content, and it reads outer to inner, exactly like
the fluent chain `c.Padding(10).Background(c).Text("x")`:
*)

open QuestPDF.FSharp

let badge = padding 10 >> background Colors.Blue.Lighten4 >> text "A badge"

(**
`badge` needs no type annotation: `Slot` is a concrete struct, so the value is not generic and F# accepts it at
the top level of a module.

## Modifier order is layout order

Each modifier wraps everything after it. Padding before the background leaves a white margin around the blue box;
the background before the padding paints the padded area too:
*)

let paddingFirst = padding 10 >> background Colors.Blue.Lighten3 >> text "padding >> background"
let backgroundFirst = background Colors.Blue.Lighten3 >> padding 10 >> text "background >> padding"

let orderDemo =
    document [
        page [
            Page.sizeOf 300 120
            Page.margin 10
            Page.content (column [ border 0.5 >> paddingFirst; border 0.5 >> backgroundFirst ])
        ]
    ]

(*** hide ***)
render orderDemo
(*** include-it-raw ***)

(**
## Parts lists

Containers take lists: `column`, `row`, `table`, `layers`, `decoration`, `richText`, `page` and `document`.
List expressions work inside them, so data drives the layout without builder syntax:
*)

let items = [ "Apples", 3; "Pears", 0; "Plums", 12 ]

let stock =
    column [
        styledText Style.bold "Stock"
        for name, count in items do
            if count > 0 then
                text $"{name}: {count}"
            else
                styledText (Style.color Colors.Red.Medium) $"{name}: sold out"
    ]

let stockDemo = document [ page [ Page.sizeOf 200 110; Page.margin 10; Page.content stock ] ]

(*** hide ***)
render stockDemo
(*** include-it-raw ***)

(**
A row, table, layers or decoration list takes parts made by its module (`Row.fill`, `Table.cell`, `Layers.primary`,
...). Each part is a function over the QuestPDF descriptor, so a lambda over the descriptor is a valid part too;
[Interop](interop.html) shows the forms.

## Values, not builders

`Content`, `Modifier` and `Style` are functions, so ordinary F# composes them: a function that takes a string and
returns content is a reusable component.
*)

let heading (title: string) =
    paddingBottom 4 >> borderBottom 1 >> borderColor Colors.Grey.Darken1 >> styledText (Style.size 14 >> Style.semiBold) title

let section title body = column [ heading title; paddingTop 4 >> text body ]

let componentDemo =
    document [
        page [
            Page.sizeOf 300 130
            Page.margin 10
            Page.content (columnSpaced 8 [ section "First" "Some text."; section "Second" "More text." ])
        ]
    ]

(*** hide ***)
render componentDemo
(*** include-it-raw ***)
