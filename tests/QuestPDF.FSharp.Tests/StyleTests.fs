module QuestPDF.FSharp.Tests.StyleTests

open System
open Expecto
open QuestPDF.Drawing.Exceptions
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open QuestPDF.FSharp
open QuestPDF.FSharp.Tests.Support

/// Text styled through Style.toTextStyle, so these tests depend on no other text element.
let private styled (style: Style) =
    raw (fun c ->
        c.Text("styled").Style (Style.toTextStyle style)
        |> ignore)

let private check name (style: Style) (expected: TextBlockDescriptor -> TextBlockDescriptor) =
    equivalent name (styled style) (fun c -> c.Text ("styled") |> expected |> ignore)

/// Text from a fixture, styled through Style.toTextStyle.
let private styledOn (fixture: IContainer -> TextBlockDescriptor) (style: Style) =
    raw (fun c ->
        (fixture c).Style (Style.toTextStyle style)
        |> ignore)

/// An equivalence test on a fixture where the style changes the PDF.
let private checkOn name fixture (style: Style) (expected: TextBlockDescriptor -> TextBlockDescriptor) =
    equivalent name (styledOn fixture style) (fun c -> fixture c |> expected |> ignore)

/// A negative control: the style changes the PDF of the fixture.
let private changesOn name fixture (style: Style) =
    distinct name (styledOn fixture style) (fun c -> fixture c |> ignore)

/// Two words, for word spacing.
let private twoWords (c: IContainer) =
    c.Text "two words"

/// Ligatures and kerning pairs.
let private shaped (c: IContainer) =
    c.Text "office fifty AVATAR Toyota"

/// A line too narrow for both words: breaking anywhere moves letters of the second word onto the first line.
let private narrow (c: IContainer) =
    c.Width(60f).Text "ab abcdefghij"

/// A test that a style gives the same QuestPDF text style as a fluent chain on the default style. The direction is
/// recorded in the text style and leaves the PDF of Latin text in the test fonts unchanged.
let private sameTextStyle name (style: Style) (expected: TextStyle -> TextStyle) =
    test name { Expect.equal (Style.toTextStyle style) (expected TextStyle.Default) "the QuestPDF text styles are equal" }

let private weights: (string * Style * (TextBlockDescriptor -> TextBlockDescriptor)) list =
    [ "thin", Style.thin, (fun t -> t.Thin ())
      "extraLight", Style.extraLight, (fun t -> t.ExtraLight ())
      "light", Style.light, (fun t -> t.Light ())
      "normalWeight", Style.bold >> Style.normalWeight, (fun t -> t.Bold().NormalWeight ())
      "medium", Style.medium, (fun t -> t.Medium ())
      "extraBold", Style.extraBold, (fun t -> t.ExtraBold ())
      "black", Style.black, (fun t -> t.Black ())
      "extraBlack", Style.extraBlack, (fun t -> t.ExtraBlack ()) ]

