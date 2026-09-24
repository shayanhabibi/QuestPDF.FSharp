# QuestPDF.FSharp

An idiomatic F# layer over [QuestPDF](https://www.questpdf.com/) 2026.9.0. Layouts are plain functions composed
with `>>`, containers take lists, and the output is byte-identical to the equivalent fluent QuestPDF code.

## Hello world

```fsharp
open QuestPDF.FSharp

License.community ()

document [
    page [
        Page.size PageSizes.A4
        Page.margin (2 * cm)
        Page.content (padding 10 >> background Colors.Grey.Lighten3 >> styledText (Style.size 20) "Hello, world!")
    ]
]
|> Pdf.save "hello.pdf"
```

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

- [Output](output.html): generation targets, metadata, PDF/A and PDF/UA, reproducible output
- [Interop](interop.html): mixing wrapper and fluent QuestPDF code
- [Testing your documents](testing-your-documents.html): byte equivalence and text extraction
- [Gotchas](gotchas.html)

## Building these docs

```shell
dotnet fsi build.fsx -- docs            # build
dotnet fsi build.fsx -- docs --watch    # serve with live reload
```
