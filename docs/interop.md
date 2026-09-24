# Interop

## Fluent code inside wrapper content

```fsharp
open QuestPDF.Fluent
open QuestPDF.Infrastructure
open QuestPDF.FSharp   // last, so its names win

column [
    raw (fun c -> c.Text("raw").Underline() |> ignore)       // IContainer -> unit
    fluent (fun c -> c.Text "fluent")                        // IContainer -> 'a, result discarded
    modify (fun c -> c.Padding 5f) >> text "modified"        // IContainer -> IContainer as a Modifier
]
```

- `Text.raw (fun t -> ...)` does the same for a `TextDescriptor` inside `richText`.
- Any lambda over a descriptor is a valid part: `row [ fun r -> r.AutoItem().Text "x" |> ignore ]`.
- `Measured.*` holds the non-inline implementations behind every length or number shim, for callers that already
  have a `Length`.

## Wrapper content inside fluent code

```fsharp
Document.Create(fun dc ->
    dc.Page(fun p -> Content.run (padding 5 >> text "x") (p.Content())) |> ignore)
```

## Mapping from fluent calls

`tests/QuestPDF.FSharp.Tests/Coverage.fs` lists every public QuestPDF fluent member with its wrapper functions, or
the reason it is reached through `raw`. Examples:

| Fluent | Wrapper |
|--------|---------|
| `c.Padding(10f)` | `padding 10` |
| `c.PaddingHorizontal(5f, Unit.Millimetre)` | `paddingH (5 * mm)` |
| `c.Row(fun r -> r.RelativeItem().Text "x")` | `row [ Row.fill (text "x") ]` |
| `c.Text(fun t -> t.Span("x").Bold())` | `richText [ Text.styled Style.bold "x" ]` |
| `t.CurrentPageNumber()` | `Text.pageNumber` |
| `page.Margin(2f, Unit.Centimetre)` | `Page.margin (2 * cm)` |
| `document.GenerateImages(settings)` | `Pdf.images ImageFormat.Png 96 document` |
