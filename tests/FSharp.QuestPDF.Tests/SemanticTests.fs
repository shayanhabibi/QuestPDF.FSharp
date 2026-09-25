module FSharp.QuestPDF.Tests.SemanticTests

open Expecto
open QuestPDF.Elements.Table
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open FSharp.QuestPDF
open FSharp.QuestPDF.Tests.Support

/// name, the semantic modifier and the fluent call it maps to.
let private tags: (string * Modifier * (IContainer -> IContainer)) list =
    [ "article", Semantic.article, (fun c -> c.SemanticArticle ())
      "section", Semantic.section, (fun c -> c.SemanticSection ())
      "division", Semantic.division, (fun c -> c.SemanticDivision ())
      "blockQuotation", Semantic.blockQuotation, (fun c -> c.SemanticBlockQuotation ())
      "caption", Semantic.caption, (fun c -> c.SemanticCaption ())
      "index", Semantic.index, (fun c -> c.SemanticIndex ())
      "tableOfContents", Semantic.tableOfContents, (fun c -> c.SemanticTableOfContents ())
      "tableOfContentsItem", Semantic.tableOfContentsItem, (fun c -> c.SemanticTableOfContentsItem ())
      "heading1", Semantic.heading1, (fun c -> c.SemanticHeading1 ())
      "heading2", Semantic.heading2, (fun c -> c.SemanticHeading2 ())
      "heading3", Semantic.heading3, (fun c -> c.SemanticHeading3 ())
      "heading4", Semantic.heading4, (fun c -> c.SemanticHeading4 ())
      "heading5", Semantic.heading5, (fun c -> c.SemanticHeading5 ())
      "heading6", Semantic.heading6, (fun c -> c.SemanticHeading6 ())
      "paragraph", Semantic.paragraph, (fun c -> c.SemanticParagraph ())
      "list", Semantic.list, (fun c -> c.SemanticList ())
      "listItem", Semantic.listItem, (fun c -> c.SemanticListItem ())
      "listLabel", Semantic.listLabel, (fun c -> c.SemanticListLabel ())
      "listItemBody", Semantic.listItemBody, (fun c -> c.SemanticListItemBody ())
      "table", Semantic.table, (fun c -> c.SemanticTable ())
      "span", Semantic.span, (fun c -> c.SemanticSpan ())
      "spanWith", Semantic.spanWith "a span", (fun c -> c.SemanticSpan ("a span"))
      "quote", Semantic.quote, (fun c -> c.SemanticQuote ())
      "code", Semantic.code, (fun c -> c.SemanticCode ())
      "link", Semantic.link "a link", (fun c -> c.SemanticLink ("a link"))
      "figure", Semantic.figure "a figure", (fun c -> c.SemanticFigure ("a figure"))
      "image", Semantic.image "an image", (fun c -> c.SemanticImage ("an image"))
      "formula", Semantic.formula "a formula", (fun c -> c.SemanticFormula ("a formula"))
      "language", Semantic.language "en-AU", (fun c -> c.SemanticLanguage ("en-AU"))
      "ignore", Semantic.ignore, (fun c -> c.SemanticIgnore ()) ]

/// A PDF/UA-1 wrapper document with pinned dates and one A5 page of the content.
let private taggedWrap (content: Content) : IDocument =
    document
        [ Meta.dated fixedDate
          Output.pdfUA
          page [ Page.size PageSizes.A5; Page.margin 20; Page.content content ] ]

/// The raw counterpart of taggedWrap.
let private taggedRaw (body: IContainer -> unit) : IDocument =
    Document
        .Create(fun container ->
            container.Page (fun p ->
                p.Size PageSizes.A5
                p.Margin 20f
                body (p.Content ()))
            |> ignore)
        .WithMetadata(pinnedMetadata ())
        .WithSettings (DocumentSettings (PDFUA_Conformance = PDFUA_Conformance.PDFUA_1))

/// Both documents generate the same PDF/UA file once their random ids are zeroed.
let private sameTagged (name: string) (wrapped: IDocument) (raw: IDocument) =
    configure ()
    let a = normalizeIds (Pdf.bytes wrapped)
    let b = normalizeIds (raw.GeneratePdf ())
    Expect.isTrue (a = b) $"{name}: tagged PDFs differ (wrapped {a.Length} bytes, raw {b.Length} bytes)"

/// A table of two columns whose first body cell carries the options.
let private headerTable (options: CellOption list) : Content =
    table
        [ Table.columns [ Table.relative 1; Table.relative 1 ]
          Table.cells [ Table.cellWith options (text "name"); Table.cell (text "value") ] ]

let private rawHeaderTable (place: ITableCellContainer -> ITableCellContainer) (c: IContainer) =
    c.Table (fun t ->
        t.ColumnsDefinition (fun d ->
            d.RelativeColumn (1f)
            d.RelativeColumn (1f))

        (place (t.Cell ())).Text ("name") |> ignore
        t.Cell().Text ("value") |> ignore)

[<Tests>]
let tests =
    testList
        "Semantic"
        [ for name, tag, apply in tags do
              equivalent $"{name} untagged" (tag >> text "s") (fun c -> (apply c).Text ("s") |> ignore)

              test $"{name} tagged" {
                  sameTagged name (taggedWrap (tag >> text "s")) (taggedRaw (fun c -> (apply c).Text ("s") |> ignore))
              }

          test "each tag writes a distinct PDF/UA file" {
              configure ()

              let rendered =
                  [ for name, tag, _ in tags -> name, normalizeIds (Pdf.bytes (taggedWrap (tag >> text "s"))) ]

              let collisions =
                  [ for a, x in rendered do
                        for b, y in rendered do
                            if a < b && x = y then
                                yield a, b ]

              Expect.isEmpty collisions "every semantic tag has a distinct tagged rendering"
          }
          equivalent
              "Cell.horizontalHeader untagged"
              (headerTable [ Cell.horizontalHeader ])
              (rawHeaderTable (fun cell -> cell.SemanticHorizontalHeader ()))
          test "Cell.horizontalHeader tagged" {
              sameTagged
                  "Cell.horizontalHeader"
                  (taggedWrap (headerTable [ Cell.horizontalHeader; Cell.at 1 1 ]))
                  (taggedRaw (rawHeaderTable (fun cell -> cell.SemanticHorizontalHeader().Row(1u).Column (1u))))
          }
          test "Cell.horizontalHeader changes the tagged output" {
              configure ()
              let tagged = normalizeIds (Pdf.bytes (taggedWrap (Semantic.table >> headerTable [ Cell.horizontalHeader ])))
              let plain = normalizeIds (Pdf.bytes (taggedWrap (Semantic.table >> headerTable [])))
              Expect.isFalse (tagged = plain) "a row header should change the PDF/UA file"
          }
          test "nested tags match the fluent chain" {
              let content =
                  Semantic.language "en-AU"
                  >> Semantic.list
                  >> column
                        [ Semantic.listItem
                        >> row
                            [ Row.auto (Semantic.listLabel >> text "1.")
                              Row.fill (Semantic.listItemBody >> text "first") ] ]

              sameTagged
                  "nested"
                  (taggedWrap content)
                  (taggedRaw (fun c ->
                      c.SemanticLanguage("en-AU").SemanticList().Column (fun col ->
                          col.Item().SemanticListItem().Row (fun r ->
                              r.AutoItem().SemanticListLabel().Text ("1.") |> ignore
                              r.RelativeItem().SemanticListItemBody().Text ("first") |> ignore))))
          } ]