[<Tests>]
let tests =
    testList
        "Style"
        [ check "none" Style.none id
          check "size int" (Style.size 20) (fun t -> t.FontSize 20f)
          check "size float" (Style.size 12.5) (fun t -> t.FontSize 12.5f)
          check "color" (Style.color Colors.Red.Medium) (fun t -> t.FontColor Colors.Red.Medium)
          check "bold" Style.bold (fun t -> t.Bold ())
          check "semiBold" Style.semiBold (fun t -> t.SemiBold ())
          check "italic" Style.italic (fun t -> t.Italic ())
          check "underline" Style.underline (fun t -> t.Underline ())
          check "family" (Style.family "Lato") (fun t -> t.FontFamily "Lato")
          test "family applies the family: an unregistered family raises" {
              configure ()

              Expect.throwsT<DocumentDrawingException>
                  (fun () ->
                      wrapContent (styled (Style.family "Unregistered Family"))
                      |> Pdf.bytes
                      |> ignore)
                  "the default family would render"
          }
          check "lineHeight" (Style.lineHeight 1.5) (fun t -> t.LineHeight (Nullable 1.5f))
          check
              "composition applies left to right"
              (Style.size 14
               >> Style.bold
               >> Style.color Colors.Blue.Darken2)
              (fun t -> t.FontSize(14f).Bold().FontColor (Colors.Blue.Darken2))
          check "a later style overrides an earlier one" (Style.size 10 >> Style.size 20) (fun t -> t.FontSize 20f)
          distinct "the earlier of two sizes is overridden" (styled (Style.size 10 >> Style.size 20)) (fun c ->
              c.Text("styled").FontSize (10f) |> ignore)
          check "fluent is the chained call" (Style.fluent (fun s -> s.Underline ())) (fun t -> t.Underline ())
          equivalent
              "apply runs the style on a QuestPDF text style"
              (raw (fun c ->
                  c.Text("styled").Style (Style.apply (Style.size 15) TextStyle.Default)
                  |> ignore))
              (fun c -> c.Text("styled").FontSize (15f) |> ignore)
          testList
              "unannotated top-level compositions"
              [ check "color then size" StyleProbe.muted (fun t -> t.FontColor(Colors.Grey.Darken1).FontSize (9f))
                check "bold then italic" StyleProbe.boldItalic (fun t -> t.Bold().Italic ())
                check "size then bold" StyleProbe.sizedBold (fun t -> t.FontSize(12f).Bold ())
                check "bold then size" StyleProbe.boldSized (fun t -> t.Bold().FontSize (12f))
                check "lineHeight then size" StyleProbe.spaced (fun t -> t.LineHeight(Nullable 1.5f).FontSize (11f)) ]
          distinct "bold is not italic" (styled Style.bold) (fun c -> c.Text("styled").Italic () |> ignore)
          check "families" (Style.families [ "Lato" ]) (fun t -> t.FontFamily ([| "Lato" |]))
          test "families applies every family: an unregistered fallback raises" {
              configure ()

              Expect.throwsT<DocumentDrawingException>
                  (fun () ->
                      wrapContent (styled (Style.families [ "Lato"; "Unregistered Family" ]))
                      |> Pdf.bytes
                      |> ignore)
                  "only the first family would render"
          }
          check "background" (Style.background Colors.Yellow.Lighten2) (fun t -> t.BackgroundColor Colors.Yellow.Lighten2)
          testList "weights" [ for name, style, expected in weights -> check name style expected ]
          check "weight" (Style.weight FontWeight.Bold) (fun t -> t.Bold ())
          distinct "weight changes the output" (styled (Style.weight FontWeight.Bold)) (fun c -> c.Text ("styled") |> ignore)
          check "strikethrough" Style.strikethrough (fun t -> t.Strikethrough ())
          check "overline" Style.overline (fun t -> t.Overline ())
          check
              "decorationColor"
              (Style.underline
               >> Style.decorationColor Colors.Red.Medium)
              (fun t -> t.Underline().DecorationColor (Colors.Red.Medium))
          check "decorationThickness int" (Style.underline >> Style.decorationThickness 3) (fun t -> t.Underline().DecorationThickness (3f))
          check "decorationThickness float" (Style.underline >> Style.decorationThickness 1.5) (fun t -> t.Underline().DecorationThickness (1.5f))
          checkOn
              "decorationSolid"
              twoWords
              (Style.underline
               >> Style.decorationWavy
               >> Style.decorationSolid)
              (fun t -> t.Underline().DecorationWavy().DecorationSolid ())
          distinct
              "decorationSolid replaces the wavy underline"
              (styledOn
                  twoWords
                  (Style.underline
                   >> Style.decorationWavy
                   >> Style.decorationSolid))
              (fun c ->
                  (twoWords c).Underline().DecorationWavy ()
                  |> ignore)
          check "decorationDouble" (Style.underline >> Style.decorationDouble) (fun t -> t.Underline().DecorationDouble ())
          check "decorationWavy" (Style.underline >> Style.decorationWavy) (fun t -> t.Underline().DecorationWavy ())
          check "decorationDotted" (Style.underline >> Style.decorationDotted) (fun t -> t.Underline().DecorationDotted ())
          check "decorationDashed" (Style.underline >> Style.decorationDashed) (fun t -> t.Underline().DecorationDashed ())
          distinct "decorationWavy changes the underline" (styled (Style.underline >> Style.decorationWavy)) (fun c ->
              c.Text("styled").Underline () |> ignore)
          check "letterSpacing int" (Style.letterSpacing 1) (fun t -> t.LetterSpacing (1f))
          check "letterSpacing float" (Style.letterSpacing 0.25) (fun t -> t.LetterSpacing (0.25f))
          checkOn "wordSpacing int" twoWords (Style.wordSpacing 2) (fun t -> t.WordSpacing (2f))
          checkOn "wordSpacing float" twoWords (Style.wordSpacing 0.5) (fun t -> t.WordSpacing (0.5f))
          changesOn "wordSpacing changes the output" twoWords (Style.wordSpacing 2)
          distinct "letterSpacing changes the output" (styled (Style.letterSpacing 1)) (fun c -> c.Text ("styled") |> ignore)
          check "subscript" Style.subscript (fun t -> t.Subscript ())
          check "superscript" Style.superscript (fun t -> t.Superscript ())
          check "normalPosition" (Style.superscript >> Style.normalPosition) (fun t -> t.Superscript().NormalPosition ())
          distinct "subscript is not superscript" (styled Style.subscript) (fun c -> c.Text("styled").Superscript () |> ignore)
          checkOn "enableFeature" shaped (Style.enableFeature FontFeatures.StandardLigatures) (fun t ->
              t.EnableFontFeature FontFeatures.StandardLigatures)
          checkOn "disableFeature" shaped (Style.disableFeature FontFeatures.Kerning) (fun t -> t.DisableFontFeature FontFeatures.Kerning)
          changesOn "enableFeature changes the output" shaped (Style.enableFeature FontFeatures.StandardLigatures)
          changesOn "disableFeature changes the output" shaped (Style.disableFeature FontFeatures.Kerning)
          check "directionAuto" Style.directionAuto (fun t -> t.DirectionAuto ())
          check "leftToRight" Style.leftToRight (fun t -> t.DirectionFromLeftToRight ())
          check "rightToLeft" Style.rightToLeft (fun t -> t.DirectionFromRightToLeft ())
          sameTextStyle "directionAuto sets the direction" Style.directionAuto (fun s -> s.DirectionAuto ())
          sameTextStyle "leftToRight sets the direction" Style.leftToRight (fun s -> s.DirectionFromLeftToRight ())
          sameTextStyle "rightToLeft sets the direction" Style.rightToLeft (fun s -> s.DirectionFromRightToLeft ())
          checkOn "breakAnywhere" narrow Style.breakAnywhere (fun t -> t.BreakAnywhere ())
          changesOn "breakAnywhere changes the output" narrow Style.breakAnywhere
          check "ofTextStyle" (Style.ofTextStyle (TextStyle.Default.FontSize 20f)) (fun t -> t.FontSize 20f)
          check
              "ofTextStyle replaces the style"
              (Style.bold
               >> Style.ofTextStyle (TextStyle.Default.FontSize 20f))
              (fun t -> t.FontSize 20f)
          distinct
              "ofTextStyle discards the earlier style"
              (styled (
                  Style.bold
                  >> Style.ofTextStyle (TextStyle.Default.FontSize 20f)
              ))
              (fun c -> c.Text("styled").Bold().FontSize (20f) |> ignore) ]
