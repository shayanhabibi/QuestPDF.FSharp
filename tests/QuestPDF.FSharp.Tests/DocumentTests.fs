module QuestPDF.FSharp.Tests.DocumentTests

open System
open System.Threading
open Expecto
open QuestPDF.Drawing.Exceptions
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open QuestPDF.FSharp
open QuestPDF.FSharp.Tests.Support

/// An A5 page with a shaded body that fills the content area, plus the part under test before the content.
let private pageWith (name: string) (part: PagePart) (raw: PageDescriptor -> unit) =
    equivalentPage
        name
        [ Page.size PageSizes.A5
          part
          Page.content (background Colors.Grey.Lighten3 >> text "body") ]
        (fun p ->
            p.Size PageSizes.A5
            raw p

            p.Content().Background(Colors.Grey.Lighten3).Text ("body")
            |> ignore)

/// The five page slots with their raw QuestPDF accessors.
let private slots: (string * (PageDescriptor -> IContainer)) list =
    [ "header", (fun p -> p.Header ())
      "content", (fun p -> p.Content ())
      "footer", (fun p -> p.Footer ())
      "background", (fun p -> p.Background ())
      "foreground", (fun p -> p.Foreground ()) ]

/// Fills every slot except the named one with its own name, through raw QuestPDF calls.
let private fillOthers (name: string) (p: PageDescriptor) =
    for other, get in slots do
        if other <> name then
            (get p).Text (other) |> ignore

/// A page with every slot filled: the named slot through the wrapper, the others raw. QuestPDF raises on a slot
/// filled twice, so a wrapper slot mapped to another slot fails the comparison.
let private slot (name: string) (part: Content -> PagePart) (raw: PageDescriptor -> IContainer) =
    equivalentPage
        name
        [ Page.size PageSizes.A5
          Page.margin 20
          fillOthers name
          part (text "tested") ]
        (fun p ->
            p.Size PageSizes.A5
            p.Margin 20f
            fillOthers name p
            (raw p).Text ("tested") |> ignore)

let pageParts =
    testList
        "page"
        [ pageWith "size" (Page.size PageSizes.A4) (fun p -> p.Size PageSizes.A4)
          pageWith "sizeOf int" (Page.sizeOf 300 400) (fun p -> p.Size (300f, 400f))
          pageWith "sizeOf cm" (Page.sizeOf (10 * cm) (15 * cm)) (fun p -> p.Size (10f, 15f, Unit.Centimetre))
          pageWith "sizeOf mixed units is points" (Page.sizeOf (100 * mm) 300.5) (fun p -> p.Size (Length.points (100 * mm), 300.5f))
          pageWith "margin cm" (Page.margin (2 * cm)) (fun p -> p.Margin (2f, Unit.Centimetre))
          pageWith "marginV cm" (Page.marginV (1 * cm)) (fun p -> p.MarginVertical (1f, Unit.Centimetre))
          pageWith "marginH cm" (Page.marginH (1.5 * cm)) (fun p -> p.MarginHorizontal (1.5f, Unit.Centimetre))
          pageWith "marginTop cm" (Page.marginTop (1 * cm)) (fun p -> p.MarginTop (1f, Unit.Centimetre))
          pageWith "marginBottom cm" (Page.marginBottom (1 * cm)) (fun p -> p.MarginBottom (1f, Unit.Centimetre))
          pageWith "marginLeft cm" (Page.marginLeft (1 * cm)) (fun p -> p.MarginLeft (1f, Unit.Centimetre))
          pageWith "marginRight cm" (Page.marginRight (1 * cm)) (fun p -> p.MarginRight (1f, Unit.Centimetre))
          pageWith "margin int" (Page.margin 30) (fun p -> p.Margin 30f)
          pageWith "color" (Page.color Colors.Amber.Lighten4) (fun p -> p.PageColor Colors.Amber.Lighten4)
          pageWith "textStyle" (Page.textStyle (Style.size 16)) (fun p -> p.DefaultTextStyle (fun (s: TextStyle) -> s.FontSize 16f))
          pageWith "rightToLeft" Page.rightToLeft (fun p -> p.ContentFromRightToLeft ())
          pageWith "leftToRight" Page.leftToRight (fun p -> p.ContentFromLeftToRight ())
          slot "header" Page.header (fun p -> p.Header ())
          slot "content" Page.content (fun p -> p.Content ())
          slot "footer" Page.footer (fun p -> p.Footer ())
          slot "background" Page.background (fun p -> p.Background ())
          slot "foreground" Page.foreground (fun p -> p.Foreground ())
          test "a slot filled twice raises DocumentComposeException" {
              configure ()

              for name, get in slots do
                  Expect.throwsT<DocumentComposeException>
                      (fun () ->
                          (rawPage (fun p ->
                              fillOthers "" p
                              (get p).Text ("again") |> ignore))
                              .GeneratePdf ()
                          |> ignore)
                      $"{name} filled twice"
          }
          test "each margin side renders differently" {
              configure ()

              let margins: (string * (PageDescriptor -> unit)) list =
                  [ "none", ignore
                    "margin", (fun p -> p.Margin (1f, Unit.Centimetre))
                    "marginV", (fun p -> p.MarginVertical (1f, Unit.Centimetre))
                    "marginH", (fun p -> p.MarginHorizontal (1f, Unit.Centimetre))
                    "marginTop", (fun p -> p.MarginTop (1f, Unit.Centimetre))
                    "marginBottom", (fun p -> p.MarginBottom (1f, Unit.Centimetre))
                    "marginLeft", (fun p -> p.MarginLeft (1f, Unit.Centimetre))
                    "marginRight", (fun p -> p.MarginRight (1f, Unit.Centimetre)) ]

              let rendered =
                  [ for name, margin in margins ->
                        name,
                        (rawPage (fun p ->
                            p.Size PageSizes.A5
                            margin p

                            p.Content().Background(Colors.Grey.Lighten3).Text ("body")
                            |> ignore))
                            .GeneratePdf () ]

              let collisions =
                  [ for a, x in rendered do
                        for b, y in rendered do
                            if a < b && x = y then
                                yield a, b ]

              Expect.isEmpty collisions "every margin setting has a distinct rendering"
          }
          test "two contents on a page raise DocumentComposeException" {
              configure ()

              Expect.throwsT<DocumentComposeException>
                  (fun () ->
                      wrapPage [ Page.content (text "a"); Page.content (text "b") ]
                      |> Pdf.bytes
                      |> ignore)
                  "a page has one content slot"
          } ]

