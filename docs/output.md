# Output

## Generation

| Function | Result |
|----------|--------|
| `Pdf.bytes doc` | the PDF file as `byte[]` |
| `Pdf.save path doc` | writes the PDF file to a path |
| `Pdf.write stream doc` | writes the PDF file to a stream |
| `Pdf.show doc` | opens the PDF file in the default viewer |
| `Pdf.images ImageFormat.Png 96 doc` | one image per page (`Png`, `Jpeg` or `Webp`, at a dpi) |
| `Pdf.svgs doc` | one SVG document per page |
| `Pdf.companion doc` | a live preview in the QuestPDF Companion app (port 12500) |

Every function takes any `IDocument`, including a fluent `Document.Create(...)`, and raises
`InvalidOperationException` until a license is set with `License.community ()`, `License.professional ()` or
`License.enterprise ()`.

## Metadata and settings

Metadata and settings are items in the `document` list, beside the pages:

```fsharp
document [
    Meta.title "Invoice 2026-042"
    Meta.author "Acme"
    Meta.dated (DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero))
    Output.pdfA PDFA_Conformance.PDFA_3B
    Output.imageQuality ImageCompressionQuality.Medium
    Output.imageDpi 144
    page [ Page.content (text "...") ]
]
```

| Item | Sets |
|------|------|
| `Output.pdfA level` | `DocumentSettings.PDFA_Conformance` |
| `Output.pdfUA` | `DocumentSettings.PDFUA_Conformance` to PDF/UA-1 |
| `Output.compress bool` | `DocumentSettings.CompressDocument` (on by default) |
| `Output.imageQuality q` | `DocumentSettings.ImageCompressionQuality` (High by default) |
| `Output.imageDpi n` | `DocumentSettings.ImageRasterDpi` (288 by default) |
| `Output.rightToLeft` | `DocumentSettings.ContentDirection`; page layout follows `Page.rightToLeft` |

A later item overrides an earlier one.

## Reproducible output

Without `Meta.dated`, QuestPDF stamps the creation and modification dates with the current time, so two
generations differ. With `Meta.dated`, the same document generates the same bytes.

PDF/A and PDF/UA files also carry a random document ID (the XMP `uuid:` and the trailer `/ID`). Two generations
differ only in those bytes, which have a fixed length; zeroing them makes the files comparable.
