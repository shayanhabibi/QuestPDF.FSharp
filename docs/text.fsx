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
# Text

## Plain and styled text

`text` draws a string in the default style; `styledText` applies a `Style` first. A `Style` is a function over
QuestPDF's text style, so `>>` composes styles the way it composes layout.
*)

open QuestPDF.FSharp

let title = Style.size 18 >> Style.bold >> Style.color Colors.Indigo.Darken2
let muted = Style.size 9 >> Style.italic >> Style.color Colors.Grey.Darken1

let plain =
    column [
        styledText title "A styled title"
        text "Body text in the page's default style."
        styledText muted "A muted note."
    ]

let plainDemo = document [ page [ Page.sizeOf 300 100; Page.margin 10; Page.content plain ] ]

(*** hide ***)
render plainDemo
(*** include-it-raw ***)

(**
`Page.textStyle` sets the default style of a page, and the `textStyle` modifier sets it for everything inside a
slot. `Style.none` is the identity, for a style chosen by a condition.

## Rich text

`richText` takes a list of text parts: spans, styled spans, links, page numbers and block settings such as
alignment.
*)

let rich =
    richText [
        Text.alignCenter
        Text.span "Plain, "
        Text.styled Style.bold "bold, "
        Text.styled (Style.italic >> Style.color Colors.Teal.Darken2) "italic teal, "
        Text.span "and "
        Text.link "a link" "https://www.questpdf.com" |> Text.withStyle (Style.underline >> Style.color Colors.Blue.Medium)
        Text.span "."
        Text.lineBreak
        Text.styled (Style.size 8) "A second line after Text.lineBreak."
    ]

let richDemo = document [ page [ Page.sizeOf 300 80; Page.margin 10; Page.content rich ] ]

(*** hide ***)
render richDemo
(*** include-it-raw ***)

(**
`Text.withStyle` styles any part after it is made, including links and page numbers. Applied twice, the outer style
applies first and the inner one overrides it.

## Page numbers

`Text.pageNumber` and `Text.totalPages` are parts, typically in a footer. `Text.formatPage` formats the numbers of a
part; the formatter receives `None` when the number is unknown.
*)

let roman (n: int) =
    [ 10, "x"; 9, "ix"; 5, "v"; 4, "iv"; 1, "i" ]
    |> List.fold (fun (rest, acc) (value, digits) -> rest % value, acc + String.replicate (rest / value) digits) (n, "")
    |> snd

let footer =
    richText [
        Text.alignRight
        Text.span "Page "
        Text.pageNumber |> Text.formatPage (Option.map roman >> Option.defaultValue "?")
        Text.span " of "
        Text.totalPages |> Text.formatPage (Option.map roman >> Option.defaultValue "?")
    ]

let numbered =
    document [
        page [
            Page.sizeOf 220 90
            Page.margin 10
            Page.content (column [ text "First page"; pageBreak; text "Second page" ])
            Page.footer footer
        ]
    ]

(*** hide ***)
render numbered
(*** include-it-raw ***)

(**
Sections number pages within a named range: `section "name"` marks content, and `Text.sectionPageNumber`,
`Text.sectionTotalPages`, `Text.sectionBeginPage` and `Text.sectionEndPage` read it. `sectionLink "name"` and
`Text.sectionLink label "name"` link to it.

## Block settings

`Text.clampLines n` limits a block to `n` lines ending in "…"; `Text.paragraphSpacing`, `Text.firstLineIndent`
and `Text.justify` shape paragraphs; `Text.style` sets the default style of the block.
*)

let long = String.replicate 12 "The quick brown fox jumps over the lazy dog. "

let clamped =
    columnSpaced 8 [
        richText [ Text.clampLines 2; Text.span long ]
        richText [ Text.justify; Text.firstLineIndent 12; Text.style (Style.size 8); Text.span long ]
    ]

let clampDemo = document [ page [ Page.sizeOf 300 150; Page.margin 10; Page.content clamped ] ]

(*** hide ***)
render clampDemo
(*** include-it-raw ***)
