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
# QuestPDF.FSharp

An idiomatic F# layer over [QuestPDF](https://www.questpdf.com/) 2026.9.0 (net10.0). Layouts are plain functions
composed with `>>`, containers take lists, and the output is byte-identical to the equivalent fluent QuestPDF code.

## Install

```shell
dotnet add package QuestPDF.FSharp
```

QuestPDF needs a license before it generates anything. The Community license covers most users; see the
[QuestPDF license terms](https://www.questpdf.com/license/). QuestPDF.FSharp never sets it for you:
*)

open QuestPDF.FSharp

License.community ()

(**
## Hello world
*)

let hello =
    document [
        page [
            Page.size PageSizes.A4
            Page.margin (2 * cm)
            Page.content (padding 10 >> background Colors.Grey.Lighten3 >> styledText (Style.size 20) "Hello, world!")
        ]
    ]

(**
`Pdf.save "hello.pdf" hello` writes the file; `Pdf.bytes hello` returns it. The page renders as:
*)

(*** hide ***)
render hello
(*** include-it-raw ***)

(**
## The rules

1. **`Content = Slot -> unit`** fills a slot. **`Modifier = Slot -> Slot`** wraps it. `padding 10 >> background c
   >> text "x"` is a `Content`, read outer to inner as in the fluent chain.
2. **`Slot` is a one-field struct over `IContainer`**, so a top-level `let heading = text "hi"` needs no annotation.
3. **Containers take lists of parts.** `for`, `if` and `match` work inside the list, and any lambda over the
   descriptor is also a valid part.
4. **Numbers are bare.** A length accepts `int`, `float`, `float32` (points) or a `Length` such as `5 * mm`. The
   unit passes through to QuestPDF unconverted.
5. **`inline` functions are one-line shims** over the non-inline `Measured.*` functions.
6. **One open.** `open QuestPDF.FSharp` brings in the elements, modifiers and `Colors`/`PageSizes`/`Color`/`PageSize`.
7. **No hidden global state.** The license, fonts and settings are explicit calls.
8. **Escape hatches both ways:** `raw`, `fluent`, `modify`, `Content.run`, `Text.raw`, and `Pdf.*` on any `IDocument`.

## Pages

- [Concepts](concepts.html): slots, content, modifiers and parts lists
- [Lengths and colours](lengths-and-colors.html): bare numbers, units, `Colors` and `PageSizes`
- [Text](text.html): plain, styled and rich text, links and page numbers
- [Layout](layout.html): columns, rows, layers and decorations
- [Tables](tables.html): columns, headers and footers, spans
- [Paging](paging.html): page setup, headers and footers, content across pages
- [Images](images.html): raster images, SVG and lines
- [Output](output.html): generation targets, metadata, PDF/A and PDF/UA, reproducible output
- [Interop](interop.html): mixing wrapper and fluent QuestPDF code, and the full mapping table
- [Testing your documents](testing-your-documents.html): byte equivalence, text extraction and snapshots
- Recipes: [invoice](recipes/invoice.html) and [resume](recipes/resume.html)
- [Gotchas](gotchas.html)

## Building these docs

```shell
dotnet fsi build.fsx -- docs            # build, evaluating every page
dotnet fsi build.fsx -- docs --watch    # serve with live reload
```
*)
