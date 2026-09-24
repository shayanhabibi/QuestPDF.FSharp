# Testing your documents

## Pin everything that varies

```fsharp
License.community ()
Font.useSystemFonts false
Font.strict true   // missing fonts and glyphs throw

let fixedDate = DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero)

let invoice = document [ Meta.dated fixedDate; page [ Page.content (text "...") ] ]
```

With the dates and fonts pinned, the same document generates the same bytes on every run.

## Byte equivalence

Compare a document against a reference built another way, for example the fluent QuestPDF version:

```fsharp
let same = Pdf.bytes invoice = rawInvoice.GeneratePdf()
```

For PDF/A and PDF/UA, zero the random document ID first: a same-length rewrite of the `uuid:` values and the
trailer `/ID` hex strings keeps every offset valid.

## Text extraction

[PdfPig](https://github.com/UglyToad/PdfPig) reads the text, words and links of a generated file:

```fsharp
open UglyToad.PdfPig

use pdf = PdfDocument.Open(Pdf.bytes invoice)
for p in pdf.GetPages() do
    printfn "%d: %s" p.Number p.Text
```

Page numbers ("Page 2 of 3"), repeated headers and link targets are all checkable this way.

## Page images

`Pdf.images ImageFormat.Png 72 doc` returns one PNG per page, for snapshot tests and visual review.
