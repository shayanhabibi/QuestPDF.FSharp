module QuestPDF.FSharp.Tests.FlowTests

open Expecto
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open QuestPDF.FSharp
open QuestPDF.FSharp.Tests.Support

/// A grey box of a given width, so the position of each inlined item is visible.
let private box (w: int) (label: string) : Content =
    background Colors.Grey.Lighten3 >> width w >> text label

let private rawBox (width: int) (label: string) (c: IContainer) =
    c.Background(Colors.Grey.Lighten3).Width(float32 width).Text (label)
    |> ignore

/// Boxes of varied widths, enough to wrap onto several lines of the A5 page.
let private sizes = [ 60; 110; 45; 90; 130; 70; 55; 120 ]

let private items =
    [ for i, w in List.indexed sizes do
          Inlined.item (box w $"item {i}") ]

let private rawInlined (settings: InlinedDescriptor -> unit) (c: IContainer) =
    c.Inlined (fun inlined ->
        settings inlined

        for i, w in List.indexed sizes do
            rawBox w $"item {i}" (inlined.Item ()))

/// The inlined settings, each with its raw counterpart.
let private inlinedForms: (string * InlinedPart * (InlinedDescriptor -> unit)) list =
    [ "spacing int", Inlined.spacing 12, (fun d -> d.Spacing (12f))
      "spacing float", Inlined.spacing 7.5, (fun d -> d.Spacing (7.5f))
      "spacing mm", Inlined.spacing (4 * mm), (fun d -> d.Spacing (4f, Unit.Millimetre))
      "spacingH", Inlined.spacingH 15, (fun d -> d.HorizontalSpacing (15f))
      "spacingH mm", Inlined.spacingH (5 * mm), (fun d -> d.HorizontalSpacing (5f, Unit.Millimetre))
      "spacingV", Inlined.spacingV 20, (fun d -> d.VerticalSpacing (20f))
      "spacingV mm", Inlined.spacingV (6 * mm), (fun d -> d.VerticalSpacing (6f, Unit.Millimetre))
      "baselineTop", Inlined.baselineTop, (fun d -> d.BaselineTop ())
      "baselineMiddle", Inlined.baselineMiddle, (fun d -> d.BaselineMiddle ())
      "baselineBottom", Inlined.baselineBottom, (fun d -> d.BaselineBottom ())
      "alignLeft", Inlined.alignLeft, (fun d -> d.AlignLeft ())
      "alignCenter", Inlined.alignCenter, (fun d -> d.AlignCenter ())
      "alignRight", Inlined.alignRight, (fun d -> d.AlignRight ())
      "alignJustify", Inlined.alignJustify, (fun d -> d.AlignJustify ())
      "alignSpaceAround", Inlined.alignSpaceAround, (fun d -> d.AlignSpaceAround ()) ]

/// Mixed-height items, so the baseline settings move them.
let private tallItems =
    [ Inlined.item (box 60 "a")
      Inlined.item (padding 20 >> box 60 "b")
      Inlined.item (box 60 "c") ]

let private rawTall (settings: InlinedDescriptor -> unit) (c: IContainer) =
    c.Inlined (fun inlined ->
        settings inlined
        rawBox 60 "a" (inlined.Item ())
        rawBox 60 "b" (inlined.Item().Padding (20f))
        rawBox 60 "c" (inlined.Item ()))

/// Enough paragraphs to fill both columns of the A5 page, so the spacer between them is drawn.
let private paragraphs =
    [ for i in 1..30 do
          text $"Paragraph {i}: the quick brown fox jumps over the lazy dog, again and again, until the column ends." ]

let private article = column paragraphs

let private rawArticle (c: IContainer) =
    c.Column (fun col ->
        for i in 1..30 do
            col.Item().Text ($"Paragraph {i}: the quick brown fox jumps over the lazy dog, again and again, until the column ends.")
            |> ignore)

let private rawMultiColumn (settings: MultiColumnDescriptor -> unit) (c: IContainer) =
    c.MultiColumn (fun multi ->
        settings multi
        rawArticle (multi.Content ()))

