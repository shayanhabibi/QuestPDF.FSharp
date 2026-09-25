module FSharp.QuestPDF.Tests.ModifierTests

open Expecto
open QuestPDF.Drawing.Exceptions
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open FSharp.QuestPDF
open FSharp.QuestPDF.Tests.Support

/// name, the modifier at 10 (int), 7.5 (float) and 3 mm, and the raw call it maps to.
let private lengthModifiers: (string * Modifier * Modifier * Modifier * (IContainer -> float32 -> Unit -> IContainer)) list =
    [ "padding", padding 10, padding 7.5, padding (3 * mm), (fun c v u -> c.Padding (v, u))
      "paddingV", paddingV 10, paddingV 7.5, paddingV (3 * mm), (fun c v u -> c.PaddingVertical (v, u))
      "paddingH", paddingH 10, paddingH 7.5, paddingH (3 * mm), (fun c v u -> c.PaddingHorizontal (v, u))
      "paddingTop", paddingTop 10, paddingTop 7.5, paddingTop (3 * mm), (fun c v u -> c.PaddingTop (v, u))
      "paddingBottom", paddingBottom 10, paddingBottom 7.5, paddingBottom (3 * mm), (fun c v u -> c.PaddingBottom (v, u))
      "paddingLeft", paddingLeft 10, paddingLeft 7.5, paddingLeft (3 * mm), (fun c v u -> c.PaddingLeft (v, u))
      "paddingRight", paddingRight 10, paddingRight 7.5, paddingRight (3 * mm), (fun c v u -> c.PaddingRight (v, u)) ]

/// A background outside the text; each padding side moves an edge of the background or the text.
let private shaded (modifier: Modifier) : Content =
    modifier
    >> background Colors.Grey.Lighten3
    >> text "p"

let private rawShaded (apply: IContainer -> IContainer) (c: IContainer) =
    (apply c).Background(Colors.Grey.Lighten3).Text ("p")
    |> ignore

[<Tests>]
let tests =
    testList
        "Modifiers"
        [ for name, ofInt, ofFloat, ofMm, apply in lengthModifiers do
              equivalent $"{name} int" (shaded ofInt) (rawShaded (fun c -> apply c 10f Unit.Point))
              equivalent $"{name} float" (shaded ofFloat) (rawShaded (fun c -> apply c 7.5f Unit.Point))
              equivalent $"{name} mm" (shaded ofMm) (rawShaded (fun c -> apply c 3f Unit.Millimetre))
              distinct $"{name} changes the output" (shaded ofInt) (rawShaded id)
          test "each padding side renders differently" {
              configure ()

              let rendered =
                  [ for name, _, _, _, apply in lengthModifiers -> name, (rawContent (rawShaded (fun c -> apply c 10f Unit.Point))).GeneratePdf () ]

              let collisions =
                  [ for a, x in rendered do
                        for b, y in rendered do
                            if a < b && x = y then
                                yield a, b ]

              Expect.isEmpty collisions "every padding modifier has a distinct rendering"
          }
          equivalent "background" (background Colors.Grey.Lighten3 >> text "b") (fun c ->
              c.Background(Colors.Grey.Lighten3).Text ("b")
              |> ignore)
          equivalent "textStyle is DefaultTextStyle" (textStyle (Style.size 18) >> text "t") (fun c ->
              c.DefaultTextStyle(fun (s: TextStyle) -> s.FontSize 18f).Text ("t")
              |> ignore)
          distinct
              "padding then background differs from background then padding"
              (padding 10
               >> background Colors.Grey.Lighten3
               >> text "o")
              (fun c ->
                  c.Background(Colors.Grey.Lighten3).Padding(10f).Text ("o")
                  |> ignore) ]

/// A grey box hugging "box content" at the top left of the page, so size, border and corner modifiers are visible.
let private boxed (modifier: Modifier) : Content =
    modify (fun c -> c.AlignLeft().AlignTop ())
    >> modifier
    >> background Colors.Grey.Lighten3
    >> text "box content"

let private rawBoxed (apply: IContainer -> IContainer) (c: IContainer) =
    (apply (c.AlignLeft().AlignTop ())).Background(Colors.Grey.Lighten3).Text ("box content")
    |> ignore

/// A grey box filling the content slot, so alignment and shrink modifiers are visible.
let private plain (modifier: Modifier) : Content =
    modifier
    >> background Colors.Grey.Lighten3
    >> text "box content"

let private rawPlain (apply: IContainer -> IContainer) (c: IContainer) =
    (apply c).Background(Colors.Grey.Lighten3).Text ("box content")
    |> ignore

