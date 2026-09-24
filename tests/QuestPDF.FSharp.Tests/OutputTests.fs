module QuestPDF.FSharp.Tests.OutputTests

open Expecto
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open QuestPDF.FSharp
open QuestPDF.FSharp.Tests.Support

/// A row of two labelled boxes, so the content direction changes the layout.
let private body: Content =
    row
        [ Row.fill (background Colors.Grey.Lighten3 >> text "first")
          Row.fill (text "second") ]

let private rawBody (c: IContainer) =
    c.Row (fun r ->
        r.RelativeItem().Background(Colors.Grey.Lighten3).Text ("first")
        |> ignore

        r.RelativeItem().Text ("second") |> ignore)

type private Marker = class end

/// A 64 x 64 px opaque PNG of random pixels, embedded in the test assembly. Its encoding in the PDF follows the
/// image quality and resolution settings.
let private noise =
    use stream = typeof<Marker>.Assembly.GetManifestResourceStream "noise.png"
    use copy = new System.IO.MemoryStream ()
    stream.CopyTo copy
    copy.ToArray ()

let private image: Content = width 20 >> Image.bytes noise

let private rawImage (c: IContainer) =
    c.Width(20f).Image (noise) |> ignore

/// A wrapper document with pinned dates, the settings items under test and one A5 page: the extra page parts, then
/// the content.
let private wrapDoc (settings: DocumentPart list) (pageParts: PagePart list) (content: Content) : IDocument =
    document (
        [ Meta.dated fixedDate ]
        @ settings
        @ [ page (
                [ Page.size PageSizes.A5; Page.margin 20 ]
                @ pageParts
                @ [ Page.content content ]
            ) ]
    )

/// A wrapper document with pinned dates, the settings items under test and one A5 page of body.
let private wrapWith (settings: DocumentPart list) : IDocument =
    wrapDoc settings [] body

/// A raw document with pinned metadata, the settings and one page built by the function.
let private rawDoc (settings: DocumentSettings) (build: PageDescriptor -> unit) : IDocument =
    Document.Create(fun container -> container.Page (fun p -> build p) |> ignore).WithMetadata(pinnedMetadata ()).WithSettings (settings)

/// An A5 page with a 20 pt margin and the content.
let private a5 (content: IContainer -> unit) (p: PageDescriptor) =
    p.Size PageSizes.A5
    p.Margin 20f
    content (p.Content ())

/// The raw counterpart of wrapWith.
let private rawWith (settings: DocumentSettings) : IDocument =
    rawDoc settings (a5 rawBody)

/// A wrapper document of the given number of pages, each a single page definition.
let private pages (count: int) : IDocument =
    document
        [ Meta.dated fixedDate
          for i in 1..count do
              page [ Page.size PageSizes.A6; Page.content (text $"page {i}") ] ]

let private settingsOf (parts: DocumentPart list) : DocumentSettings =
    (wrapWith parts).GetSettings ()

let private startsWith (prefix: byte[]) (bytes: byte[]) =
    bytes.Length >= prefix.Length
    && bytes[.. prefix.Length - 1] = prefix

let private png =
    [| 0x89uy; 0x50uy; 0x4Euy; 0x47uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy |]

let private jpeg = [| 0xFFuy; 0xD8uy; 0xFFuy |]

