module FSharp.QuestPDF.Tests.TextRichTests

open Expecto
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open FSharp.QuestPDF
open FSharp.QuestPDF.Tests.Support

let private rich (build: TextDescriptor -> unit) (c: IContainer) =
    c.Text (fun t -> build t)

/// Lower-case roman numerals for 1 to 39.
let private roman (n: int) =
    String.replicate (n / 10) "x"
    + [| ""; "i"; "ii"; "iii"; "iv"; "v"; "vi"; "vii"; "viii"; "ix" |][n % 10]

let private romanOrQuestion (n: int option) =
    n |> Option.map roman |> Option.defaultValue "?"

let private formatter (format: int option -> string) =
    PageNumberFormatter (fun n -> format (Option.ofNullable n))

/// Two pages with the parts in the footer of each.
let private twoPages (footer: TextPart list) =
    let pagePart =
        page
            [ Page.size PageSizes.A6
              Page.margin 20
              Page.content (text "body")
              Page.footer (richText footer) ]

    document [ Meta.dated fixedDate; pagePart; pagePart ]

let private rawTwoPages (footer: TextDescriptor -> unit) =
    rawDocument (fun dc ->
        for _ in 1..2 do
            dc.Page (fun p ->
                p.Size PageSizes.A6
                p.Margin 20f
                p.Content().Text ("body") |> ignore
                p.Footer().Text (fun t -> footer t))
            |> ignore)

/// A link on page 1 to a section on page 2.
let private linkedSection (part: TextPart) : Content =
    column
        [ richText [ part ]
          raw (fun c -> c.PageBreak ())
          section "target" >> text "here" ]

let private rawLinkedSection (link: TextDescriptor -> unit) (c: IContainer) =
    c.Column (fun col ->
        col.Item().Text (fun t -> link t)
        col.Item().PageBreak ()

        col.Item().Section("target").Text ("here")
        |> ignore)

/// A section over pages 2 to 4, with the four section numbers in the footer of each page.
let private sectionOverThreePages =
    let pageBreak = raw (fun c -> c.PageBreak ())

    document
        [ Meta.dated fixedDate
          page
              [ Page.size PageSizes.A6
                Page.margin 20
                Page.content (
                    column
                        [ text "before"
                          pageBreak
                          section "s"
                          >> column [ text "first"; pageBreak; text "second"; pageBreak; text "third" ]
                          pageBreak
                          text "after" ]
                )
                Page.footer (
                    richText
                        [ Text.span " w"
                          Text.sectionPageNumber "s"
                          Text.span " t"
                          Text.sectionTotalPages "s"
                          Text.span " b"
                          Text.sectionBeginPage "s"
                          Text.span " e"
                          Text.sectionEndPage "s" ]
                ) ] ]

/// Two paragraphs of several lines each.
let private paragraphs =
    String.replicate 12 "first paragraph "
    + "\n"
    + String.replicate 12 "second paragraph "

let private longText = String.replicate 40 "clamped words " + "LAST"