/// The int, float and millimetre forms of a length modifier against the raw call, plus a control against no modifier.
let private lengthCase
    name
    (ofInt: Modifier, i: int)
    (ofFloat: Modifier, f: float)
    (ofMm: Modifier, m: float)
    (apply: IContainer -> float32 -> Unit -> IContainer)
    =
    testList
        name
        [ equivalent "int" (boxed ofInt) (rawBoxed (fun c -> apply c (float32 i) Unit.Point))
          equivalent "float" (boxed ofFloat) (rawBoxed (fun c -> apply c (float32 f) Unit.Point))
          equivalent "mm" (boxed ofMm) (rawBoxed (fun c -> apply c (float32 m) Unit.Millimetre))
          distinct "changes the output" (boxed ofInt) (rawBoxed id) ]

/// Asserts that every named raw rendering differs from every other.
let private pairwiseDistinct name (cases: (string * (IContainer -> unit)) list) =
    test name {
        configure ()

        let rendered =
            [ for label, draw in cases -> label, (rawContent draw).GeneratePdf () ]

        let collisions =
            [ for a, x in rendered do
                  for b, y in rendered do
                      if a < b && x = y then
                          yield a, b ]

        Expect.isEmpty collisions "every case renders differently"
    }

let private plainCases: (string * Modifier * (IContainer -> IContainer)) list =
    [ "alignLeft", alignLeft, (fun c -> c.AlignLeft ())
      "alignCenter", alignCenter, (fun c -> c.AlignCenter ())
      "alignRight", alignRight, (fun c -> c.AlignRight ())
      "alignTop", alignTop, (fun c -> c.AlignTop ())
      "alignMiddle", alignMiddle, (fun c -> c.AlignMiddle ())
      "alignBottom", alignBottom, (fun c -> c.AlignBottom ())
      "shrink", shrink, (fun c -> c.Shrink ())
      "shrinkH", shrinkH, (fun c -> c.ShrinkHorizontal ())
      "shrinkV", shrinkV, (fun c -> c.ShrinkVertical ())
      "unconstrained", unconstrained, (fun c -> c.Unconstrained ()) ]

let private boxedCases: (string * Modifier * (IContainer -> IContainer)) list =
    [ "extend", extend, (fun c -> c.Extend ())
      "extendH", extendH, (fun c -> c.ExtendHorizontal ())
      "extendV", extendV, (fun c -> c.ExtendVertical ())
      "borderInside", border 4 >> borderInside, (fun c -> c.Border(4f).BorderAlignmentInside ())
      "borderMiddle", border 4 >> borderMiddle, (fun c -> c.Border(4f).BorderAlignmentMiddle ())
      "borderOutside", border 4 >> borderOutside, (fun c -> c.Border(4f).BorderAlignmentOutside ())
      "borderColor", border 4 >> borderColor Colors.Red.Medium, (fun c -> c.Border(4f).BorderColor (Colors.Red.Medium))
      "aspectRatio int", aspectRatio 2, (fun c -> c.AspectRatio (2f))
      "aspectRatio float", aspectRatio 1.5, (fun c -> c.AspectRatio (1.5f))
      "aspectRatioWith FitHeight", aspectRatioWith AspectRatioOption.FitHeight 0.5, (fun c -> c.AspectRatio (0.5f, AspectRatioOption.FitHeight))
      "aspectRatioWith FitArea", aspectRatioWith AspectRatioOption.FitArea 0.5, (fun c -> c.AspectRatio (0.5f, AspectRatioOption.FitArea)) ]

let private rawLines (t: TextDescriptor) =
    for i in 1..12 do
        t.Line ($"line {i}") |> ignore

/// A block of twelve lines after a filler, taller than the space left on the first page.
let private splitBlock (keep: Modifier) : Content =
    raw (fun c ->
        c.Column (fun col ->
            col.Item().Height(440f).Text ("filler") |> ignore
            Content.run (keep >> raw (fun c -> c.Text rawLines)) (col.Item ())))

let private rawSplitBlock (keep: IContainer -> IContainer) (c: IContainer) =
    c.Column (fun col ->
        col.Item().Height(440f).Text ("filler") |> ignore
        (keep (col.Item ())).Text rawLines)

let private linked (sectionLinkModifier: Modifier) (sectionModifier: Modifier) : Content =
    raw (fun c ->
        c.Column (fun col ->
            Content.run (sectionLinkModifier >> text "go") (col.Item ())
            col.Item().PageBreak ()
            Content.run (sectionModifier >> text "here") (col.Item ())))