let documents =
    let created = DateTimeOffset (2025, 5, 6, 7, 8, 9, TimeSpan.FromHours 2.)
    let modified = DateTimeOffset (2025, 6, 7, 8, 9, 10, TimeSpan.Zero)

    testList
        "document"
        [ equivalentDoc
              "several pages"
              (document
                  [ Meta.dated fixedDate
                    page [ Page.size PageSizes.A5; Page.content (text "one") ]
                    page [ Page.size PageSizes.A6; Page.content (text "two") ] ])
              (rawDocument (fun dc ->
                  dc
                      .Page(fun p ->
                          p.Size PageSizes.A5
                          p.Content().Text ("one") |> ignore)
                      .Page (fun p ->
                          p.Size PageSizes.A6
                          p.Content().Text ("two") |> ignore)
                  |> ignore))
          test "Meta parts set the metadata" {
              let metadata =
                  (document
                      [ Meta.title "Title"
                        Meta.author "Author"
                        Meta.subject "Subject"
                        Meta.keywords "a, b"
                        Meta.creator "Creator"
                        Meta.producer "Producer"
                        Meta.language "en-AU"
                        Meta.created created
                        Meta.modified modified ])
                      .GetMetadata ()

              Expect.equal
                  (metadata.Title, metadata.Author, metadata.Subject, metadata.Keywords, metadata.Creator, metadata.Producer, metadata.Language)
                  ("Title", "Author", "Subject", "a, b", "Creator", "Producer", "en-AU")
                  "strings"

              Expect.equal (metadata.CreationDate, metadata.ModifiedDate) (created, modified) "dates"
          }
          test "Meta.dated sets both dates" {
              let metadata = (document [ Meta.dated created ]).GetMetadata ()
              Expect.equal (metadata.CreationDate, metadata.ModifiedDate) (created, created) "dates"
          }
          test "Meta.dated gives identical bytes twice" {
              configure ()

              let build () =
                  document [ Meta.dated fixedDate; page [ Page.content (text "same") ] ]

              Expect.isTrue (Pdf.bytes (build ()) = Pdf.bytes (build ())) "pinned dates are reproducible"
          }
          test "without Meta.dated the bytes differ across seconds" {
              configure ()

              let build () =
                  document [ page [ Page.content (text "now") ] ]

              let first = Pdf.bytes (build ())
              Thread.Sleep 1100
              Expect.isFalse (first = Pdf.bytes (build ())) "the default dates are DateTimeOffset.Now"
          }
          test "without Meta.dated one document value generates the same bytes across seconds" {
              configure ()
              let built = document [ page [ Page.content (text "now") ] ]
              let first = Pdf.bytes built
              Thread.Sleep 1100
              Expect.isTrue (first = Pdf.bytes built) "the dates are fixed when the document is built"
          } ]

[<Tests>]
let tests = testList "Document" [ pageParts; documents ]
