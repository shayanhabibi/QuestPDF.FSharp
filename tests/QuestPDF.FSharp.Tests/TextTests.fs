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

/// A paragraph of several lines, so justification changes every line but the last.
let private paragraph = String.replicate 30 "aligned words "

/// Two layouts for the alignment tests. Across both, every alignment and the absence of one render differently.
/// Each context is a name, the wrapper direction and earlier parts, and the raw direction and earlier calls.
let private contexts: (string * Modifier * TextPart list * (IContainer -> IContainer) * (TextDescriptor -> unit)) list =
    [ "left to right", id, [], id, ignore
      "right to left after alignCenter",
      modify (fun c -> c.ContentFromRightToLeft ()),
      [ Text.alignCenter ],
      (fun c -> c.ContentFromRightToLeft ()),
      (fun t -> t.AlignCenter ()) ]

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
              "nested withStyle: an inner ofTextStyle merges over the outer style"
              (richText
                  [ Text.styled (Style.ofTextStyle (TextStyle.Default.FontSize 20f)) "a"
                    |> Text.withStyle Style.bold ])
              (rich (fun t ->
                  t.Span("a").Bold().Style (TextStyle.Default.FontSize 20f)
                  |> ignore))
          distinct
              "nested withStyle: an inner ofTextStyle keeps the outer style"
              (richText
                  [ Text.styled (Style.ofTextStyle (TextStyle.Default.FontSize 20f)) "a"
                    |> Text.withStyle Style.bold ])
              (rich (fun t ->
                  t.Span("a").Style (TextStyle.Default.FontSize 20f)
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
              [ for context, wrapperDirection, earlier, rawDirection, before in contexts do
                    for name, part, apply in alignments do
                        equivalent
                            $"{name}, {context}"
                            (wrapperDirection
                             >> richText (earlier @ [ part; Text.span paragraph ]))
                            (fun c ->
                                (rawDirection c)
                                    .Text (fun t ->
                                        before t
                                        apply t
                                        t.Span (paragraph) |> ignore))
                test "the alignments render differently across the contexts" {
                    configure ()

                    let renderings (apply: TextDescriptor -> unit) =
                        [ for _, _, _, rawDirection, before in contexts ->
                              (rawContent (fun c ->
                                  (rawDirection c)
                                      .Text (fun t ->
                                          before t
                                          apply t
                                          t.Span (paragraph) |> ignore)))
                                  .GeneratePdf () ]

                    let rendered =
                        ("no alignment", renderings ignore)
                        :: [ for name, _, apply in alignments -> name, renderings apply ]

                    let collisions =
                        [ for a, x in rendered do
                              for b, y in rendered do
                                  if a < b && x = y then
                                      yield a, b ]

                    Expect.isEmpty collisions "every alignment differs from every other and from no alignment in some context"
                } ] ]