let settings =
    testList
        "settings"
        [ test "a document without Output items keeps the QuestPDF defaults" {
              let actual = settingsOf []
              let expected = DocumentSettings ()
              Expect.equal actual.PDFA_Conformance expected.PDFA_Conformance "PDF/A"
              Expect.equal actual.PDFUA_Conformance expected.PDFUA_Conformance "PDF/UA"
              Expect.equal actual.CompressDocument expected.CompressDocument "compression"
              Expect.equal actual.ImageCompressionQuality expected.ImageCompressionQuality "image quality"
              Expect.equal actual.ImageRasterDpi expected.ImageRasterDpi "raster dpi"
              Expect.equal actual.ContentDirection expected.ContentDirection "direction"
          }
          test "pdfA sets the PDF/A conformance" {
              Expect.equal (settingsOf [ Output.pdfA PDFA_Conformance.PDFA_3B ]).PDFA_Conformance PDFA_Conformance.PDFA_3B "PDF/A-3B"
              Expect.equal (settingsOf [ Output.pdfA PDFA_Conformance.PDFA_2B ]).PDFA_Conformance PDFA_Conformance.PDFA_2B "PDF/A-2B"
          }
          test "pdfUA sets PDF/UA-1" { Expect.equal (settingsOf [ Output.pdfUA ]).PDFUA_Conformance PDFUA_Conformance.PDFUA_1 "PDF/UA-1" }
          test "compress sets document compression" {
              Expect.isFalse (settingsOf [ Output.compress false ]).CompressDocument "off"
              Expect.isTrue (settingsOf [ Output.compress false; Output.compress true ]).CompressDocument "the last item wins"
          }
          test "imageQuality sets the image compression quality" {
              Expect.equal (settingsOf [ Output.imageQuality ImageCompressionQuality.Low ]).ImageCompressionQuality ImageCompressionQuality.Low "low"

              Expect.equal
                  (settingsOf [ Output.imageQuality ImageCompressionQuality.Medium ]).ImageCompressionQuality
                  ImageCompressionQuality.Medium
                  "medium"
          }
          test "imageDpi sets the image raster resolution" {
              Expect.equal (settingsOf [ Output.imageDpi 144 ]).ImageRasterDpi 144 "144 dpi"
              Expect.equal (settingsOf [ Output.imageDpi 36 ]).ImageRasterDpi 36 "36 dpi"
          }
          test "rightToLeft sets the content direction" {
              Expect.equal (settingsOf [ Output.rightToLeft ]).ContentDirection ContentDirection.RightToLeft "right to left"
          }
          test "several items combine" {
              let actual = settingsOf [ Output.compress false; Output.imageDpi 96; Output.pdfUA ]

              Expect.equal
                  (actual.CompressDocument, actual.ImageRasterDpi, actual.PDFUA_Conformance)
                  (false, 96, PDFUA_Conformance.PDFUA_1)
                  "all three"
          } ]

let equivalence =
    testList
        "equivalence"
        [ equivalentDoc "compress false" (wrapWith [ Output.compress false ]) (rawWith (DocumentSettings (CompressDocument = false)))
          equivalentDoc
              "rightToLeft"
              (wrapWith [ Output.rightToLeft ])
              (rawDoc (DocumentSettings (ContentDirection = ContentDirection.RightToLeft)) (fun p ->
                  p.ContentFromRightToLeft ()
                  a5 rawBody p))
          test "rightToLeft lays out every page from right to left" {
              configure ()
              Expect.isFalse (Pdf.bytes (wrapWith [ Output.rightToLeft ]) = Pdf.bytes (wrapWith [])) "the layout is mirrored"
          }
          equivalentDoc
              "Page.leftToRight under rightToLeft"
              (wrapDoc [ Output.rightToLeft ] [ Page.leftToRight ] body)
              (rawDoc (DocumentSettings (ContentDirection = ContentDirection.RightToLeft)) (fun p ->
                  p.ContentFromRightToLeft ()
                  p.Size PageSizes.A5
                  p.Margin 20f
                  p.ContentFromLeftToRight ()
                  rawBody (p.Content ())))
          test "Page.leftToRight overrides rightToLeft for its page" {
              configure ()

              Expect.isTrue
                  (Pdf.bytes (wrapDoc [ Output.rightToLeft ] [ Page.leftToRight ] body) = Pdf.bytes (wrapWith []))
                  "the page is left to right"
          }
          for quality in [ ImageCompressionQuality.Low; ImageCompressionQuality.Medium ] do
              equivalentDoc
                  $"imageQuality {quality}"
                  (wrapDoc [ Output.imageQuality quality ] [] image)
                  (rawDoc (DocumentSettings (ImageCompressionQuality = quality)) (a5 rawImage))
          for dpi in [ 36; 72 ] do
              equivalentDoc
                  $"imageDpi {dpi}"
                  (wrapDoc [ Output.imageDpi dpi ] [] image)
                  (rawDoc (DocumentSettings (ImageRasterDpi = dpi)) (a5 rawImage))
          test "imageQuality and imageDpi change an embedded image" {
              configure ()
              let plain = Pdf.bytes (wrapDoc [] [] image)
              Expect.isFalse (Pdf.bytes (wrapDoc [ Output.imageQuality ImageCompressionQuality.Low ] [] image) = plain) "quality"
              Expect.isFalse (Pdf.bytes (wrapDoc [ Output.imageDpi 36 ] [] image) = plain) "dpi"
          }
          test "compress false changes the output" {
              configure ()
              Expect.isFalse (Pdf.bytes (wrapWith [ Output.compress false ]) = Pdf.bytes (wrapWith [])) "compression differs"
          }
          for level in [ PDFA_Conformance.PDFA_3B; PDFA_Conformance.PDFA_2B ] do
              test $"PDF/A {level} is reproducible and equal to raw after normalizing ids" {
                  configure ()

                  let wrapped () =
                      Pdf.bytes (wrapWith [ Output.pdfA level ])

                  let a, b = wrapped (), wrapped ()
                  Expect.isFalse (a = b) "PDF/A output carries a random id"
                  Expect.isTrue (normalizeIds a = normalizeIds b) "equal after normalizing"

                  let raw = (rawWith (DocumentSettings (PDFA_Conformance = level))).GeneratePdf ()

                  Expect.isTrue (normalizeIds a = normalizeIds raw) "equal to raw after normalizing"
              }
          test "PDF/A levels generate different files" {
              configure ()

              let level conformance =
                  normalizeIds (Pdf.bytes (wrapWith [ Output.pdfA conformance ]))

              Expect.isFalse (level PDFA_Conformance.PDFA_2B = level PDFA_Conformance.PDFA_3B) "2B differs from 3B"
          }
          test "PDF/UA is reproducible and equal to raw after normalizing ids" {
              configure ()

              let wrapped () =
                  Pdf.bytes (wrapWith [ Output.pdfUA ])

              let a, b = wrapped (), wrapped ()
              Expect.isFalse (a = b) "PDF/UA output carries a random id"
              Expect.isTrue (normalizeIds a = normalizeIds b) "equal after normalizing"

              let raw =
                  (rawWith (DocumentSettings (PDFUA_Conformance = PDFUA_Conformance.PDFUA_1))).GeneratePdf ()

              Expect.isTrue (normalizeIds a = normalizeIds raw) "equal to raw after normalizing"
          } ]

