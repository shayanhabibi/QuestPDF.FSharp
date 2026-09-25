(**
---
category: Guide
categoryindex: 1
index: 8
---
*)
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
# Output

## Generation

| Function | Result |
|----------|--------|
| `Pdf.bytes doc` | the PDF file as `byte[]` |
| `Pdf.save path doc` | writes the PDF file to a path |
| `Pdf.write stream doc` | writes the PDF file to a stream |
| `Pdf.show doc` | opens the PDF file in the default viewer |
| `Pdf.images format dpi doc` | one image per page (`ImageFormat.Png`, `Jpeg` or `Webp`) |
| `Pdf.svgs doc` | one SVG document per page |
| `Pdf.companion doc` | a live preview in the QuestPDF Companion app (port 12500) |
| `Pdf.companionAsync doc` | the same preview as an `Async<unit>` that stops on cancellation |

`Pdf.companion` blocks until the Companion app closes; [a live preview on save](recipes/hot-reload-preview.html)
with `QuestPDF.FSharp.Preview` serves the pages to a browser without blocking.

Every function takes any `IDocument`, including a fluent `Document.Create(...)`, and raises
`InvalidOperationException` until a license is set with `License.community ()`, `License.professional ()` or
`License.enterprise ()`.
*)

open System
open QuestPDF.FSharp

let note = document [ page [ Page.sizeOf 200 80; Page.margin 10; Page.content (text "A short note.") ] ]

let pdf = Pdf.bytes note
let pngs = Pdf.images ImageFormat.Png 96 note
let svgs = Pdf.svgs note

(*** hide ***)
$"The PDF file is {pdf.Length} bytes; the note has {pngs.Length} PNG page and {svgs.Length} SVG page."
(*** include-it ***)

(**
The page images on this site are made with `Pdf.images ImageFormat.Png 96`:
*)

(*** hide ***)
render note
(*** include-it-raw ***)

(**
## Metadata and settings

Metadata and settings are items in the `document` list, beside the pages:
*)

let invoice =
    document [
        Meta.title "Invoice 2026-042"
        Meta.author "Acme"
        Meta.language "en-GB"
        Meta.dated (DateTimeOffset (2026, 1, 2, 0, 0, 0, TimeSpan.Zero))
        Output.pdfA PDFA_Conformance.PDFA_3B
        Output.imageQuality ImageCompressionQuality.Medium
        Output.imageDpi 144
        page [ Page.content (text "...") ]
    ]

(**
| Item | Sets |
|------|------|
| `Meta.title`, `author`, `subject`, `keywords`, `creator`, `producer`, `language` | the document information |
| `Meta.created`, `Meta.modified` | one date each |
| `Meta.dated date` | both dates |
| `Output.pdfA level` | PDF/A conformance, for example `PDFA_Conformance.PDFA_3B` |
| `Output.pdfUA` | PDF/UA-1 (accessible PDF) conformance |
| `Output.compress bool` | compression of the file (on by default) |
| `Output.imageQuality q` | the default compression quality of embedded images (`High` by default) |
| `Output.imageDpi n` | the default resolution of embedded images (288 by default) |
| `Output.rightToLeft` | every page laid out from right to left; `Page.leftToRight` restores one page |

A later item overrides an earlier one. The built document reads them back with `GetMetadata ()` and
`GetSettings ()`:
*)

let settings = invoice.GetSettings ()

(*** hide ***)
$"PDF/A: {settings.PDFA_Conformance}, image quality: {settings.ImageCompressionQuality}, image dpi: {settings.ImageRasterDpi}"
(*** include-it ***)

(**
## Accessible PDF: semantic tags

`Semantic.*` modifiers tag content with its role for assistive technology: `heading1` ... `heading6`,
`paragraph`, `list`, `listItem`, `listLabel` and `listItemBody`, `table` (with `Cell.horizontalHeader` for a row
header cell), `figure alt` and `image alt` with alternative text, `language`, `ignore` for decoration, and more. The
tags go into a tagged PDF, such as a document with `Output.pdfUA`; the layout is unchanged.
*)

