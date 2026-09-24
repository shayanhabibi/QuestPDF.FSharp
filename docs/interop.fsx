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
# Interop

## Fluent code inside wrapper content

Raw interop code opens `QuestPDF.Fluent` and `QuestPDF.Infrastructure` first and `QuestPDF.FSharp` last, so the
wrapper names win.
*)

open QuestPDF.Fluent
open QuestPDF.Infrastructure
open QuestPDF.FSharp

let mixed =
    column [
        raw (fun c -> c.Text("raw: IContainer -> unit").Underline() |> ignore)
        fluent (fun c -> c.Text "fluent: IContainer -> 'a, the result discarded")
        modify (fun c -> c.Padding(5f).Background Colors.Amber.Lighten4) >> text "modify: IContainer -> IContainer as a Modifier"
        richText [ Text.span "Text.raw: "; Text.raw (fun t -> t.Span("TextDescriptor -> unit").Italic() |> ignore) ]
        row [ Row.fill (text "a lambda part:"); fun r -> r.AutoItem().Text "RowDescriptor -> unit" |> ignore ]
    ]

let mixedDemo = document [ page [ Page.sizeOf 330 120; Page.margin 10; Page.content mixed ] ]

(*** hide ***)
render mixedDemo
(*** include-it-raw ***)

(**
Every part list accepts a lambda over its QuestPDF descriptor: `PageDescriptor -> unit` in a page,
`RowDescriptor -> unit` in a row, `TableDescriptor -> unit` in a table, and so on.

`Measured.*` holds the non-inline implementation behind every length or number shim, for callers that already have
a `Length`: `Measured.padding (5 * mm)` is `padding (5 * mm)`.

## Wrapper content inside fluent code

`Content.run` mounts wrapper content in a container, and `Pdf.*` generates any `IDocument`:
*)

let fluentDocument =
    Document.Create (fun dc ->
        dc.Page (fun p ->
            p.Size (PageSize.landscape PageSizes.A7)
            p.Margin 10f
            Content.run (padding 5 >> background Colors.Green.Lighten4 >> text "wrapper content in a fluent page") (p.Content ()))
        |> ignore)

(*** hide ***)
render fluentDocument
(*** include-it-raw ***)

(**
`Content.ofComponent` draws a QuestPDF `IComponent`.

## Mapping from fluent calls

Every public member of the `QuestPDF.Fluent` and `QuestPDF.Companion` types, and of the QuestPDF extension classes,
is listed below with its QuestPDF.FSharp counterparts or the reason it is reached through the escape hatches. The
table is generated from `tests/QuestPDF.FSharp.Tests/Coverage.fs`; a test fails when a QuestPDF upgrade adds a
member the file does not list.
*)

(*** hide ***)
#load "../tests/QuestPDF.FSharp.Tests/Coverage.fs"

open QuestPDF.FSharp.Tests.Coverage

let encode (value: string) = Net.WebUtility.HtmlEncode value

[ "<table><thead><tr><th>QuestPDF member</th><th>QuestPDF.FSharp</th></tr></thead><tbody>"
  for key, mapping in mappings do
      let target =
          match mapping with
          | Wrapped names -> names |> List.map (fun name -> "<code>" + encode name + "</code>") |> String.concat ", "
          | Raw reason -> "<em>raw</em>: " + encode reason

      "<tr><td><code>" + encode key + "</code></td><td>" + target + "</td></tr>"
  "</tbody></table>" ]
|> String.concat "\n"
(*** include-it-raw ***)