[<Tests>]
let tests =
    testList
        "Rich text"
        [ testList
              "links"
              [ equivalent
                    "link"
                    (richText [ Text.link "site" "https://example.com" ])
                    (rich (fun t ->
                        t.Hyperlink ("site", "https://example.com")
                        |> ignore))
                equivalent
                    "withStyle on a link"
                    (richText
                        [ Text.link "site" "https://example.com"
                          |> Text.withStyle (Style.color Colors.Blue.Medium >> Style.underline) ])
                    (rich (fun t ->
                        t.Hyperlink("site", "https://example.com").FontColor(Colors.Blue.Medium).Underline ()
                        |> ignore))
                test "link targets the URL" {
                    configure ()

                    let pdf =
                        Pdf.bytes (wrapContent (richText [ Text.span "see "; Text.link "site" "https://example.com" ]))

                    Expect.equal
                        (hyperlinks pdf
                         |> List.map (fun (page, _, uri) -> page, uri))
                        [ (1, "https://example.com") ]
                        "one link on page 1"
                }
                equivalent
                    "sectionLink"
                    (linkedSection (Text.sectionLink "go" "target"))
                    (rawLinkedSection (fun t -> t.SectionLink ("go", "target") |> ignore))
                test "sectionLink resolves to the page of the section" {
                    configure ()
                    let pdf = Pdf.bytes (wrapContent (linkedSection (Text.sectionLink "go" "target")))
                    Expect.equal (internalLinks pdf) [ (1, 2) ] "a link on page 1 to page 2"
                } ]
          testList
              "lines"
              [ equivalent
                    "line"
                    (richText [ Text.line "a"; Text.span "b" ])
                    (rich (fun t ->
                        t.Line ("a") |> ignore
                        t.Span ("b") |> ignore))
                equivalent
                    "emptyLine"
                    (richText [ Text.span "a"; Text.emptyLine; Text.span "b" ])
                    (rich (fun t ->
                        t.Span ("a") |> ignore
                        t.EmptyLine () |> ignore
                        t.Span ("b") |> ignore))
                equivalent
                    "emptyLine is the lineBreak span"
                    (richText [ Text.span "a"; Text.emptyLine; Text.span "b" ])
                    (rich (fun t ->
                        t.Span ("a") |> ignore
                        t.Span ("\n") |> ignore
                        t.Span ("b") |> ignore))
                equivalent
                    "withStyle on a line"
                    (richText [ Text.line "a" |> Text.withStyle Style.bold ])
                    (rich (fun t -> t.Line("a").Bold () |> ignore)) ]
          testList
              "section page numbers"
              [ for name, part, apply in
                    [ "sectionPageNumber", Text.sectionPageNumber "s", (fun (t: TextDescriptor) -> t.PageNumberWithinSection "s")
                      "sectionTotalPages", Text.sectionTotalPages "s", (fun t -> t.TotalPagesWithinSection "s")
                      "sectionBeginPage", Text.sectionBeginPage "s", (fun t -> t.BeginPageNumberOfSection "s")
                      "sectionEndPage", Text.sectionEndPage "s", (fun t -> t.EndPageNumberOfSection "s") ] do
                    equivalent name (section "s" >> richText [ Text.span "n "; part ]) (fun c ->
                        c
                            .Section("s")
                            .Text (fun t ->
                                t.Span ("n ") |> ignore
                                apply t |> ignore))
                test "sectionBeginPage reads the first page of the section" {
                    configure ()

                    let pdf =
                        Pdf.bytes (
                            wrapContent (
                                column
                                    [ richText [ Text.span "begins on "; Text.sectionBeginPage "later" ]
                                      raw (fun c -> c.PageBreak ())
                                      section "later" >> text "the section" ]
                            )
                        )

                    Expect.stringContains (pageTexts pdf).[0] "begins on 2" "the section starts on page 2"
                }
                test "each section number reads its own value on the pages of the section" {
                    configure ()
                    let texts = pageTexts (Pdf.bytes sectionOverThreePages)

                    Expect.equal
                        (texts |> List.skip 1 |> List.take 3)
                        [ "first w1 t3 b2 e4"; "second w2 t3 b2 e4"; "third w3 t3 b2 e4" ]
                        "within counts from 1, the total is 3, the section begins on page 2 and ends on page 4"
                } ]
          testList
              "formatPage"
              [ equivalentDoc
                    "formatPage on pageNumber"
                    (twoPages [ Text.pageNumber |> Text.formatPage romanOrQuestion ])
                    (rawTwoPages (fun t ->
                        t.CurrentPageNumber().Format (formatter romanOrQuestion)
                        |> ignore))
                test "formatPage renders the formatted page numbers" {
                    configure ()

                    let texts =
                        pageTexts (Pdf.bytes (twoPages [ Text.span "page "; Text.pageNumber |> Text.formatPage romanOrQuestion ]))

                    Expect.equal texts [ "bodypage i"; "bodypage ii" ] "roman page numbers"
                }
                equivalentDoc
                    "formatPage with withStyle"
                    (twoPages
                        [ Text.totalPages
                          |> Text.formatPage romanOrQuestion
                          |> Text.withStyle Style.bold ])
                    (rawTwoPages (fun t ->
                        t.TotalPages().Format(formatter romanOrQuestion).Bold ()
                        |> ignore))
                test "the inner formatPage wins" {
                    configure ()

                    let texts =
                        pageTexts (
                            Pdf.bytes (
                                twoPages
                                    [ Text.pageNumber
                                      |> Text.formatPage romanOrQuestion
                                      |> Text.formatPage (fun _ -> "outer") ]
                            )
                        )

                    Expect.equal texts [ "bodyi"; "bodyii" ] "the formatter nearest the page number applies"
                }
                equivalentDoc
                    "formatPage on a span is ignored"
                    (twoPages [ Text.span "s" |> Text.formatPage romanOrQuestion ])
                    (rawTwoPages (fun t -> t.Span ("s") |> ignore)) ]
          testList
              "block settings"
              [ equivalent
                    "clampLines"
                    (richText [ Text.clampLines 2; Text.span longText ])
                    (rich (fun t ->
                        t.ClampLines (2)
                        t.Span (longText) |> ignore))
                equivalent
                    "clampLinesWith"
                    (richText [ Text.clampLinesWith 2 " [more]"; Text.span longText ])
                    (rich (fun t ->
                        t.ClampLines (2, " [more]")
                        t.Span (longText) |> ignore))
                test "clampLines cuts the text with an ellipsis" {
                    configure ()

                    let text =
                        (pageTexts (Pdf.bytes (wrapContent (richText [ Text.clampLines 2; Text.span longText ])))).[0]

                    Expect.isFalse (text.Contains "LAST") "the last word is cut"
                    Expect.isTrue (text.EndsWith "…") "the text ends with an ellipsis"
                }
                for name, part, apply in
                    [ "paragraphSpacing int", Text.paragraphSpacing 12, (fun (t: TextDescriptor) -> t.ParagraphSpacing (12f))
                      "paragraphSpacing float", Text.paragraphSpacing 7.5, (fun t -> t.ParagraphSpacing (7.5f))
                      "paragraphSpacing mm", Text.paragraphSpacing (4 * mm), (fun t -> t.ParagraphSpacing (4f, Unit.Millimetre))
                      "firstLineIndent int", Text.firstLineIndent 20, (fun t -> t.ParagraphFirstLineIndentation (20f))
                      "firstLineIndent float", Text.firstLineIndent 12.5, (fun t -> t.ParagraphFirstLineIndentation (12.5f))
                      "firstLineIndent mm", Text.firstLineIndent (8 * mm), (fun t -> t.ParagraphFirstLineIndentation (8f, Unit.Millimetre)) ] do
                    equivalent
                        name
                        (richText [ part; Text.span paragraphs ])
                        (rich (fun t ->
                            apply t
                            t.Span (paragraphs) |> ignore))

                    distinct $"{name} changes the output" (richText [ part; Text.span paragraphs ]) (rich (fun t -> t.Span (paragraphs) |> ignore))
                equivalent "raw" (richText [ Text.raw (fun t -> t.Span("r").Italic () |> ignore) ]) (rich (fun t -> t.Span("r").Italic () |> ignore))
                equivalent
                    "withStyle and formatPage leave raw unchanged"
                    (richText
                        [ Text.raw (fun t -> t.CurrentPageNumber () |> ignore)
                          |> Text.withStyle Style.bold
                          |> Text.formatPage (fun _ -> "formatted") ])
                    (rich (fun t -> t.CurrentPageNumber () |> ignore)) ] ]
