module QuestPDF.FSharp.Tests.TextTests

open Expecto
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open QuestPDF.FSharp
open QuestPDF.FSharp.Tests.Support

let private rich (build: TextDescriptor -> unit) (c: IContainer) =
    c.Text (fun t -> build t)

let private alignments =
    [ "alignLeft", Text.alignLeft, (fun (t: TextDescriptor) -> t.AlignLeft ())
      "alignCenter", Text.alignCenter, (fun t -> t.AlignCenter ())
      "alignRight", Text.alignRight, (fun t -> t.AlignRight ())
      "alignStart", Text.alignStart, (fun t -> t.AlignStart ())
      "alignEnd", Text.alignEnd, (fun t -> t.AlignEnd ())
      "justify", Text.justify, (fun t -> t.Justify ()) ]

[<Tests>]
let tests =
    testList
        "Text"
        [ equivalent "text" (text "hello") (fun c -> c.Text ("hello") |> ignore)
          equivalent "styledText" (styledText (Style.size 20 >> Style.bold) "hello") (fun c -> c.Text("hello").FontSize(20f).Bold () |> ignore)
          equivalent
              "richText spans"
              (richText [ Text.span "a"; Text.span "b" ])
              (rich (fun t ->
                  t.Span ("a") |> ignore
                  t.Span ("b") |> ignore))
          equivalent "styled span" (richText [ Text.styled Style.bold "a" ]) (rich (fun t -> t.Span("a").Bold () |> ignore))
          equivalent
              "withStyle on a span"
              (richText [ Text.span "a" |> Text.withStyle (Style.size 14) ])
              (rich (fun t -> t.Span("a").FontSize (14f) |> ignore))
          equivalent
              "withStyle on a page number"
              (richText [ Text.pageNumber |> Text.withStyle Style.bold ])
              (rich (fun t -> t.CurrentPageNumber().Bold () |> ignore))
          equivalent "totalPages" (richText [ Text.totalPages ]) (rich (fun t -> t.TotalPages () |> ignore))
          equivalent
              "nested withStyle applies the outer style, then the inner"
              (richText
                  [ Text.styled (Style.color Colors.Blue.Medium) "a"
                    |> Text.withStyle (Style.color Colors.Red.Medium) ])
              (rich (fun t ->
                  t.Span("a").FontColor(Colors.Red.Medium).FontColor (Colors.Blue.Medium)
                  |> ignore))
          distinct
              "nested withStyle: the inner style wins"
              (richText
                  [ Text.styled (Style.color Colors.Blue.Medium) "a"
                    |> Text.withStyle (Style.color Colors.Red.Medium) ])
              (rich (fun t ->
                  t.Span("a").FontColor (Colors.Red.Medium)
                  |> ignore))
          equivalent
              "lineBreak is a newline span"
              (richText [ Text.span "a"; Text.lineBreak; Text.span "b" ])
              (rich (fun t ->
                  t.Span ("a") |> ignore
                  t.Span ("\n") |> ignore
                  t.Span ("b") |> ignore))
          equivalent
              "Text.style is the block default style"
              (richText [ Text.style (Style.size 18); Text.span "a" ])
              (rich (fun t ->
                  t.DefaultTextStyle (fun (s: TextStyle) -> s.FontSize 18f)
                  t.Span ("a") |> ignore))
          testList
              "alignment"
              [ for name, part, apply in alignments ->
                    equivalent
                        name
                        (richText [ part; Text.span "aligned" ])
                        (rich (fun t ->
                            apply t
                            t.Span ("aligned") |> ignore)) ] ]
