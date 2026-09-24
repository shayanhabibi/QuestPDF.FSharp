module QuestPDF.FSharp.Tests.TransformTests

open Expecto
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open QuestPDF.FSharp
open QuestPDF.FSharp.Tests.Support

/// A shaded two-word row, so every transform moves or mirrors visible content.
let private sample: Content =
    background Colors.Grey.Lighten3
    >> row [ Row.auto (text "left"); Row.auto (text "right") ]

let private rawSample (c: IContainer) =
    c
        .Background(Colors.Grey.Lighten3)
        .Row (fun r ->
            r.AutoItem().Text ("left") |> ignore
            r.AutoItem().Text ("right") |> ignore)

/// The transform modifiers, with the raw call each maps to.
let private transforms: (string * Modifier * (IContainer -> IContainer)) list =
    [ "rotate int", rotate 30, (fun c -> c.Rotate (30f))
      "rotate float", rotate 12.5, (fun c -> c.Rotate (12.5f))
      "rotateLayoutCw", rotateLayoutCw, (fun c -> c.RotateLayoutClockwise ())
      "rotateLayoutCcw", rotateLayoutCcw, (fun c -> c.RotateLayoutCounterclockwise ())
      "scale int", scale 2, (fun c -> c.Scale (2f))
      "scale float", scale 0.5, (fun c -> c.Scale (0.5f))
      "scaleH", scaleH 1.5, (fun c -> c.ScaleHorizontal (1.5f))
      "scaleV", scaleV 1.5, (fun c -> c.ScaleVertical (1.5f))
      "flipH", flipH, (fun c -> c.FlipHorizontal ())
      "flipV", flipV, (fun c -> c.FlipVertical ())
      "flipOver", flipOver, (fun c -> c.FlipOver ())
      "offsetX int", offsetX 20, (fun c -> c.OffsetX (20f))
      "offsetX mm", offsetX (5 * mm), (fun c -> c.OffsetX (5f, Unit.Millimetre))
      "offsetY float", offsetY 12.5, (fun c -> c.OffsetY (12.5f))
      "offsetY mm", offsetY (5 * mm), (fun c -> c.OffsetY (5f, Unit.Millimetre))
      "contentRtl", contentRtl, (fun c -> c.ContentFromRightToLeft ())
      "debugArea", debugArea "area", (fun c -> c.DebugArea ("area")) ]

/// A component that draws a fixed label.
type private Label(value: string) =
    interface IComponent with
        member _.Compose container =
            container.Padding(4f).Text (value) |> ignore

[<Tests>]
let tests =
    testList
        "Transforms"
        [ for name, modifier, apply in transforms do
              equivalent name (modifier >> sample) (fun c -> rawSample (apply c))
              distinct $"{name} changes the output" (modifier >> sample) rawSample
          equivalent "contentLtr" (contentLtr >> sample) (fun c -> rawSample (c.ContentFromLeftToRight ()))
          test "the transforms render differently" {
              configure ()

              let rendered =
                  [ for name, _, apply in transforms -> name, (rawContent (fun c -> rawSample (apply c))).GeneratePdf () ]

              let collisions =
                  [ for a, x in rendered do
                        for b, y in rendered do
                            if a < b && x = y then
                                yield a, b ]

              Expect.isEmpty collisions "every transform has a distinct rendering"
          }
          equivalent
              "zIndex"
              (layers
                  [ Layers.primary (
                        zIndex 2
                        >> background Colors.Red.Lighten3
                        >> text "front"
                    )
                    Layers.layer (
                        zIndex 1
                        >> background Colors.Blue.Lighten3
                        >> text "back"
                    ) ])
              (fun c ->
                  c.Layers (fun l ->
                      l.PrimaryLayer().ZIndex(2).Background(Colors.Red.Lighten3).Text ("front")
                      |> ignore

                      l.Layer().ZIndex(1).Background(Colors.Blue.Lighten3).Text ("back")
                      |> ignore))
          distinct
              "zIndex changes the output"
              (layers
                  [ Layers.primary (
                        zIndex 2
                        >> background Colors.Red.Lighten3
                        >> text "front"
                    )
                    Layers.layer (
                        zIndex 1
                        >> background Colors.Blue.Lighten3
                        >> text "back"
                    ) ])
              (fun c ->
                  c.Layers (fun l ->
                      l.PrimaryLayer().Background(Colors.Red.Lighten3).Text ("front")
                      |> ignore

                      l.Layer().Background(Colors.Blue.Lighten3).Text ("back")
                      |> ignore))
          equivalent
              "Content.ofComponent"
              (padding 5
               >> Content.ofComponent (Label "component"))
              (fun c -> c.Padding(5f).Component (Label "component"))
          distinct
              "Content.ofComponent draws the component"
              (padding 5
               >> Content.ofComponent (Label "component"))
              (fun c -> c.Padding (5f) |> ignore) ]
