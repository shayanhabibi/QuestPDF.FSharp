module QuestPDF.FSharp.Tests.ModifierTests

open Expecto
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open QuestPDF.FSharp
open QuestPDF.FSharp.Tests.Support

/// name, the modifier at 10 (int), 7.5 (float) and 3 mm, and the raw call it maps to.
let private lengthModifiers: (string * Modifier * Modifier * Modifier * (IContainer -> float32 -> Unit -> IContainer)) list =
    [ "padding", padding 10, padding 7.5, padding (3 * mm), (fun c v u -> c.Padding (v, u))
      "paddingV", paddingV 10, paddingV 7.5, paddingV (3 * mm), (fun c v u -> c.PaddingVertical (v, u))
      "paddingH", paddingH 10, paddingH 7.5, paddingH (3 * mm), (fun c v u -> c.PaddingHorizontal (v, u))
      "paddingTop", paddingTop 10, paddingTop 7.5, paddingTop (3 * mm), (fun c v u -> c.PaddingTop (v, u))
      "paddingBottom", paddingBottom 10, paddingBottom 7.5, paddingBottom (3 * mm), (fun c v u -> c.PaddingBottom (v, u))
      "paddingLeft", paddingLeft 10, paddingLeft 7.5, paddingLeft (3 * mm), (fun c v u -> c.PaddingLeft (v, u))
      "paddingRight", paddingRight 10, paddingRight 7.5, paddingRight (3 * mm), (fun c v u -> c.PaddingRight (v, u)) ]

[<Tests>]
let tests =
    testList
        "Modifiers"
        [ for name, ofInt, ofFloat, ofMm, apply in lengthModifiers do
              equivalent $"{name} int" (ofInt >> text "p") (fun c -> (apply c 10f Unit.Point).Text ("p") |> ignore)
              equivalent $"{name} float" (ofFloat >> text "p") (fun c -> (apply c 7.5f Unit.Point).Text ("p") |> ignore)
              equivalent $"{name} mm" (ofMm >> text "p") (fun c -> (apply c 3f Unit.Millimetre).Text ("p") |> ignore)
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
