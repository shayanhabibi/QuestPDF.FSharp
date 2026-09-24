module QuestPDF.FSharp.Tests.PagingTests

open System
open Expecto
open QuestPDF.Elements
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open QuestPDF.FSharp
open QuestPDF.FSharp.Tests.Support

/// Eighty numbered lines: enough to fill more than one A5 page.
let private longColumn: Content = column [ for i in 1..80 -> text $"line {i}" ]

let private rawLongColumn (c: IContainer) =
    c.Column (fun col ->
        for i in 1..80 do
            col.Item().Text ($"line {i}") |> ignore)

/// An A5 document with a header and eighty lines of content.
let private withHeader (header: Content) =
    wrapPage [ Page.size PageSizes.A5; Page.header header; Page.content longColumn ]

let private rawWithHeader (header: IContainer -> unit) =
    rawPage (fun p ->
        p.Size PageSizes.A5
        header (p.Header ())
        rawLongColumn (p.Content ()))

/// The modifiers that take no argument, with the raw call each maps to.
let private pagingModifiers: (string * Modifier * (IContainer -> IContainer)) list =
    [ "showEntire", showEntire, (fun c -> c.ShowEntire ())
      "preventPageBreak", preventPageBreak, (fun c -> c.PreventPageBreak ())
      "showOnce", showOnce, (fun c -> c.ShowOnce ())
      "skipOnce", skipOnce, (fun c -> c.SkipOnce ())
      "repeat", repeat, (fun c -> c.Repeat ())
      "stopPaging", stopPaging, (fun c -> c.StopPaging ()) ]

let private isEven (context: ShowIfContext) =
    context.PageNumber % 2 = 0

[<Tests>]
let tests =
    testList
        "Paging"
        [ for name, modifier, apply in pagingModifiers do
              equivalent
                  name
                  (column
                      [ text "first"
                        modifier
                        >> background Colors.Grey.Lighten3
                        >> text "second" ])
                  (fun c ->
                      c.Column (fun col ->
                          col.Item().Text ("first") |> ignore

                          (apply (col.Item ())).Background(Colors.Grey.Lighten3).Text ("second")
                          |> ignore))
          test "showOnce and skipOnce in a header" {
              configure ()

              let texts =
                  pageTexts (Pdf.bytes (withHeader (column [ showOnce >> text "ONCE"; skipOnce >> text "SKIP" ])))

              Expect.equal texts.Length 2 "two pages"
              Expect.equal [ for t in texts -> t.Contains "ONCE", t.Contains "SKIP" ] [ true, false; false, true ] "ONCE on page 1, SKIP after"
          }
          equivalentDoc
              "header with showOnce and skipOnce"
              (withHeader (column [ showOnce >> text "ONCE"; skipOnce >> text "SKIP" ]))
              (rawWithHeader (fun h ->
                  h.Column (fun col ->
                      col.Item().ShowOnce().Text ("ONCE") |> ignore
                      col.Item().SkipOnce().Text ("SKIP") |> ignore)))
          test "repeat draws a finished item on every page" {
              configure ()

              let sideTexts (side: Modifier) =
                  pageTexts (Pdf.bytes (wrapContent (row [ Row.constant 60 (side >> text "SIDE"); Row.fill longColumn ])))
                  |> List.map (fun t -> t.Contains "SIDE")

              Expect.equal (sideTexts repeat) [ true; true; true ] "repeated on every page"
              Expect.equal (sideTexts id) [ true; false; false ] "drawn once without repeat"
          }
          test "stopPaging keeps the content on one page" {
              configure ()
              Expect.equal (pageCount (Pdf.bytes (wrapContent (stopPaging >> longColumn)))) 1 "one page"
          }
          equivalent "ensureSpace int" (column [ text "a"; ensureSpace 200 >> text "b" ]) (fun c ->
              c.Column (fun col ->
                  col.Item().Text ("a") |> ignore
                  col.Item().EnsureSpace(200f).Text ("b") |> ignore))
          equivalent "ensureSpace float" (column [ text "a"; ensureSpace 120.5 >> text "b" ]) (fun c ->
              c.Column (fun col ->
                  col.Item().Text ("a") |> ignore

                  col.Item().EnsureSpace(120.5f).Text ("b")
                  |> ignore))
          test "ensureSpace moves content that would break to the next page" {
              configure ()

              let firstPage (modifier: Modifier) =
                  pageTexts (Pdf.bytes (wrapContent (column [ height 400 >> text "top"; modifier >> longColumn ])))
                  |> List.head

              Expect.equal (firstPage (ensureSpace 200)) "top" "the lines start on page 2"
              Expect.stringStarts (firstPage id) "topline 1" "without ensureSpace the lines start on page 1"
          }
          equivalent "showIf true" (showIf true >> text "shown") (fun c -> c.ShowIf(true).Text ("shown") |> ignore)
          equivalent "showIf false" (showIf false >> text "hidden") (fun c -> c.ShowIf(false).Text ("hidden") |> ignore)
          distinct "showIf false hides the content" (showIf false >> text "hidden") (fun c -> c.Text ("hidden") |> ignore)
          equivalentDoc
              "showWhen"
              (wrapPage
                  [ Page.size PageSizes.A5
                    Page.content longColumn
                    Page.footer (showWhen isEven >> text "EVEN") ])
              (rawPage (fun p ->
                  p.Size PageSizes.A5
                  rawLongColumn (p.Content ())

                  p.Footer().ShowIf(Predicate isEven).Text ("EVEN")
                  |> ignore))
          test "showWhen on even pages" {
              configure ()

              let texts =
                  pageTexts (
                      Pdf.bytes (
                          wrapPage
                              [ Page.size PageSizes.A6
                                Page.content longColumn
                                Page.footer (showWhen isEven >> text "EVEN") ]
                      )
                  )

              Expect.isGreaterThan texts.Length 2 "at least three pages"
              Expect.equal [ for t in texts -> t.Contains "EVEN" ] [ for i in 1 .. texts.Length -> i % 2 = 0 ] "EVEN on even pages only"
          }
          testList
              "page size"
              [ equivalentPage
                    "minSize and maxSize"
                    [ Page.minSize PageSizes.A6
                      Page.maxSize PageSizes.A4
                      Page.content (text "flex") ]
                    (fun p ->
                        p.MinSize PageSizes.A6
                        p.MaxSize PageSizes.A4
                        p.Content().Text ("flex") |> ignore)
                equivalentPage "continuous int" [ Page.continuous 300; Page.content longColumn ] (fun p ->
                    p.ContinuousSize (300f)
                    rawLongColumn (p.Content ()))
                equivalentPage "continuous cm" [ Page.continuous (10 * cm); Page.content longColumn ] (fun p ->
                    p.ContinuousSize (10f, Unit.Centimetre)
                    rawLongColumn (p.Content ()))
                test "continuous is one page" {
                    configure ()
                    let pdf = Pdf.bytes (wrapPage [ Page.continuous 300; Page.content longColumn ])
                    Expect.equal (pageCount pdf) 1 "one page"
                } ] ]
