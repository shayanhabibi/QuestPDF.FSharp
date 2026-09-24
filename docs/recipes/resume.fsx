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
# Recipe: a two-column resume

A sidebar of fixed width beside a main column. Styles are values composed with `>>`, the headings and jobs are
functions, and `showEntire` keeps each job on one page.
*)

open System
open QuestPDF.FSharp

let fixedDate = DateTimeOffset (2026, 1, 2, 3, 4, 5, TimeSpan.Zero)

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
          job "Lead dev" "2020–now" [ "Built QuestPDF.FSharp"; "Led a team of 4" ]
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

(*** hide ***)
render resume
(*** include-it-raw ***)

(**
## The fluent QuestPDF version

The same resume written against QuestPDF's fluent API, for comparison. Both generate identical bytes.
*)

open QuestPDF.Fluent
open QuestPDF.Infrastructure
open QuestPDF.FSharp

module Raw =
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
                                    job (col.Item ()) "Lead dev" "2020–now" [ "Built QuestPDF.FSharp"; "Led a team of 4" ]
                                    job (col.Item ()) "Engineer" "2016–2020" [ "Shipped things" ])))
                |> ignore)
            .WithMetadata (DocumentMetadata (Title = "Jane Doe", CreationDate = fixedDate, ModifiedDate = fixedDate))

let identical = Pdf.bytes resume = Pdf.bytes Raw.resume

(*** include-value: identical ***)