let accessible =
    document [
        Meta.title "Opening hours"
        Meta.language "en-GB"
        Output.pdfUA
        page [
            Page.sizeOf 220 110
            Page.margin 10
            Page.content (
                Semantic.article
                >> column [
                    Semantic.heading1 >> styledText (Style.size 14 >> Style.bold) "Opening hours"
                    Semantic.paragraph >> text "Monday to Friday, 9:00 to 17:00."
                    Semantic.list
                    >> column [
                        for day in [ "Saturday: 10:00 to 14:00"; "Sunday: closed" ] do
                            Semantic.listItem
                            >> row [
                                Row.auto (Semantic.listLabel >> paddingRight 4 >> text "-")
                                Row.fill (Semantic.listItemBody >> text day)
                            ]
                    ]
                ]
            )
        ]
    ]

(*** hide ***)
render accessible
(*** include-it-raw ***)

(**
## Merging documents

`Pdf.merge` joins documents into one, taking the same `Meta.*` and `Output.*` items as `document`, plus
`Merge.continuousPageNumbers` (one numbering across the documents) or `Merge.originalPageNumbers` (each document
keeps its own). The result is an `IDocument`, so every `Pdf.*` function generates it:
*)

let bundle = Pdf.merge [ Meta.title "Bundle"; Merge.continuousPageNumbers ] [ note; invoice ]

(*** hide ***)
$"The bundle has {(Pdf.images ImageFormat.Png 24 bundle).Length} pages."
(*** include-it ***)

(**
## Editing PDF files

`PdfFile` edits existing PDF files through qpdf: `PdfFile.load path` starts a pipeline, `takePages`, `merge`,
`mergePages`, `overlay`, `underlay`, `attach`, `extendMetadata`, `encrypt40`/`encrypt128`/`encrypt256`, `decrypt`,
`removeRestrictions` and `linearize` add steps, and `PdfFile.save path` runs them. Page selectors use the qpdf
syntax, such as `"1-3,r1"`.
*)

let folder = IO.Directory.CreateTempSubdirectory "questpdf-fsharp-docs"
let source = IO.Path.Combine (folder.FullName, "bundle.pdf")
let firstPage = IO.Path.Combine (folder.FullName, "first-page.pdf")

bundle |> Pdf.save source

PdfFile.load source
|> PdfFile.takePages "1"
|> PdfFile.encrypt256 [ Encryption.ownerPassword "owner"; Encryption.allowPrinting true ]
|> PdfFile.save firstPage

(*** hide ***)
$"first-page.pdf exists: {IO.File.Exists firstPage}"
(*** include-it ***)

(**
A list of `Encryption.*` parts bound apart from the call needs its part type, `Encryption40Part`, `Encryption128Part`
or `Encryption256Part`, since the same setters serve every strength. The file also carries its source as an attachment:
*)

let common: Encryption256Part list = [ Encryption.ownerPassword "owner"; Encryption.allowPrinting false ]

PdfFile.load source
|> PdfFile.attach source [ Attachment.relationship DocumentAttachmentRelationship.Source ]
|> PdfFile.encrypt256 (Encryption.userPassword "reader" :: common)
|> PdfFile.save firstPage

(**
## Reproducible output

`document` fixes the metadata when it builds the document. Without `Meta.dated`, both dates are the time of the
`document` call: generating the same document value twice gives the same bytes, and building the document again
a second later gives different bytes. `Meta.dated` pins both dates, so every build of the same content generates
the same bytes:
*)

let build () =
    document [ Meta.dated (DateTimeOffset (2026, 1, 2, 0, 0, 0, TimeSpan.Zero)); page [ Page.content (text "same") ] ]

let reproducible = Pdf.bytes (build ()) = Pdf.bytes (build ())

(*** include-value: reproducible ***)

(**
PDF/A and PDF/UA files also carry a random document ID (the XMP `uuid:` and the trailer `/ID`). Two generations
of such a file differ in those bytes only, so zeroing them makes the files comparable;
[Testing your documents](testing-your-documents.html) shows how.
*)
