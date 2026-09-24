module QuestPDF.FSharp.Tests.ElementsTests

open System.IO
open Expecto
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open QuestPDF.FSharp
open QuestPDF.FSharp.Tests.Support

type private Marker = class end

let private resource (name: string) =
    use stream = typeof<Marker>.Assembly.GetManifestResourceStream name
    use copy = new MemoryStream ()
    stream.CopyTo copy
    copy.ToArray ()

/// A 24 x 16 px PNG, embedded in the test assembly.
let private png = resource "swatch.png"

/// A 64 x 64 px opaque PNG of random pixels, embedded in the test assembly. Drawn 20 pt wide, its encoding changes
/// with every non-default encoding option.
let private noise = resource "noise.png"

let private svg =
    """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="10" viewBox="0 0 20 10"><rect width="20" height="10" fill="#3366cc"/><circle cx="5" cy="5" r="4" fill="#ffcc00"/></svg>"""

/// An image option, the box it is drawn in, and the raw calls for both.
let private imageForms: (string * ImageOption * Modifier * (IContainer -> IContainer) * (ImageDescriptor -> ImageDescriptor)) list =
    [ "fitWidth", Image.fitWidth, width 100, (fun c -> c.Width (100f)), (fun d -> d.FitWidth ())
      "fitHeight", Image.fitHeight, height 40, (fun c -> c.Height (40f)), (fun d -> d.FitHeight ())
      "fitArea", Image.fitArea, width 100 >> height 40, (fun c -> c.Width(100f).Height (40f)), (fun d -> d.FitArea ())
      "fitUnproportionally",
      Image.fitUnproportionally,
      width 100 >> height 40,
      (fun c -> c.Width(100f).Height (40f)),
      (fun d -> d.FitUnproportionally ()) ]

/// The encoding options, drawn on the noise image in a 20 pt box, with the raw call each maps to.
let private encodingForms: (string * ImageOption * (ImageDescriptor -> ImageDescriptor)) list =
    [ "original", Image.original, (fun d -> d.UseOriginalImage ())
      "dpi", Image.dpi 36, (fun d -> d.WithRasterDpi (36))
      "quality", Image.quality ImageCompressionQuality.Low, (fun d -> d.WithCompressionQuality (ImageCompressionQuality.Low))
      "options compose", Image.fitWidth >> Image.dpi 36, (fun d -> d.FitWidth().WithRasterDpi (36)) ]

