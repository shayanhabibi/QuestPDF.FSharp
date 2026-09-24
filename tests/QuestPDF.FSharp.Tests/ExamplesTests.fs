module QuestPDF.FSharp.Tests.ExamplesTests

open System
open Expecto
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open QuestPDF.FSharp
open QuestPDF.FSharp.Tests.Support

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
                                    job (col.Item ()) "Lead dev" "2020–now" [ "Built QuestPDF.FSharp"; "Led a team of 4" ]
                                    job (col.Item ()) "Engineer" "2016–2020" [ "Shipped things" ])))
                |> ignore)
            .WithMetadata (DocumentMetadata (Title = "Jane Doe", CreationDate = fixedDate, ModifiedDate = fixedDate))

[<Tests>]
let tests =
    testList
        "Examples"
        [ equivalentDoc "hello world" hello rawHello
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