let targets =
    testList
        "targets"
        [ test "images Png 72 gives one PNG per page" {
              configure ()
              let document = pages 3
              let images = Pdf.images ImageFormat.Png 72 document
              Expect.equal images.Length (pageCount (Pdf.bytes document)) "one image per page"
              Expect.all images (startsWith png) "PNG signature"
          }
          test "images leaves the document settings and PDF unchanged" {
              configure ()
              let document = wrapDoc [ Output.imageDpi 100 ] [] image
              let before = Pdf.bytes document
              Pdf.images ImageFormat.Png 72 document |> ignore
              Expect.equal (document.GetSettings ()).ImageRasterDpi 100 "the raster dpi of the document"
              Expect.isTrue (Pdf.bytes document = before) "the same PDF after images"
          }
          test "images Jpeg gives JPEG files" {
              configure ()
              Expect.all (Pdf.images ImageFormat.Jpeg 72 (pages 2)) (startsWith jpeg) "JPEG signature"
          }
          test "images equals GenerateImages with the same format and dpi" {
              configure ()
              let document = pages 2

              let raw =
                  document.GenerateImages (ImageGenerationSettings (ImageFormat = ImageFormat.Png, RasterDpi = 72))
                  |> List.ofSeq

              Expect.isTrue (Pdf.images ImageFormat.Png 72 document = raw) "same images"
          }
          test "images at a higher dpi are larger" {
              configure ()
              let small = Pdf.images ImageFormat.Png 72 (pages 1)
              let large = Pdf.images ImageFormat.Png 144 (pages 1)
              Expect.isGreaterThan large[0].Length small[0].Length "more pixels"
          }
          test "svgs gives one SVG document per page" {
              configure ()
              let svgs = Pdf.svgs (pages 3)
              Expect.equal svgs.Length 3 "one per page"
              Expect.all svgs (fun svg -> svg.Contains "<svg") "SVG markup"
          }
          test "svgs equals GenerateSvg" {
              configure ()
              let document = pages 2
              Expect.equal (Pdf.svgs document) (List.ofSeq (document.GenerateSvg ())) "same markup"
          } ]

[<Tests>]
let tests = testList "Output" [ settings; equivalence; targets ]
