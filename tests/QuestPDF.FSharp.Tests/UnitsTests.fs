module QuestPDF.FSharp.Tests.UnitsTests

open System.Reflection
open Expecto
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open QuestPDF.FSharp

/// QuestPDF's internal unit conversion, the reference for Length.points.
let private questPdfPoints =
    let toPoints =
        typeof<IContainer>.Assembly
            .GetType("QuestPDF.Infrastructure.UnitExtensions")
            .GetMethod (
                "ToPoints",
                BindingFlags.Static
                ||| BindingFlags.Public
                ||| BindingFlags.NonPublic
            )

    fun (value: float32) (unit: Unit) -> toPoints.Invoke (null, [| box value; box unit |]) :?> float32

let private allUnits =
    [ Unit.Point
      Unit.Meter
      Unit.Centimetre
      Unit.Millimetre
      Unit.Feet
      Unit.Inch
      Unit.Mil ]

let lengths =
    testList
        "lengths"
        [ test "int times cm" { Expect.equal (2 * cm) { Value = 2f; Unit = Unit.Centimetre } "2 * cm" }
          test "float times mm" { Expect.equal (2.5 * mm) { Value = 2.5f; Unit = Unit.Millimetre } "2.5 * mm" }
          test "float32 times pt" { Expect.equal (2.5f * pt) { Value = 2.5f; Unit = Unit.Point } "2.5f * pt" }
          test "inch, mil and feet" {
              Expect.equal (1 * inch) { Value = 1f; Unit = Unit.Inch } "inch"
              Expect.equal (3 * mil) { Value = 3f; Unit = Unit.Mil } "mil"
              Expect.equal (2 * feet) { Value = 2f; Unit = Unit.Feet } "feet"
          }
          test "len of int is points" { Expect.equal (len 10) { Value = 10f; Unit = Unit.Point } "len 10" }
          test "len of float is points" { Expect.equal (len 2.5) { Value = 2.5f; Unit = Unit.Point } "len 2.5" }
          test "len of float32 is points" { Expect.equal (len 2.5f) { Value = 2.5f; Unit = Unit.Point } "len 2.5f" }
          test "len of int64 is points" { Expect.equal (len 10L) { Value = 10f; Unit = Unit.Point } "len 10L" }
          test "len of decimal is points" { Expect.equal (len 2.5m) { Value = 2.5f; Unit = Unit.Point } "len 2.5m" }
          test "int64 and decimal times a unit" {
              Expect.equal (3L * mm) { Value = 3f; Unit = Unit.Millimetre } "3L * mm"
              Expect.equal (1.5m * cm) { Value = 1.5f; Unit = Unit.Centimetre } "1.5m * cm"
          }
          test "len of Length is unchanged" { Expect.equal (len (5 * mm)) (5 * mm) "len (5 * mm)" }
          test "points of 2 cm and 20 mm match QuestPDF" {
              Expect.equal (Length.points (2 * cm)) 56.692913f "2 cm"
              Expect.equal (Length.points (20 * mm)) 56.692917f "20 mm"
              Expect.notEqual (Length.points (2 * cm)) (Length.points (20 * mm)) "float32 conversion keeps them apart"
          }
          test "points match QuestPDF for every unit" {
              for unit in allUnits do
                  for value in [ 0f; 0.5f; 1f; 2f; 2.5f; 20f; 123.456f ] do
                      Expect.equal (Length.points { Value = value; Unit = unit }) (questPdfPoints value unit) $"{value} {unit}"
          } ]

let colors =
    testList
        "colors"
        [ test "hex" { Expect.equal (Color.hex "#80FF0000").Hex (Color.FromHex "#80FF0000").Hex "hex" }
          test "rgb" { Expect.equal (Color.rgb 1uy 2uy 3uy).Hex (Color.FromRGB (1uy, 2uy, 3uy)).Hex "rgb" }
          test "argb" { Expect.equal (Color.argb 4uy 1uy 2uy 3uy).Hex (Color.FromARGB (4uy, 1uy, 2uy, 3uy)).Hex "argb" }
          test "withAlpha" { Expect.equal (Color.withAlpha 0.5 Colors.Red.Medium).Hex (Colors.Red.Medium.WithAlpha (0.5f)).Hex "withAlpha" }
          test "Colors alias resolves the Material palette" {
              let blue: Color = Colors.Blue.Medium
              Expect.equal (string blue) "#2196F3" "Colors.Blue.Medium"
          } ]

let pageSizes =
    let dims (size: PageSize) =
        size.Width, size.Height

    testList
        "page sizes"
        [ test "landscape" { Expect.equal (dims (PageSize.landscape PageSizes.A4)) (dims (PageSizes.A4.Landscape ())) "landscape" }
          test "portrait" {
              let wide = PageSizes.A4.Landscape ()
              Expect.equal (dims (PageSize.portrait wide)) (dims (wide.Portrait ())) "portrait"
          }
          test "custom with one unit keeps the unit" {
              Expect.equal (dims (PageSize.custom (10 * cm) (15 * cm))) (dims (PageSize (10f, 15f, Unit.Centimetre))) "cm"
          }
          test "custom takes bare numbers as points" {
              Expect.equal (dims (PageSize.custom 300 400.5)) (dims (PageSize (300f, 400.5f, Unit.Point))) "points"
          }
          test "custom with a bare number and a length converts to points" {
              let expected = PageSize (300f, Length.points (10 * cm))
              Expect.equal (dims (PageSize.custom 300 (10 * cm))) (dims expected) "mixed"
          }
          test "custom with mixed units converts to points" {
              let expected = PageSize (Length.points (100 * mm), Length.points (4 * inch))
              Expect.equal (dims (PageSize.custom (100 * mm) (4 * inch))) (dims expected) "mixed"
          } ]

[<Tests>]
let tests = testList "Units" [ lengths; colors; pageSizes ]
