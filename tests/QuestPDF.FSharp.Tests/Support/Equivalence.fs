/// Byte-equivalence assertions between wrapper documents and raw QuestPDF documents.
[<AutoOpen>]
module QuestPDF.FSharp.Tests.Support.Equivalence

open System
open System.IO
open Expecto
open QuestPDF.Fluent
open QuestPDF.Infrastructure
open QuestPDF.FSharp

let private artifactsDirectory () =
    Environment.GetEnvironmentVariable "QUESTPDF_FSHARP_TEST_ARTIFACTS"
    |> Option.ofObj

let private firstDifference (a: byte[]) (b: byte[]) =
    Seq.zip a b
    |> Seq.tryFindIndex (fun (x, y) -> x <> y)
    |> Option.defaultValue (min a.Length b.Length)

let private safeName (name: string) =
    String (
        name
        |> Seq.map (fun c -> if Char.IsLetterOrDigit c then c else '_')
        |> Seq.toArray
    )

let private writeArtifacts (name: string) (label: string) (document: IDocument) (pdf: byte[]) =
    match artifactsDirectory () with
    | None -> ()
    | Some directory ->
        Directory.CreateDirectory directory |> ignore
        let stem = Path.Combine (directory, $"{safeName name}.{label}")
        File.WriteAllBytes ($"{stem}.pdf", pdf)

        try
            document.GenerateImages ()
            |> Seq.iteri (fun i png -> File.WriteAllBytes ($"{stem}.{i + 1}.png", png))
        with _ ->
            ()

/// Asserts that both documents generate identical PDF bytes. On failure, writes both PDFs and their
/// page images to $QUESTPDF_FSHARP_TEST_ARTIFACTS when that variable is set.
let samePdf (name: string) (wrapped: IDocument) (raw: IDocument) =
    configure ()
    let a = Pdf.bytes wrapped
    let b = raw.GeneratePdf ()

    if a <> b then
        writeArtifacts name "wrapped" wrapped a
        writeArtifacts name "raw" raw b

    Expect.isTrue (a = b) $"{name}: PDFs differ at byte {firstDifference a b} (wrapped {a.Length} bytes, raw {b.Length} bytes)"

/// A test that wrapper content and raw content generate identical PDFs on the standard A5 page.
let equivalent (name: string) (content: Content) (raw: IContainer -> unit) =
    testCase name (fun () -> samePdf name (wrapContent content) (rawContent raw))

/// A test that wrapper page parts and a raw page builder generate identical PDFs.
let equivalentPage (name: string) (parts: PagePart list) (raw: PageDescriptor -> unit) =
    testCase name (fun () -> samePdf name (wrapPage parts) (rawPage raw))

/// A test that two documents generate identical PDFs.
let equivalentDoc (name: string) (wrapped: IDocument) (raw: IDocument) =
    testCase name (fun () -> samePdf name wrapped raw)

/// A negative control: wrapper content and raw content generate different PDFs.
let distinct (name: string) (content: Content) (raw: IContainer -> unit) =
    testCase name (fun () ->
        configure ()
        let a = Pdf.bytes (wrapContent content)
        let b = (rawContent raw).GeneratePdf ()
        Expect.isFalse (a = b) $"{name}: PDFs should differ but are identical ({a.Length} bytes)")
