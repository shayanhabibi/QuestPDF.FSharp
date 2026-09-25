module FSharp.QuestPDF.Tests.ExamplesTests

open System
open System.Globalization
open Expecto
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open FSharp.QuestPDF
open FSharp.QuestPDF.Tests.Support

/// Worked example (a), with pinned dates.
let hello =
    document
        [ Meta.dated fixedDate
          page
              [ Page.size PageSizes.A4
                Page.margin (2 * cm)
                Page.content (
                    padding 10
                    >> background Colors.Grey.Lighten3
                    >> styledText (Style.size 20) "Hello, world!"
                ) ] ]

let rawHello =
    rawPage (fun p ->
        p.Size PageSizes.A4
        p.Margin (2f, Unit.Centimetre)

        p.Content().Padding(10f).Background(Colors.Grey.Lighten3).Text("Hello, world!").FontSize (20f)
        |> ignore)

/// Worked example (c), a two-column resume, with pinned dates.
module Resume =
    let accent = Colors.Teal.Darken2

    let link (label, url) =
        Text.link label url
        |> Text.withStyle (Style.color accent >> Style.underline)

    let heading s =
        paddingBottom 2
        >> borderBottom 0.5
        >> borderColor accent
        >> styledText
            (Style.size 12
             >> Style.semiBold
             >> Style.color accent)
            s

    let sidebar =
        columnSpaced
            6
            [ heading "Contact"
              richText
                  [ Text.span "me@x.dev"
                    Text.lineBreak
                    link ("github.com/me", "https://github.com/me") ]
              heading "Skills"
              text "F#, .NET, PDF" ]

    let job role dates bullets =
        showEntire
        >> columnSpaced
            2
            [ richText
                  [ Text.styled Style.semiBold role
                    Text.styled
                        (Style.color Colors.Grey.Darken1
                         >> Style.enableFeature "tnum")
                        $" · {dates}" ]
              for b in bullets do
                  row [ Row.constant 10 (text "•"); Row.fill (text b) ] ]

    let main =
        columnSpaced
            8
            [ styledText (Style.size 24 >> Style.bold) "Jane Doe"
              heading "Experience"
              job "Lead dev" "2020–now" [ "Built FSharp.QuestPDF"; "Led a team of 4" ]
              job "Engineer" "2016–2020" [ "Shipped things" ] ]

    let resume =
        document
            [ Meta.title "Jane Doe"
              Meta.dated fixedDate
              page
                  [ Page.size PageSizes.A4
                    Page.margin (18 * mm)
                    Page.textStyle (Style.size 10 >> Style.lineHeight 1.2)
                    Page.content (row [ Row.spacing 16; Row.constant (55 * mm) sidebar; Row.fill main ]) ] ]

/// Worked example (b), a multi-page invoice, with pinned dates.
module Invoice =
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

/// The raw QuestPDF counterpart of worked example (b).
module RawInvoice =
    open Invoice

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

/// The raw QuestPDF counterpart of worked example (c).
module RawResume =
    let accent = Colors.Teal.Darken2

    let heading (c: IContainer) (s: string) =
        c.PaddingBottom(2f).BorderBottom(0.5f).BorderColor(accent).Text(s).FontSize(12f).SemiBold().FontColor (accent)
        |> ignore

    let link (t: TextDescriptor) (label: string, url: string) =
        t.Hyperlink(label, url).FontColor(accent).Underline ()
        |> ignore

    let job (c: IContainer) (role: string) (dates: string) (bullets: string list) =
        c
            .ShowEntire()
            .Column (fun col ->
                col.Spacing (2f)

                col
                    .Item()
                    .Text (fun t ->
                        t.Span(role).SemiBold () |> ignore

                        t.Span($" · {dates}").FontColor(Colors.Grey.Darken1).EnableFontFeature ("tnum")
                        |> ignore)

                for b in bullets do
                    col
                        .Item()
                        .Row (fun r ->
                            r.ConstantItem(10f).Text ("•") |> ignore
                            r.RelativeItem().Text (b) |> ignore))

    let resume =
        Document
            .Create(fun dc ->
                dc.Page (fun p ->
                    p.Size (PageSizes.A4)
                    p.Margin (18f, Unit.Millimetre)
                    p.DefaultTextStyle (fun (s: TextStyle) -> s.FontSize(10f).LineHeight (Nullable 1.2f))

                    p
                        .Content()
                        .Row (fun r ->
                            r.Spacing (16f)

                            r
                                .ConstantItem(55f, Unit.Millimetre)
                                .Column (fun col ->
                                    col.Spacing (6f)
                                    heading (col.Item ()) "Contact"

                                    col
                                        .Item()
                                        .Text (fun t ->
                                            t.Span ("me@x.dev") |> ignore
                                            t.Span ("\n") |> ignore
                                            link t ("github.com/me", "https://github.com/me"))

                                    heading (col.Item ()) "Skills"
                                    col.Item().Text ("F#, .NET, PDF") |> ignore)

                            r
                                .RelativeItem()
                                .Column (fun col ->
                                    col.Spacing (8f)

                                    col.Item().Text("Jane Doe").FontSize(24f).Bold ()
                                    |> ignore

                                    heading (col.Item ()) "Experience"
                                    job (col.Item ()) "Lead dev" "2020–now" [ "Built FSharp.QuestPDF"; "Led a team of 4" ]
                                    job (col.Item ()) "Engineer" "2016–2020" [ "Shipped things" ])))
                |> ignore)
            .WithMetadata (DocumentMetadata (Title = "Jane Doe", CreationDate = fixedDate, ModifiedDate = fixedDate))

[<Tests>]
let tests =
    testList
        "Examples"
        [ equivalentDoc "hello world" hello rawHello
          equivalentDoc "invoice" (Invoice.invoice "2026-042" Invoice.lines) (RawInvoice.invoice "2026-042" Invoice.lines)
          test "invoice numbers its pages in the footer" {
              configure ()
              let texts = pageTexts (Pdf.bytes (Invoice.invoice "2026-042" Invoice.lines))
              Expect.equal texts.Length 2 "two pages"
              Expect.stringEnds texts[0] "Page 1 of 2" "footer of page 1"
              Expect.stringEnds texts[1] "Page 2 of 2" "footer of page 2"
          }
          test "invoice repeats the table header and ends with the total" {
              configure ()
              let pdf = Pdf.bytes (Invoice.invoice "2026-042" Invoice.lines)

              for page in [ 1; 2 ] do
                  Expect.contains (wordsInOrder pdf page) "Amount" $"the table header on page {page}"

              let total =
                  Invoice.lines
                  |> List.sumBy (fun l -> decimal l.Qty * l.Price)
                  |> Invoice.money

              let lastPage = wordsInOrder pdf 2
              let beforeFooter = lastPage |> List.takeWhile (fun w -> w <> "Page")
              Expect.equal (List.last beforeFooter) total "the total ends the table"
              Expect.contains beforeFooter "Total" "the table footer row"
          }
          equivalentDoc "resume" Resume.resume RawResume.resume
          test "resume links to the profile" {
              configure ()

              Expect.equal (hyperlinks (Pdf.bytes Resume.resume)) [ (1, "github.com/me", "https://github.com/me") ] "one link on page 1"
          }
          test "resume renders the bullets and dates" {
              configure ()
              let words = wordsInOrder (Pdf.bytes Resume.resume) 1
              Expect.contains words "•" "a bullet"
              Expect.contains words "2020–now" "the dates of the first job"
          } ]