[<Tests>]
let tests =
    testList
        "Elements"
        [ testList
              "lines"
              [ equivalent "lineH int" (lineH 2 Colors.Red.Medium) (fun c ->
                    c.LineHorizontal(2f).LineColor (Colors.Red.Medium)
                    |> ignore)
                equivalent "lineH mm" (lineH (1 * mm) Colors.Red.Medium) (fun c ->
                    c.LineHorizontal(1f, Unit.Millimetre).LineColor (Colors.Red.Medium)
                    |> ignore)
                equivalent "lineV float" (height 50 >> lineV 1.5 Colors.Blue.Medium) (fun c ->
                    c.Height(50f).LineVertical(1.5f).LineColor (Colors.Blue.Medium)
                    |> ignore)
                equivalent "lineHWith dash pattern" (lineHWith 1 (fun l -> l.LineColor(Colors.Green.Medium).LineDashPattern ([| 4f; 2f |]))) (fun c ->
                    c.LineHorizontal(1f).LineColor(Colors.Green.Medium).LineDashPattern ([| 4f; 2f |])
                    |> ignore)
                equivalent
                    "lineVWith mm"
                    (height 50
                     >> lineVWith (0.5 * mm) (fun l -> l.LineColor Colors.Green.Medium))
                    (fun c ->
                        c.Height(50f).LineVertical(0.5f, Unit.Millimetre).LineColor (Colors.Green.Medium)
                        |> ignore)
                distinct "line colour changes the output" (lineH 2 Colors.Red.Medium) (fun c ->
                    c.LineHorizontal(2f).LineColor (Colors.Blue.Medium)
                    |> ignore) ]
          testList
              "images"
              [ equivalent "Image.bytes" (width 100 >> Image.bytes png) (fun c -> c.Width(100f).Image (png) |> ignore)
                test "Image.file" {
                    let path = Path.GetTempFileName ()

                    try
                        File.WriteAllBytes (path, png)
                        samePdf "Image.file" (wrapContent (width 100 >> Image.file path)) (rawContent (fun c -> c.Width(100f).Image (path) |> ignore))
                    finally
                        File.Delete path
                }
                test "Image.fileWith" {
                    let path = Path.GetTempFileName ()

                    try
                        File.WriteAllBytes (path, png)

                        samePdf
                            "Image.fileWith"
                            (wrapContent (
                                width 100
                                >> height 40
                                >> Image.fileWith Image.fitArea path
                            ))
                            (rawContent (fun c ->
                                c.Width(100f).Height(40f).Image(path).FitArea ()
                                |> ignore))
                    finally
                        File.Delete path
                }
                test "Image.shared" {
                    use image = QuestPDF.Infrastructure.Image.FromBinaryData png

                    samePdf
                        "Image.shared"
                        (wrapContent (
                            column
                                [ width 100 >> Image.shared image Image.fitWidth
                                  width 50 >> Image.shared image id ]
                        ))
                        (rawContent (fun c ->
                            c.Column (fun col ->
                                col.Item().Width(100f).Image(image).FitWidth ()
                                |> ignore

                                col.Item().Width(50f).Image (image) |> ignore)))
                }
                for name, option, box, rawBox, rawOption in imageForms do
                    equivalent $"Image.bytesWith {name}" (box >> Image.bytesWith option png) (fun c -> rawOption ((rawBox c).Image png) |> ignore)
                for name, option, rawOption in encodingForms do
                    equivalent $"Image.bytesWith {name}" (width 20 >> Image.bytesWith option noise) (fun c ->
                        rawOption (c.Width(20f).Image noise) |> ignore)

                    distinct $"{name} changes the output" (width 20 >> Image.bytesWith option noise) (fun c -> c.Width(20f).Image (noise) |> ignore)
                distinct
                    "fit options change the output"
                    (width 100
                     >> height 40
                     >> Image.bytesWith Image.fitUnproportionally png)
                    (fun c ->
                        c.Width(100f).Height(40f).Image(png).FitArea ()
                        |> ignore) ]
          testList
              "svg"
              [ equivalent "Svg.text" (width 100 >> Svg.text svg) (fun c -> c.Width(100f).Svg (svg) |> ignore)
                for name, option, box, rawBox, rawOption in
                    [ "fitWidth", Svg.fitWidth, width 100, (fun (c: IContainer) -> c.Width (100f)), (fun (d: SvgImageDescriptor) -> d.FitWidth ())
                      "fitHeight", Svg.fitHeight, height 40, (fun c -> c.Height (40f)), (fun d -> d.FitHeight ())
                      "fitArea", Svg.fitArea, width 100 >> height 40, (fun c -> c.Width(100f).Height (40f)), (fun d -> d.FitArea ()) ] do
                    equivalent $"Svg.textWith {name}" (box >> Svg.textWith option svg) (fun c -> rawOption ((rawBox c).Svg svg) |> ignore)
                distinct "the svg is drawn" (width 100 >> Svg.text svg) (fun c -> c.Width (100f) |> ignore) ]
          testList
              "page break and placeholder"
              [ equivalent "pageBreak" (column [ text "one"; pageBreak; text "two" ]) (fun c ->
                    c.Column (fun col ->
                        col.Item().Text ("one") |> ignore
                        col.Item().PageBreak ()
                        col.Item().Text ("two") |> ignore))
                test "pageBreak starts a new page" {
                    configure ()

                    let pdf =
                        Pdf.bytes (wrapContent (column [ text "one"; pageBreak; text "two"; pageBreak; text "three" ]))

                    Expect.equal (pageTexts pdf) [ "one"; "two"; "three" ] "one word per page"
                }
                equivalent "placeholder" (height 60 >> placeholder "logo") (fun c -> c.Height(60f).Placeholder ("logo"))
                distinct "placeholder text is drawn" (height 60 >> placeholder "logo") (fun c -> c.Height(60f).Placeholder ("other")) ] ]