[<Tests>]
let boxTests =
    testList
        "Box modifiers"
        [ testList
              "sizes"
              [ lengthCase "width" (width 60, 60) (width 45.5, 45.5) (width (20 * mm), 20.) (fun c v u -> c.Width (v, u))
                lengthCase "minWidth" (minWidth 120, 120) (minWidth 95.5, 95.5) (minWidth (40 * mm), 40.) (fun c v u -> c.MinWidth (v, u))
                lengthCase "maxWidth" (maxWidth 30, 30) (maxWidth 25.5, 25.5) (maxWidth (10 * mm), 10.) (fun c v u -> c.MaxWidth (v, u))
                lengthCase "height" (height 60, 60) (height 45.5, 45.5) (height (20 * mm), 20.) (fun c v u -> c.Height (v, u))
                lengthCase "minHeight" (minHeight 80, 80) (minHeight 65.5, 65.5) (minHeight (30 * mm), 30.) (fun c v u -> c.MinHeight (v, u))
                lengthCase
                    "maxHeight"
                    (maxHeight 30 >> extendV, 30)
                    (maxHeight 25.5 >> extendV, 25.5)
                    (maxHeight (10 * mm) >> extendV, 10.)
                    (fun c v u -> c.MaxHeight(v, u).ExtendVertical ()) ]
          testList
              "borders"
              [ lengthCase "border" (border 2, 2) (border 1.5, 1.5) (border (1 * mm), 1.) (fun c v u -> c.Border (v, u))
                lengthCase "borderV" (borderV 2, 2) (borderV 1.5, 1.5) (borderV (1 * mm), 1.) (fun c v u -> c.BorderVertical (v, u))
                lengthCase "borderH" (borderH 2, 2) (borderH 1.5, 1.5) (borderH (1 * mm), 1.) (fun c v u -> c.BorderHorizontal (v, u))
                lengthCase "borderTop" (borderTop 2, 2) (borderTop 1.5, 1.5) (borderTop (1 * mm), 1.) (fun c v u -> c.BorderTop (v, u))
                lengthCase "borderBottom" (borderBottom 2, 2) (borderBottom 1.5, 1.5) (borderBottom (1 * mm), 1.) (fun c v u -> c.BorderBottom (v, u))
                lengthCase "borderLeft" (borderLeft 2, 2) (borderLeft 1.5, 1.5) (borderLeft (1 * mm), 1.) (fun c v u -> c.BorderLeft (v, u))
                lengthCase "borderRight" (borderRight 2, 2) (borderRight 1.5, 1.5) (borderRight (1 * mm), 1.) (fun c v u -> c.BorderRight (v, u))
                pairwiseDistinct
                    "each border side renders differently"
                    [ for name, apply in
                          [ "border", (fun (c: IContainer) -> c.Border (2f))
                            "borderV", (fun c -> c.BorderVertical (2f))
                            "borderH", (fun c -> c.BorderHorizontal (2f))
                            "borderTop", (fun c -> c.BorderTop (2f))
                            "borderBottom", (fun c -> c.BorderBottom (2f))
                            "borderLeft", (fun c -> c.BorderLeft (2f))
                            "borderRight", (fun c -> c.BorderRight (2f)) ] -> name, rawBoxed apply ]
                pairwiseDistinct
                    "the border alignments render differently"
                    [ "inside", rawBoxed (fun c -> c.Border(4f).BorderAlignmentInside ())
                      "middle", rawBoxed (fun c -> c.Border(4f).BorderAlignmentMiddle ())
                      "outside", rawBoxed (fun c -> c.Border(4f).BorderAlignmentOutside ()) ]
                distinct "borderColor changes the output" (boxed (border 4 >> borderColor Colors.Red.Medium)) (rawBoxed (fun c -> c.Border (4f))) ]
          testList
              "corners"
              [ lengthCase "cornerRadius" (cornerRadius 10, 10) (cornerRadius 7.5, 7.5) (cornerRadius (3 * mm), 3.) (fun c v u ->
                    c.CornerRadius (v, u))
                lengthCase
                    "cornerRadiusTopLeft"
                    (cornerRadiusTopLeft 10, 10)
                    (cornerRadiusTopLeft 7.5, 7.5)
                    (cornerRadiusTopLeft (3 * mm), 3.)
                    (fun c v u -> c.CornerRadiusTopLeft (v, u))
                lengthCase
                    "cornerRadiusTopRight"
                    (cornerRadiusTopRight 10, 10)
                    (cornerRadiusTopRight 7.5, 7.5)
                    (cornerRadiusTopRight (3 * mm), 3.)
                    (fun c v u -> c.CornerRadiusTopRight (v, u))
                lengthCase
                    "cornerRadiusBottomLeft"
                    (cornerRadiusBottomLeft 10, 10)
                    (cornerRadiusBottomLeft 7.5, 7.5)
                    (cornerRadiusBottomLeft (3 * mm), 3.)
                    (fun c v u -> c.CornerRadiusBottomLeft (v, u))
                lengthCase
                    "cornerRadiusBottomRight"
                    (cornerRadiusBottomRight 10, 10)
                    (cornerRadiusBottomRight 7.5, 7.5)
                    (cornerRadiusBottomRight (3 * mm), 3.)
                    (fun c v u -> c.CornerRadiusBottomRight (v, u))
                pairwiseDistinct
                    "each corner renders differently"
                    [ for name, apply in
                          [ "all", (fun (c: IContainer) -> c.CornerRadius (10f))
                            "topLeft", (fun c -> c.CornerRadiusTopLeft (10f))
                            "topRight", (fun c -> c.CornerRadiusTopRight (10f))
                            "bottomLeft", (fun c -> c.CornerRadiusBottomLeft (10f))
                            "bottomRight", (fun c -> c.CornerRadiusBottomRight (10f)) ] -> name, rawBoxed apply ] ]
          testList
              "placement"
              [ for name, modifier, apply in plainCases do
                    equivalent name (plain modifier) (rawPlain apply)
                pairwiseDistinct
                    "the alignments and no alignment render differently"
                    (("none", rawPlain id)
                     :: [ for name, _, apply in plainCases do
                              if name.StartsWith "align" then
                                  yield name, rawPlain apply ])
                pairwiseDistinct
                    "the shrink modifiers and no modifier render differently"
                    (("none", rawPlain id)
                     :: [ for name, _, apply in plainCases do
                              if name.StartsWith "shrink" then
                                  yield name, rawPlain apply ])
                distinct "unconstrained changes the output" (plain unconstrained) (rawPlain id)
                for name, modifier, apply in boxedCases do
                    equivalent name (boxed modifier) (rawBoxed apply)
                pairwiseDistinct
                    "the extend modifiers, the aspect ratios and no modifier render differently"
                    [ "none", rawBoxed id
                      "extend", rawBoxed (fun c -> c.Extend ())
                      "extendH", rawBoxed (fun c -> c.ExtendHorizontal ())
                      "extendV", rawBoxed (fun c -> c.ExtendVertical ())
                      "aspectRatio", rawBoxed (fun c -> c.AspectRatio (2f))
                      "aspectRatio FitHeight", rawBoxed (fun c -> c.AspectRatio (0.5f, AspectRatioOption.FitHeight)) ]
                equivalent "scaleToFit" (height 10 >> scaleToFit >> text "box content") (fun c ->
                    c.Height(10f).ScaleToFit().Text ("box content")
                    |> ignore)
                test "without scaleToFit, the text overflows its height" {
                    configure ()

                    Expect.throwsT<DocumentLayoutException>
                        (fun () ->
                            (rawContent (fun c -> c.Height(10f).Text ("box content") |> ignore)).GeneratePdf ()
                            |> ignore)
                        "the text fits without scaling"
                }
                equivalent "showEntire" (splitBlock showEntire) (rawSplitBlock (fun c -> c.ShowEntire ()))
                test "showEntire moves the whole block to the next page" {
                    configure ()
                    let kept = pageTexts (Pdf.bytes (wrapContent (splitBlock showEntire)))
                    let split = pageTexts ((rawContent (rawSplitBlock id)).GeneratePdf ())
                    Expect.isFalse (kept[0].Contains "line 1") "the kept block starts on page 2"
                    Expect.isTrue (split[0].Contains "line 1") "without showEntire, the block starts on page 1"
                } ]
          testList
              "links"
              [ equivalent "hyperlink" (hyperlink "https://example.com" >> text "site") (fun c ->
                    c.Hyperlink("https://example.com").Text ("site")
                    |> ignore)
                test "hyperlink targets the URL" {
                    configure ()
                    let pdf = Pdf.bytes (wrapContent (hyperlink "https://example.com" >> text "site"))

                    Expect.equal
                        (hyperlinks pdf
                         |> List.map (fun (page, _, uri) -> page, uri))
                        [ (1, "https://example.com") ]
                        "one link on page 1"
                }
                equivalent "section and sectionLink" (linked (sectionLink "target") (section "target")) (fun c ->
                    c.Column (fun col ->
                        col.Item().SectionLink("target").Text ("go")
                        |> ignore

                        col.Item().PageBreak ()

                        col.Item().Section("target").Text ("here")
                        |> ignore))
                test "sectionLink resolves to the page of the section" {
                    configure ()
                    let pdf = Pdf.bytes (wrapContent (linked (sectionLink "target") (section "target")))
                    Expect.equal (internalLinks pdf) [ (1, 2) ] "a link on page 1 to page 2"
                } ] ]
