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

/// The modifiers that take no argument, with the raw call each maps to, and whether the modifier changes the split block.
let private pagingModifiers: (string * Modifier * (IContainer -> IContainer) * bool) list =
    [ "showEntire", showEntire, (fun c -> c.ShowEntire ()), true
      "preventPageBreak", preventPageBreak, (fun c -> c.PreventPageBreak ()), true
      "showOnce", showOnce, (fun c -> c.ShowOnce ()), false
      "skipOnce", skipOnce, (fun c -> c.SkipOnce ()), true
      "repeat", repeat, (fun c -> c.Repeat ()), false
      "stopPaging", stopPaging, (fun c -> c.StopPaging ()), true ]

/// A 400 pt block followed by a block of numbered lines under the modifier; the lines break across the first page.
let private splitBlock (lines: int) (modifier: Modifier) : Content =
    column
        [ height 400 >> text "top"
          modifier
          >> column [ for i in 1..lines -> text $"line {i}" ] ]

let private rawSplitBlock (lines: int) (apply: IContainer -> IContainer) (c: IContainer) =
    c.Column (fun col ->
        col.Item().Height(400f).Text ("top") |> ignore

        (apply (col.Item ()))
            .Column (fun inner ->
                for i in 1..lines do
                    inner.Item().Text ($"line {i}") |> ignore))

let private isEven (context: ShowIfContext) =
    context.PageNumber % 2 = 0

[<Tests>]
let tests =
    testList
        "Paging"
        [ for name, modifier, apply, changesSplit in pagingModifiers do
              equivalent name (splitBlock 20 modifier) (rawSplitBlock 20 apply)

              if changesSplit then
                  distinct $"{name} changes the split block" (splitBlock 20 modifier) (rawSplitBlock 20 id)
          test "preventPageBreak moves a block that would break to the next page" {
              configure ()
              let kept = pageTexts (Pdf.bytes (wrapContent (splitBlock 20 preventPageBreak)))
              let split = pageTexts (Pdf.bytes (wrapContent (splitBlock 20 id)))
              Expect.equal kept[0] "top" "the lines start on page 2"
              Expect.stringStarts split[0] "topline 1" "without preventPageBreak the lines start on page 1"
          }
          test "preventPageBreak breaks a block taller than a page" {
              configure ()
              let texts = pageTexts (Pdf.bytes (wrapContent (splitBlock 60 preventPageBreak)))
              Expect.equal texts.Length 3 "three pages"
              Expect.equal texts[0] "top" "the lines start on page 2"
              Expect.stringStarts texts[1] "line 1line 2" "the lines continue across pages"
          }
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
          test "ensureSpace keeps content that fits on the current page" {
              configure ()

              let texts =
                  pageTexts (Pdf.bytes (wrapContent (column [ text "a"; ensureSpace 1000 >> text "b" ])))

              Expect.equal texts [ "ab" ] "one page, although less than 1000 pt remains"
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
