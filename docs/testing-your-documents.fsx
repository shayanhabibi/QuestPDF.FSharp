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
# Testing your documents

## Pin everything that varies

A document generates the same bytes on every run once its dates and fonts are fixed:
*)

open System
open QuestPDF.FSharp

License.community ()
Font.useSystemFonts false   // only registered and bundled fonts
Font.strict true            // missing fonts and glyphs throw instead of falling back

let fixedDate = DateTimeOffset (2026, 1, 2, 3, 4, 5, TimeSpan.Zero)

let receipt (total: decimal) =
    document [
        Meta.dated fixedDate
        page [
            Page.sizeOf 200 100
            Page.margin 10
            Page.content (column [ styledText Style.bold "Receipt"; text $"Total: {total:N2}" ])
            Page.footer (richText [ Text.span "Page "; Text.pageNumber; Text.span " of "; Text.totalPages ])
        ]
    ]

(*** hide ***)
render (receipt 12.5m)
(*** include-it-raw ***)

(**
## Byte equivalence

Compare a document against a reference built another way, for example the fluent QuestPDF version. Equal bytes
mean an identical file:
*)

open QuestPDF.Fluent
open QuestPDF.Infrastructure
open QuestPDF.FSharp

let rawReceipt (total: decimal) =
    Document
        .Create(fun dc ->
            dc.Page (fun p ->
                p.Size (200f, 100f)
                p.Margin 10f

                p.Content().Column (fun col ->
                    col.Item().Text("Receipt").Bold () |> ignore
                    col.Item().Text ($"Total: {total:N2}") |> ignore)

                p.Footer().Text (fun t ->
                    t.Span "Page " |> ignore
                    t.CurrentPageNumber () |> ignore
                    t.Span " of " |> ignore
                    t.TotalPages () |> ignore))
            |> ignore)
        .WithMetadata (DocumentMetadata (CreationDate = fixedDate, ModifiedDate = fixedDate))

let same = Pdf.bytes (receipt 12.5m) = Pdf.bytes (rawReceipt 12.5m)

(*** include-value: same ***)

(**
PDF/A and PDF/UA files carry a random document ID: a `uuid:` in the XMP metadata and the trailer `/ID`, which
QuestPDF writes as a hex string or as a literal string. Zero both before comparing:
*)

open System.Text
open System.Text.RegularExpressions

/// A PDF string object: a hex string, or a literal string with backslash escapes.
let pdfString = @"(?:<[0-9A-Fa-f]*>|\((?:\x5C[\s\S]|[^\x5C()])*\))"
let zeroId = "<00000000000000000000000000000000>"

let normalizeIds (pdf: byte[]) =
    let text = Encoding.Latin1.GetString pdf
    let text = Regex.Replace (text, @"uuid:[0-9A-Fa-f\-]{36}", fun _ -> "uuid:" + String ('0', 36))
    let text = Regex.Replace (text, @"/ID\s*\[\s*" + pdfString + @"\s*" + pdfString + @"\s*\]", fun _ -> $"/ID [{zeroId} {zeroId}]")
    Encoding.Latin1.GetBytes text

let archival () = document [ Meta.dated fixedDate; Output.pdfA PDFA_Conformance.PDFA_3B; page [ Page.content (text "archived") ] ]

let comparable =
    List.init 20 (fun _ -> normalizeIds (Pdf.bytes (archival ())) = normalizeIds (Pdf.bytes (archival ())))
    |> List.forall id

(*** include-value: comparable ***)

(**
## Text extraction

[PdfPig](https://github.com/UglyToad/PdfPig) reads the text, words and links of a generated file:

```fsharp
open UglyToad.PdfPig

use pdf = PdfDocument.Open (Pdf.bytes (receipt 12.5m))

for p in pdf.GetPages () do
    printfn "%d: %s" p.Number p.Text
```

Page numbers ("Page 2 of 3"), repeated headers and link targets are all checkable this way.

## Page images

`Pdf.images ImageFormat.Png 72 doc` returns one PNG per page, for snapshot tests and visual review. A partial
application bound at the top level needs a type annotation: the document parameter is an interface, so F# infers a
generic function and reports a value restriction error otherwise.
*)

let snapshot: IDocument -> byte[] list = Pdf.images ImageFormat.Png 72

let pages = snapshot (receipt 12.5m)

(*** hide ***)
$"{pages.Length} page image(s), {pages.Head.Length} bytes"
(*** include-it ***)
