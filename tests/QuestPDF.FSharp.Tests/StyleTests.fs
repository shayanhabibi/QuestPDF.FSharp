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
          distinct "bold is not italic" (styled Style.bold) (fun c -> c.Text("styled").Italic () |> ignore) ]
