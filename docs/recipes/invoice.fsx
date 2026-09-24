(*** hide ***)
#r "nuget: QuestPDF, 2026.9.0"
#r "../../src/QuestPDF.FSharp/bin/Release/net10.0/QuestPDF.FSharp.dll"

open System
open QuestPDF.FSharp

License.community ()
Font.useSystemFonts false
Font.strict true
Font.registerDirectory (IO.Path.Combine (__SOURCE_DIRECTORY__, "../..", "fonts"))

/// The pages of a document as PNG images at 96 dpi, in HTML img elements.
let render (document: QuestPDF.Infrastructure.IDocument) =
    Pdf.images ImageFormat.Png 96 document
    |> List.map (fun png ->
        "<img class=\"page-render\" alt=\"A rendered page\" style=\"max-width: 100%; border: 1px solid #ccc; margin: 4px;\" src=\"data:image/png;base64,"
        + Convert.ToBase64String png
        + "\" />")
    |> String.concat "\n"

(**
# Recipe: a multi-page invoice

Sixty line items make a table that spans two A4 pages. The page header and footer repeat, the table header repeats
on the second page, the footer row carries the total, and the page footer numbers the pages.
*)

open System
open System.Globalization
open QuestPDF.FSharp

let fixedDate = DateTimeOffset (2026, 1, 2, 3, 4, 5, TimeSpan.Zero)

type LineItem =
    { Name: string
      Qty: int
      Price: decimal }

/// Sixty lines: enough for two A4 pages.
let lines =
    [ for i in 1..60 ->
          { Name = $"Widget {i}"
            Qty = i % 4 + 1
            Price = decimal i * 1.25m } ]

let money (d: decimal) =
    d.ToString ("N2", CultureInfo.InvariantCulture)

let muted = Style.color Colors.Grey.Darken1 >> Style.size 9

let headCell s =
    Table.cell (
        background Colors.Grey.Lighten3
        >> padding 4
        >> styledText Style.semiBold s
    )

let bodyCell s =
    Table.cell (
        borderBottom 0.5
        >> borderColor Colors.Grey.Lighten2
        >> padding 4
        >> text s
    )

let numCell s =
    Table.cell (
        borderBottom 0.5
        >> borderColor Colors.Grey.Lighten2
        >> padding 4
        >> alignRight
        >> text s
    )

let invoice (no: string) (lines: LineItem list) =
    let total =
        lines
        |> List.sumBy (fun l -> decimal l.Qty * l.Price)

    document
        [ Meta.title $"Invoice {no}"
          Meta.author "ACME"
          Meta.dated fixedDate
          page
              [ Page.size PageSizes.A4
                Page.margin (15 * mm)
                Page.textStyle (Style.size 10)
                Page.header (
                    row
                        [ Row.fill (
                              styledText
                                  (Style.size 20
                                   >> Style.bold
                                   >> Style.color Colors.Blue.Darken2)
                                  $"Invoice #{no}"
                          )
                          Row.auto (alignRight >> text "2026-09-24") ]
                )
                Page.content (
                    paddingV (5 * mm)
                    >> table
                        [ Table.columns
                              [ Table.constant (10 * mm)
                                Table.relative 4
                                Table.relative 1
                                Table.relative 1.5 ]
                          Table.header [ headCell "#"; headCell "Item"; headCell "Qty"; headCell "Amount" ]
                          for i, l in List.indexed lines do
                              Table.cells
                                  [ bodyCell (string (i + 1))
                                    bodyCell l.Name
                                    numCell (string l.Qty)
                                    numCell (money (decimal l.Qty * l.Price)) ]
                          Table.footer
                              [ Table.cellWith
                                    [ Cell.columnSpan 3 ]
                                    (alignRight
                                     >> padding 4
                                     >> styledText Style.bold "Total")
                                Table.cell (
                                    alignRight
                                    >> padding 4
                                    >> styledText Style.bold (money total)
                                ) ] ]
                )
                Page.footer (
                    richText
                        [ Text.alignCenter
                          Text.style muted
                          Text.span "Page "
                          Text.pageNumber
                          Text.span " of "
                          Text.totalPages ]
                ) ] ]

(*** hide ***)
render (invoice "2026-042" lines)
(*** include-it-raw ***)

(**
## The fluent QuestPDF version

The same invoice written against QuestPDF's fluent API, for comparison. Both generate identical bytes.
*)

open QuestPDF.Fluent
open QuestPDF.Infrastructure
open QuestPDF.FSharp

module Raw =
    let headCell (c: IContainer) (s: string) =
        c.Background(Colors.Grey.Lighten3).Padding(4f).Text(s).SemiBold ()
        |> ignore

    let bodyCell (c: IContainer) (s: string) =
        c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4f).Text (s)
        |> ignore

    let numCell (c: IContainer) (s: string) =
        c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4f).AlignRight().Text (s)
        |> ignore

    let invoice (no: string) (lines: LineItem list) =
        let total =
            lines
            |> List.sumBy (fun l -> decimal l.Qty * l.Price)

        Document
            .Create(fun dc ->
                dc.Page (fun p ->
                    p.Size (PageSizes.A4)
                    p.Margin (15f, Unit.Millimetre)
                    p.DefaultTextStyle (fun (s: TextStyle) -> s.FontSize (10f))

                    p
                        .Header()
                        .Row (fun r ->
                            r.RelativeItem().Text($"Invoice #{no}").FontSize(20f).Bold().FontColor (Colors.Blue.Darken2)
                            |> ignore

                            r.AutoItem().AlignRight().Text ("2026-09-24")
                            |> ignore)

                    p
                        .Content()
                        .PaddingVertical(5f, Unit.Millimetre)
                        .Table (fun t ->
                            t.ColumnsDefinition (fun d ->
                                d.ConstantColumn (10f, Unit.Millimetre)
                                d.RelativeColumn (4f)
                                d.RelativeColumn (1f)
                                d.RelativeColumn (1.5f))

                            t.Header (fun h ->
                                for s in [ "#"; "Item"; "Qty"; "Amount" ] do
                                    headCell (h.Cell ()) s)

                            for i, l in List.indexed lines do
                                bodyCell (t.Cell ()) (string (i + 1))
                                bodyCell (t.Cell ()) l.Name
                                numCell (t.Cell ()) (string l.Qty)
                                numCell (t.Cell ()) (money (decimal l.Qty * l.Price))

                            t.Footer (fun f ->
                                f.Cell().ColumnSpan(3u).AlignRight().Padding(4f).Text("Total").Bold ()
                                |> ignore

                                f.Cell().AlignRight().Padding(4f).Text(money total).Bold ()
                                |> ignore))

                    p
                        .Footer()
                        .Text (fun t ->
                            t.AlignCenter ()
                            t.DefaultTextStyle (fun (s: TextStyle) -> s.FontColor(Colors.Grey.Darken1).FontSize (9f))
                            t.Span ("Page ") |> ignore
                            t.CurrentPageNumber () |> ignore
                            t.Span (" of ") |> ignore
                            t.TotalPages () |> ignore))
                |> ignore)
            .WithMetadata (DocumentMetadata (Title = $"Invoice {no}", Author = "ACME", CreationDate = fixedDate, ModifiedDate = fixedDate))

let identical = Pdf.bytes (invoice "2026-042" lines) = Pdf.bytes (Raw.invoice "2026-042" lines)

(*** include-value: identical ***)
