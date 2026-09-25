module Program

open System.IO
open QuestPDF.FSharp

[<EntryPoint>]
let main argv =
    License.community ()
    Font.registerDirectory (Path.Combine (__SOURCE_DIRECTORY__, "..", "..", "fonts"))

    let path =
        argv
        |> Array.tryHead
        |> Option.defaultValue "invoice.pdf"

    Invoice.build () |> Pdf.save path
    printfn "Wrote %s" (Path.GetFullPath path)
    0
