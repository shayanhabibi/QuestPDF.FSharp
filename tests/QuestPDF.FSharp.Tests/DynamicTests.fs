module QuestPDF.FSharp.Tests.DynamicTests

open System
open System.IO
open Expecto
open QuestPDF.Elements
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open QuestPDF.FSharp
open QuestPDF.FSharp.Tests.Support

type private Marker = class end

/// A 64 x 64 px opaque PNG of random pixels, embedded in the test assembly.
let private noise =
    use stream = typeof<Marker>.Assembly.GetManifestResourceStream "noise.png"
    use copy = new MemoryStream ()
    stream.CopyTo copy
    copy.ToArray ()

/// A dynamic component that labels three pages with their numbers, building each label with the given element maker.
type private PageLabels(create: DynamicContext -> string -> IDynamicElement) =
    interface IDynamicComponent with
        member _.Compose context =
            DynamicComponentComposeResult (
                Content = create context $"page {context.PageNumber}",
                HasMoreContent = (context.PageNumber < 3)
            )

let private wrapperLabel (context: DynamicContext) (label: string) =
    Dynamic.createElement context (padding 4 >> background Colors.Grey.Lighten3 >> text label)

let private rawLabel (context: DynamicContext) (label: string) =
    context.CreateElement (fun c -> c.Padding(4f).Background(Colors.Grey.Lighten3).Text (label) |> ignore)

/// A dynamic component that counts items in its state, one per page, and draws the element made by Dynamic.element or
/// by the raw Element call inside a bordered box.
type private Counter(place: IDynamicElement -> IContainer -> unit) =
    let mutable state = 1

    interface IDynamicComponent<int> with
        member _.State
            with get () = state
            and set value = state <- value

    interface IDynamicComponent with
        member _.Compose context =
            let label =
                context.CreateElement (fun c -> c.Text ($"item {state}") |> ignore)

            let boxed =
                context.CreateElement (fun c -> place label (c.Border(1f).Padding (2f)))

            state <- state + 1

            DynamicComponentComposeResult (Content = boxed, HasMoreContent = (state <= 3))

/// A dynamic component that writes the number of captured positions of "box" and the page and width of the first.
type private CaptureReader(create: DynamicContext -> string -> IDynamicElement) =
    interface IDynamicComponent with
        member _.Compose context =
            let positions = context.GetContentCapturedPositions "box" |> Seq.toList

            let summary =
                match positions with
                | first :: _ -> $"{positions.Length} at page {first.PageNumber}, width {first.Width}"
                | [] -> "none"

            DynamicComponentComposeResult (Content = create context summary, HasMoreContent = false)

/// The dynamic image options, drawn on the noise image in a 20 pt box, with the raw call each maps to.
let private dynamicImageForms: (string * DynamicImageOption * (DynamicImageDescriptor -> DynamicImageDescriptor)) list =
    [ "original", DynamicImage.original, (fun d -> d.UseOriginalImage ())
      "dpi", DynamicImage.dpi 36, (fun d -> d.WithRasterDpi (36))
      "quality", DynamicImage.quality ImageCompressionQuality.Low, (fun d -> d.WithCompressionQuality (ImageCompressionQuality.Low))
      "options compose",
      DynamicImage.dpi 36 >> DynamicImage.quality ImageCompressionQuality.Low,
      (fun d -> d.WithRasterDpi(36).WithCompressionQuality (ImageCompressionQuality.Low)) ]

let private noiseSource = Func<ImageSize, byte[]> (fun _ -> noise)

let private sample =
    column [ text "first"; text "second" ]

let private rawSample (c: IContainer) =
    c.Column (fun col ->
        col.Item().Text ("first") |> ignore
        col.Item().Text ("second") |> ignore)

/// The number of times the item content is built while generating a column of three items, each deferred by the given
/// wrapper function.
let private wrapperBuilds (defer: Content -> Content) =
    configure ()
    let calls = ref 0

    let counted: Content =
        fun slot ->
            calls.Value <- calls.Value + 1
            text "item" slot

    Pdf.bytes (wrapContent (column [ for _ in 1..3 -> defer counted ])) |> ignore
    calls.Value

/// The number of times the item content is built while generating a column of three items, each deferred by the given
/// raw QuestPDF call.
let private rawBuilds (defer: IContainer -> Action<IContainer> -> unit) =
    configure ()
    let calls = ref 0

    let counted =
        Action<IContainer> (fun inner ->
            calls.Value <- calls.Value + 1
            inner.Text ("item") |> ignore)

    let document =
        rawContent (fun c -> c.Column (fun col -> for _ in 1..3 do defer (col.Item ()) counted))

    document.GeneratePdf () |> ignore
    calls.Value

