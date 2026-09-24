module QuestPDF.FSharp.Tests.GenerateTests

open System
open System.IO
open Expecto
open QuestPDF.Fluent
open QuestPDF.Infrastructure
open QuestPDF.FSharp
open QuestPDF.FSharp.Tests.Support

let private sample () =
    wrapContent (text "generated")

/// Runs the body, then restores the global settings the Setup and license tests change.
let private restoringSettings (body: unit -> unit) () =
    let license = QuestPDF.Settings.License
    let systemFonts = QuestPDF.Settings.UseSystemFonts
    let families = QuestPDF.Settings.ThrowOnMissingFontFamilies
    let glyphs = QuestPDF.Settings.ThrowOnMissingTextGlyphs

    try
        body ()
    finally
        QuestPDF.Settings.License <- license
        QuestPDF.Settings.UseSystemFonts <- systemFonts
        QuestPDF.Settings.ThrowOnMissingFontFamilies <- families
        QuestPDF.Settings.ThrowOnMissingTextGlyphs <- glyphs

let generation =
    testList
        "Pdf"
        [ test "bytes equals write to a stream" {
              configure ()
              use stream = new MemoryStream ()
              sample () |> Pdf.write stream
              Expect.isTrue (stream.ToArray () = Pdf.bytes (sample ())) "write"
          }
          test "save writes the bytes to a path" {
              configure ()

              let path =
                  Path.Combine (Path.GetTempPath (), $"questpdf-fsharp-{Guid.NewGuid ():N}.pdf")

              try
                  sample () |> Pdf.save path
                  Expect.isTrue (File.ReadAllBytes path = Pdf.bytes (sample ())) "save"
              finally
                  File.Delete path
          } ]

let settings =
    testSequenced
    <| testList
        "settings"
        [ testCase "generation without a license raises the wrapper's message"
          <| restoringSettings (fun () ->
              configure ()
              QuestPDF.Settings.License <- Nullable ()

              let error = Expect.throwsC (fun () -> sample () |> Pdf.bytes |> ignore) id
              Expect.equal (error.GetType ()) typeof<InvalidOperationException> "type"
              Expect.equal error.Message "Call License.community () (or professional/enterprise) before generating." "message")
          testCase "every generator checks the license first"
          <| restoringSettings (fun () ->
              configure ()
              QuestPDF.Settings.License <- Nullable ()

              let generators: (string * (IDocument -> unit)) list =
                  [ "images", (Pdf.images ImageFormat.Png 72 >> ignore)
                    "svgs", (Pdf.svgs >> ignore)
                    "companion", Pdf.companion
                    "show", Pdf.show ]

              for name, generate in generators do
                  let error = Expect.throwsC (fun () -> generate (sample ())) id
                  Expect.equal (error.GetType ()) typeof<InvalidOperationException> $"{name}: type"
                  Expect.stringStarts error.Message "Call License.community ()" $"{name}: message")
          testCase "License functions set the license"
          <| restoringSettings (fun () ->
              License.professional ()
              Expect.equal QuestPDF.Settings.License (Nullable LicenseType.Professional) "professional"
              License.enterprise ()
              Expect.equal QuestPDF.Settings.License (Nullable LicenseType.Enterprise) "enterprise"
              License.community ()
              Expect.equal QuestPDF.Settings.License (Nullable LicenseType.Community) "community")
          testCase "Font.strict sets both missing-font flags"
          <| restoringSettings (fun () ->
              Font.strict false

              Expect.equal (QuestPDF.Settings.ThrowOnMissingFontFamilies, QuestPDF.Settings.ThrowOnMissingTextGlyphs) (false, false) "off"

              Font.strict true
              Expect.equal (QuestPDF.Settings.ThrowOnMissingFontFamilies, QuestPDF.Settings.ThrowOnMissingTextGlyphs) (true, true) "on")
          testCase "Font.useSystemFonts sets UseSystemFonts"
          <| restoringSettings (fun () ->
              Font.useSystemFonts true
              Expect.isTrue QuestPDF.Settings.UseSystemFonts "on"
              Font.useSystemFonts false
              Expect.isFalse QuestPDF.Settings.UseSystemFonts "off")
          testCase "Font.registered lists Lato"
          <| fun () -> Expect.contains (Font.registered () |> List.map _.FamilyName) "Lato" "the default font" ]

let support =
    testList
        "support"
        [ test "normalizeIds makes PDF/UA output reproducible" {
              configure ()

              let build () =
                  (Document.Create (fun dc ->
                      dc.Page (fun p -> p.Content().Text ("ua") |> ignore)
                      |> ignore))
                      .WithMetadata(pinnedMetadata ())
                      .WithSettings(DocumentSettings (PDFUA_Conformance = PDFUA_Conformance.PDFUA_1))
                      .GeneratePdf ()

              let a, b = build (), build ()
              Expect.isFalse (a = b) "PDF/UA output carries a random id"
              Expect.equal (normalizeIds a).Length a.Length "same length"
              Expect.isTrue (normalizeIds a = normalizeIds b) "equal after normalizing"
          } ]

[<Tests>]
let tests = testList "Generate" [ generation; settings; support ]
