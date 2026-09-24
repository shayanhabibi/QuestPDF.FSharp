module QuestPDF.FSharp.Tests.StyleTests

open System
open Expecto
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
          check "lineHeight" (Style.lineHeight 1.5) (fun t -> t.LineHeight (Nullable 1.5f))
          check
              "composition applies left to right"
              (Style.size 14
               >> Style.bold
               >> Style.color Colors.Blue.Darken2)
              (fun t -> t.FontSize(14f).Bold().FontColor (Colors.Blue.Darken2))
          distinct "bold is not italic" (styled Style.bold) (fun c -> c.Text("styled").Italic () |> ignore) ]