[<Tests>]
let tests =
    testList
        "Dynamic"
        [ testList
              "deferred"
              [ equivalent "lazyContent" (padding 5 >> lazyContent sample) (fun c -> c.Padding(5f).Lazy (fun inner -> rawSample inner))
                equivalent "lazyContentCached" (padding 5 >> lazyContentCached sample) (fun c ->
                    c.Padding(5f).LazyWithCache (fun inner -> rawSample inner))
                test "lazyContent builds its content as often as Lazy" {
                    Expect.equal (wrapperBuilds lazyContent) (rawBuilds (fun c build -> c.Lazy build)) "builds per item"
                }
                test "lazyContentCached builds its content as often as LazyWithCache" {
                    Expect.equal
                        (wrapperBuilds lazyContentCached)
                        (rawBuilds (fun c build -> c.LazyWithCache build))
                        "builds per item"
                }
                test "lazyContent and lazyContentCached differ in builds" {
                    Expect.notEqual (wrapperBuilds lazyContent) (wrapperBuilds lazyContentCached) "cached builds fewer times"
                }
                equivalent "nested lazyContent" (lazyContent (column [ lazyContent (text "inner"); text "outer" ])) (fun c ->
                    c.Lazy (fun outer ->
                        outer.Column (fun col ->
                            col.Item().Lazy (fun inner -> inner.Text ("inner") |> ignore)
                            col.Item().Text ("outer") |> ignore)))
                equivalent "capturePosition" (capturePosition "box" >> padding 5 >> text "captured") (fun c ->
                    c.CaptureContentPosition("box").Padding(5f).Text ("captured")
                    |> ignore) ]
          testList
              "components"
              [ testCase "Dynamic.ofComponent" (fun () ->
                    samePdf
                        "Dynamic.ofComponent"
                        (wrapContent (padding 5 >> Dynamic.ofComponent (PageLabels wrapperLabel)))
                        (rawContent (fun c -> c.Padding(5f).Dynamic (PageLabels rawLabel))))
                test "Dynamic.ofComponent spans the pages the component asks for" {
                    configure ()
                    let pdf = Pdf.bytes (wrapContent (Dynamic.ofComponent (PageLabels wrapperLabel)))
                    Expect.equal (PdfText.pageCount pdf) 3 "one page per composition"
                }
                testCase "Dynamic.ofStateful with Dynamic.element" (fun () ->
                    samePdf
                        "Dynamic.ofStateful"
                        (wrapContent (Dynamic.ofStateful (Counter (fun element c -> Content.run (Dynamic.element element) c))))
                        (rawContent (fun c -> c.Dynamic<int> (Counter (fun element inner -> inner.Element element)))))
                testCase "capturePosition read by a dynamic component" (fun () ->
                    samePdf
                        "capturePosition read by a dynamic component"
                        (wrapContent (
                            column [ width 120 >> capturePosition "box" >> text "captured"
                                     Dynamic.ofComponent (CaptureReader wrapperLabel) ]
                        ))
                        (rawContent (fun c ->
                            c.Column (fun col ->
                                col.Item().Width(120f).CaptureContentPosition("box").Text ("captured")
                                |> ignore

                                col.Item().Dynamic (CaptureReader rawLabel)))))
                test "a dynamic component reads the captured position" {
                    configure ()

                    let pdf =
                        Pdf.bytes (
                            wrapContent (
                                column [ width 120 >> capturePosition "box" >> text "captured"
                                         Dynamic.ofComponent (CaptureReader wrapperLabel) ]
                            )
                        )

                    Expect.stringContains (String.concat " " (PdfText.pageTexts pdf)) "1 at page 1, width 120" "the captured position"
                } ]
          testList
              "inline elements"
              [ equivalent
                    "Text.element"
                    (richText [ Text.span "before "
                                Text.element (width 8 >> height 8 >> background Colors.Red.Medium >> empty)
                                Text.span " after" ])
                    (fun c ->
                        c.Text (fun t ->
                            t.Span ("before ") |> ignore
                            t.Element (fun e -> e.Width(8f).Height(8f).Background (Colors.Red.Medium) |> ignore)
                            t.Span (" after") |> ignore))
                equivalent
                    "Text.elementWith"
                    (richText [ Text.span "before "
                                Text.elementWith TextInjectedElementAlignment.Top (width 8 >> height 20 >> background Colors.Red.Medium >> empty)
                                Text.span " after" ])
                    (fun c ->
                        c.Text (fun t ->
                            t.Span ("before ") |> ignore

                            t.Element (
                                (fun e -> e.Width(8f).Height(20f).Background (Colors.Red.Medium) |> ignore),
                                TextInjectedElementAlignment.Top
                            )

                            t.Span (" after") |> ignore))
                distinct
                    "Text.elementWith alignment changes the output"
                    (richText [ Text.span "before "
                                Text.elementWith TextInjectedElementAlignment.Top (width 8 >> height 20 >> background Colors.Red.Medium >> empty)
                                Text.span " after" ])
                    (fun c ->
                        c.Text (fun t ->
                            t.Span ("before ") |> ignore
                            t.Element (fun e -> e.Width(8f).Height(20f).Background (Colors.Red.Medium) |> ignore)
                            t.Span (" after") |> ignore))
                equivalent
                    "Text.element ignores Text.withStyle"
                    (richText [ Text.withStyle (Style.size 20) (Text.element (text "inline")) ])
                    (fun c -> c.Text (fun t -> t.Element (fun e -> e.Text ("inline") |> ignore))) ]
          testList
              "dynamic images"
              [ equivalent "Image.dynamic" (width 20 >> Image.dynamic (fun _ -> noise)) (fun c -> c.Width(20f).Image (noiseSource) |> ignore)
                for name, option, apply in dynamicImageForms do
                    equivalent $"Image.dynamicWith {name}" (width 20 >> Image.dynamicWith option (fun _ -> noise)) (fun c ->
                        c.Width(20f).Image (noiseSource) |> apply |> ignore)
                test "Image.dynamic receives the drawn size" {
                    configure ()
                    let mutable requested = None

                    Pdf.bytes (
                        wrapContent (
                            width 20
                            >> height 20
                            >> Image.dynamicWith (DynamicImage.dpi 72) (fun size ->
                                requested <- Some (size.Width, size.Height)
                                noise)
                        )
                    )
                    |> ignore

                    Expect.equal requested (Some (20, 20)) "20 pt at 72 dpi is 20 px"
                } ] ]
