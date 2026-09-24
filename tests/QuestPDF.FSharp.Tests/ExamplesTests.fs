module QuestPDF.FSharp.Tests.ExamplesTests

open Expecto
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open QuestPDF.FSharp
open QuestPDF.FSharp.Tests.Support

/// Worked example (a), with pinned dates.
let hello =
    document
        [ Meta.dated fixedDate
          page
              [ Page.size PageSizes.A4
                Page.margin (2 * cm)
                Page.content (
                    padding 10
                    >> background Colors.Grey.Lighten3
                    >> styledText (Style.size 20) "Hello, world!"
                ) ] ]

let rawHello =
    rawPage (fun p ->
        p.Size PageSizes.A4
        p.Margin (2f, Unit.Centimetre)

        p.Content().Padding(10f).Background(Colors.Grey.Lighten3).Text("Hello, world!").FontSize (20f)
        |> ignore)

[<Tests>]
let tests = testList "Examples" [ equivalentDoc "hello world" hello rawHello ]