[<Tests>]
let tests =
    testList
        "Flow"
        [ testList
              "inlined"
              [ equivalent "items" (inlined items) (rawInlined ignore)
                equivalent "empty" (inlined []) (fun c -> c.Inlined ignore)
                for name, part, rawPart in inlinedForms do
                    equivalent name (inlined (part :: items)) (rawInlined rawPart)
                for name, part, rawPart in inlinedForms do
                    if name.StartsWith "spacing" || (name.StartsWith "align" && name <> "alignLeft") then
                        distinct $"{name} changes the output" (inlined (part :: items)) (rawInlined ignore)
                for name, part, rawPart in inlinedForms do
                    if name.StartsWith "baseline" then
                        equivalent $"{name} with mixed heights" (inlined (part :: tallItems)) (rawTall rawPart)
                distinct "baselineMiddle moves mixed-height items" (inlined (Inlined.baselineMiddle :: tallItems)) (rawTall ignore)
                distinct "baselineBottom moves mixed-height items" (inlined (Inlined.baselineBottom :: tallItems)) (rawTall ignore)
                equivalent
                    "items and settings from a list expression"
                    (inlined
                        [ Inlined.spacing 5
                          Inlined.alignCenter
                          for i, w in List.indexed sizes do
                              if i % 2 = 0 then
                                  Inlined.item (box w $"item {i}") ])
                    (fun c ->
                        c.Inlined (fun inlined ->
                            inlined.Spacing (5f)
                            inlined.AlignCenter ()

                            for i, w in List.indexed sizes do
                                if i % 2 = 0 then
                                    rawBox w $"item {i}" (inlined.Item ()))) ]
          testList
              "multiColumn"
              [ equivalent "content" (multiColumn [ MultiColumn.content article ]) (rawMultiColumn ignore)
                equivalent "columns" (multiColumn [ MultiColumn.columns 3; MultiColumn.content article ]) (rawMultiColumn (fun m -> m.Columns (3)))
                equivalent "columns 2 is the default" (multiColumn [ MultiColumn.columns 2; MultiColumn.content article ]) (rawMultiColumn ignore)
                distinct "columns changes the output" (multiColumn [ MultiColumn.columns 3; MultiColumn.content article ]) (rawMultiColumn ignore)
                equivalent "spacing int" (multiColumn [ MultiColumn.spacing 20; MultiColumn.content article ]) (rawMultiColumn (fun m -> m.Spacing (20f)))
                equivalent "spacing float" (multiColumn [ MultiColumn.spacing 12.5; MultiColumn.content article ]) (rawMultiColumn (fun m -> m.Spacing (12.5f)))
                equivalent
                    "spacing mm"
                    (multiColumn [ MultiColumn.spacing (8 * mm); MultiColumn.content article ])
                    (rawMultiColumn (fun m -> m.Spacing (8f, Unit.Millimetre)))
                distinct "spacing changes the output" (multiColumn [ MultiColumn.spacing 20; MultiColumn.content article ]) (rawMultiColumn ignore)
                equivalent
                    "balanceHeight"
                    (multiColumn [ MultiColumn.balanceHeight; MultiColumn.content article ])
                    (rawMultiColumn (fun m -> m.BalanceHeight ()))
                distinct "balanceHeight changes the output" (multiColumn [ MultiColumn.balanceHeight; MultiColumn.content article ]) (rawMultiColumn ignore)
                equivalent
                    "spacer"
                    (multiColumn [ MultiColumn.spacing 20; MultiColumn.spacer (background Colors.Grey.Medium >> empty); MultiColumn.content article ])
                    (rawMultiColumn (fun m ->
                        m.Spacing (20f)
                        m.Spacer().Background (Colors.Grey.Medium) |> ignore))
                distinct
                    "spacer changes the output"
                    (multiColumn [ MultiColumn.spacing 20; MultiColumn.spacer (background Colors.Grey.Medium >> empty); MultiColumn.content article ])
                    (rawMultiColumn (fun m -> m.Spacing (20f)))
                equivalent
                    "all parts"
                    (multiColumn
                        [ MultiColumn.columns 3
                          MultiColumn.spacing 15
                          MultiColumn.balanceHeight
                          MultiColumn.spacer (background Colors.Grey.Lighten2 >> empty)
                          MultiColumn.content article ])
                    (rawMultiColumn (fun m ->
                        m.Columns (3)
                        m.Spacing (15f)
                        m.BalanceHeight ()
                        m.Spacer().Background (Colors.Grey.Lighten2) |> ignore)) ] ]
