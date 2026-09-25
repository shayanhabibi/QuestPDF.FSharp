module QuestPDF.FSharp.Tests.StyledBoxTests

open Expecto
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open QuestPDF.FSharp
open QuestPDF.FSharp.Tests.Support

/// A box hugging "box content" inside a page margin, so gradients and shadows are visible and a shadow is on the page.
let private boxed (modifier: Modifier) : Content =
    modify (fun c -> c.Padding(30f).AlignLeft().AlignTop ())
    >> modifier
    >> padding 10
    >> text "box content"

let private rawBoxed (apply: IContainer -> IContainer) (c: IContainer) =
    (apply (c.Padding(30f).AlignLeft().AlignTop ())).Padding(10f).Text ("box content")
    |> ignore

let private gradient = [ Colors.Red.Medium; Colors.Blue.Medium ]

let private rawGradient = [| Colors.Red.Medium; Colors.Blue.Medium |]

/// A raw BoxShadowStyle with the given settings.
let private rawShadow (set: BoxShadowStyle -> unit) =
    let style = BoxShadowStyle ()
    set style
    style

[<Tests>]
let tests =
    testList
        "Styled box"
        [ testList
              "backgroundGradient"
              [ equivalent "int angle" (boxed (backgroundGradient 45 gradient)) (rawBoxed (fun c -> c.BackgroundLinearGradient (45f, rawGradient)))
                equivalent
                    "float angle, three colours"
                    (boxed (backgroundGradient 22.5 [ Colors.Red.Medium; Colors.Green.Medium; Colors.Blue.Medium ]))
                    (rawBoxed (fun c -> c.BackgroundLinearGradient (22.5f, [| Colors.Red.Medium; Colors.Green.Medium; Colors.Blue.Medium |])))
                distinct "changes the output" (boxed (backgroundGradient 45 gradient)) (rawBoxed id)
                distinct "the angle changes the output" (boxed (backgroundGradient 45 gradient)) (rawBoxed (fun c -> c.BackgroundLinearGradient (90f, rawGradient))) ]
          testList
              "borderGradient"
              [ equivalent "int angle" (boxed (border 4 >> borderGradient 90 gradient)) (rawBoxed (fun c -> c.Border(4f).BorderLinearGradient (90f, rawGradient)))
                equivalent
                    "float angle"
                    (boxed (border 4 >> borderGradient 12.5 gradient))
                    (rawBoxed (fun c -> c.Border(4f).BorderLinearGradient (12.5f, rawGradient)))
                distinct "changes the output" (boxed (border 4 >> borderGradient 90 gradient)) (rawBoxed (fun c -> c.Border (4f))) ]
          testList
              "shadow"
              [ equivalent
                    "every setting"
                    (boxed (
                        shadow
                            [ Shadow.offsetX 3
                              Shadow.offsetY 4.5
                              Shadow.blur 6
                              Shadow.spread 1.5
                              Shadow.color Colors.Black ]
                        >> background Colors.White
                    ))
                    (rawBoxed (fun c ->
                        c
                            .Shadow(
                                rawShadow (fun s ->
                                    s.OffsetX <- 3f
                                    s.OffsetY <- 4.5f
                                    s.Blur <- 6f
                                    s.Spread <- 1.5f
                                    s.Color <- Colors.Black)
                            )
                            .Background (Colors.White)))
                equivalent "offset sets both offsets" (boxed (shadow [ Shadow.offset 3 4 ])) (rawBoxed (fun c ->
                    c.Shadow (
                        rawShadow (fun s ->
                            s.OffsetX <- 3f
                            s.OffsetY <- 4f)
                    )))
                equivalent "lengths convert to points" (boxed (shadow [ Shadow.blur (2 * mm); Shadow.spread (1 * mm) ])) (rawBoxed (fun c ->
                    c.Shadow (
                        rawShadow (fun s ->
                            s.Blur <- Length.points (2 * mm)
                            s.Spread <- Length.points (1 * mm))
                    )))
                equivalent "no settings is the default style" (boxed (shadow [])) (rawBoxed (fun c -> c.Shadow (BoxShadowStyle ())))
                distinct "changes the output" (boxed (shadow [ Shadow.offset 3 4; Shadow.blur 2 ])) (rawBoxed id)
                distinct "the colour changes the output" (boxed (shadow [ Shadow.spread 4; Shadow.color Colors.Red.Medium ])) (rawBoxed (fun c ->
                    c.Shadow (rawShadow (fun s -> s.Spread <- 4f)))) ] ]
