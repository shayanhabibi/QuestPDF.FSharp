module QuestPDF.FSharp.Tests.CoreTests

open Expecto
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open QuestPDF.FSharp
open QuestPDF.FSharp.Tests.Support

/// A top-level composition without a type annotation; it compiles free of the value restriction.
let probe = padding 10 >> text "probe"

[<Tests>]
let tests =
    testList
        "Core"
        [ equivalent "raw is the direct call" (raw (fun c -> c.Text ("raw") |> ignore)) (fun c -> c.Text ("raw") |> ignore)
          equivalent "fluent discards the descriptor" (fluent (fun c -> c.Text("fluent").Underline ())) (fun c ->
              c.Text("fluent").Underline () |> ignore)
          equivalent "modify is the chained call" (modify (fun c -> c.Padding (5f)) >> text "m") (fun c -> c.Padding(5f).Text ("m") |> ignore)
          equivalentDoc
              "Content.run mounts wrapper content in raw code"
              (rawPage (fun p ->
                  p.Size PageSizes.A5
                  p.Margin 20f
                  Content.run (padding 5 >> text "x") (p.Content ())))
              (rawContent (fun c -> c.Padding(5f).Text ("x") |> ignore))
          equivalent "empty is an empty page" empty ignore
          equivalent "top-level unannotated value" probe (fun c -> c.Padding(10f).Text ("probe") |> ignore)
          distinct "different text differs" (text "a") (fun c -> c.Text ("b") |> ignore) ]
