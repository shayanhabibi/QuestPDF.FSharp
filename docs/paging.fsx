(**
---
category: Guide
categoryindex: 1
index: 6
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
# Paging

## Page setup

A `page` list holds the page settings and slots. QuestPDF repeats the page definition over as many pages as its
content needs.

| Part | Sets |
|------|------|
| `Page.size`, `Page.sizeOf`, `Page.minSize`, `Page.maxSize` | the page size |
| `Page.continuous width` | one page as tall as its content |
| `Page.margin`, `Page.marginV`, `Page.marginH`, `Page.marginTop`, ... | the margins |
| `Page.color`, `Page.textStyle` | the page colour and default text style |
| `Page.header`, `Page.content`, `Page.footer` | the slots, header and footer repeated on each page |
| `Page.background`, `Page.foreground` | layers under and over the whole page |
| `Page.rightToLeft`, `Page.leftToRight` | the content direction |

A page takes one of each slot: two `Page.content` parts fail at generation with `DocumentComposeException`.
*)

open FSharp.QuestPDF

let paragraphs = [ for i in 1..14 do text $"Paragraph {i}: some content that flows from page to page." ]

let report =
    document [
        page [
            Page.sizeOf 260 200
            Page.margin 12
            Page.textStyle (Style.size 9)
            Page.header (paddingBottom 4 >> styledText (Style.bold >> Style.color Colors.Blue.Darken2) "Report")
            Page.content (columnSpaced 4 paragraphs)
            Page.footer (richText [ Text.alignCenter; Text.pageNumber; Text.span " / "; Text.totalPages ])
        ]
    ]

(*** hide ***)
render report
(*** include-it-raw ***)

(**
`Page.background` and `Page.foreground` take content that covers the whole page, margins included. `Page.color` is
a flat page colour; a background made of modifiers only ends in `empty`, the content that draws nothing:
*)

let framed =
    document [
        page [
            Page.sizeOf 200 70
            Page.margin 14
            Page.background (padding 5 >> border 1 >> borderColor Colors.Amber.Darken2 >> background Colors.Amber.Lighten5 >> empty)
            Page.content (text "A page with a framed background.")
        ]
    ]

(*** hide ***)
render framed
(*** include-it-raw ***)

(**
## Content across pages

These modifiers control how content behaves at page boundaries:

| Modifier | Effect |
|----------|--------|
| `showOnce` | draws the content on the first page it appears on only |
| `skipOnce` | skips the content on the first page it appears on |
| `repeat` | repeats the content on every page its container spans |
| `ensureSpace h` | moves the content to the next page when less than `h` is left and the content does not fit whole; `h` is a length |
| `preventPageBreak`, `showEntire` | keeps the content together on one page |
| `showWhen predicate` | draws the content when the predicate holds for the page |
| `showIf condition` | draws the content when the condition holds |
| `pageBreak` | content that ends the page |
*)

let isEven (context: QuestPDF.Elements.ShowIfContext) = context.PageNumber % 2 = 0

let flow =
    document [
        page [
            Page.sizeOf 220 120
            Page.margin 10
            Page.header (column [ showOnce >> styledText Style.bold "First page only"; skipOnce >> text "(continued)" ])
            Page.content (column [ text "Page one."; pageBreak; text "Page two."; pageBreak; text "Page three." ])
            Page.footer (showWhen isEven >> styledText (Style.color Colors.Grey.Darken1) "even page")
        ]
    ]

(*** hide ***)
render flow
(*** include-it-raw ***)

(**
## Deferred and dynamic content

`lazyContent` builds its content when the layout reaches it and releases it after drawing, which lowers the memory
use of documents with thousands of pages; `lazyContentCached` keeps the built layout, trading memory for speed.
`capturePosition id` records where content lands on each page. `Dynamic.ofComponent` and `Dynamic.ofStateful` draw
a QuestPDF `IDynamicComponent`, composed anew for each page, and `Dynamic.createElement` and `Dynamic.element` build
and draw its elements from wrapper content.

## Several page definitions

A document lists pages in order; each `page` starts on a new sheet with its own settings, for example a cover
followed by the body:
*)

let withCover =
    document [
        page [ Page.sizeOf 150 100; Page.color Colors.Indigo.Darken3; Page.content (alignCenter >> alignMiddle >> styledText (Style.size 16 >> Style.color Colors.White) "Cover") ]
        page [ Page.sizeOf 150 100; Page.margin 10; Page.content (text "The body.") ]
    ]

(*** hide ***)
render withCover
(*** include-it-raw ***)
