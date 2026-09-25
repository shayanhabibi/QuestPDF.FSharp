module FSharp.QuestPDF.Tests.OperationsTests

open System
open System.IO
open Expecto
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open FSharp.QuestPDF
open FSharp.QuestPDF.Tests.Support
open UglyToad.PdfPig

/// A pinned document of one page per label; each page shows its label and its page number.
let private labelled (labels: string list) : IDocument =
    document
        [ Meta.dated fixedDate
          for label in labels do
              page
                  [ Page.size PageSizes.A6
                    Page.margin 20
                    Page.content (richText [ Text.span $"{label} "; Text.pageNumber ]) ] ]

let private pages () =
    [ labelled [ "a"; "a" ]; labelled [ "b"; "b"; "b" ] ]

let private mergeTests =
    testList
        "merge"
        [ equivalentDoc
              "Pdf.merge keeps original page numbers by default"
              (Pdf.merge [ Meta.dated fixedDate ] (pages ()))
              (Document.Merge(pages ()).WithMetadata (pinnedMetadata ()))
          equivalentDoc
              "Merge.originalPageNumbers is UseOriginalPageNumbers"
              (Pdf.merge [ Merge.originalPageNumbers; Meta.dated fixedDate ] (pages ()))
              (Document.Merge(pages ()).UseOriginalPageNumbers().WithMetadata (pinnedMetadata ()))
          equivalentDoc
              "Merge.continuousPageNumbers is UseContinuousPageNumbers"
              (Pdf.merge [ Merge.continuousPageNumbers; Meta.dated fixedDate ] (pages ()))
              (Document.Merge(pages ()).UseContinuousPageNumbers().WithMetadata (pinnedMetadata ()))
          equivalentDoc
              "Pdf.merge applies settings items"
              (Pdf.merge [ Meta.dated fixedDate; Output.compress false ] (pages ()))
              (Document.Merge(pages ()).WithMetadata(pinnedMetadata ()).WithSettings (DocumentSettings (CompressDocument = false)))
          test "the numbering items number the pages" {
              configure ()

              let texts parts =
                  Pdf.merge parts (pages ())
                  |> Pdf.bytes
                  |> pageTexts

              Expect.equal (texts []) [ "a 1"; "a 2"; "b 1"; "b 2"; "b 3" ] "original"

              Expect.equal (texts [ Merge.continuousPageNumbers ]) [ "a 1"; "a 2"; "b 3"; "b 4"; "b 5" ] "continuous"
          }
          test "a page in the list of Pdf.merge raises" {
              Expect.throwsT<ArgumentException> (fun () -> Pdf.merge [ page [] ] (pages ()) |> ignore) "page"
          }
          test "a Merge item in the list of document raises" {
              Expect.throwsT<ArgumentException> (fun () -> document [ Merge.continuousPageNumbers ] |> ignore) "Merge item"
          } ]

/// A temporary directory holding a four-page source PDF, a two-page stamp PDF and a text file.
type private Workspace() =
    let directory =
        Directory.CreateDirectory (Path.Combine (Path.GetTempPath (), $"fsharp-questpdf-ops-{Guid.NewGuid ():N}"))

    do
        configure ()

        labelled [ "one"; "two"; "three"; "four" ]
        |> Pdf.save (Path.Combine (directory.FullName, "source.pdf"))

        labelled [ "stamp"; "seal" ]
        |> Pdf.save (Path.Combine (directory.FullName, "stamp.pdf"))

        File.WriteAllText (Path.Combine (directory.FullName, "notes.txt"), "notes")

    member _.Path(name: string) =
        Path.Combine (directory.FullName, name)

    member this.Source = this.Path "source.pdf"
    member this.Stamp = this.Path "stamp.pdf"
    member this.Notes = this.Path "notes.txt"

    interface IDisposable with
        member _.Dispose() =
            directory.Delete true

/// A test that a PdfFile pipeline writes the file of the equivalent fluent DocumentOperation chain, up to the /ID of
/// the trailer.
let private sameFile (name: string) (wrapped: Workspace -> PdfFile) (raw: Workspace -> DocumentOperation) =
    testCase name (fun () ->
        use workspace = new Workspace ()
        let a, b = workspace.Path "wrapped.pdf", workspace.Path "raw.pdf"
        wrapped workspace |> PdfFile.save a
        (raw workspace).Save b
        let a, b = File.ReadAllBytes a, File.ReadAllBytes b
        Expect.isTrue (normalizeIds a = normalizeIds b) $"{name}: files differ ({a.Length} and {b.Length} bytes)")

let private textsWith (password: string) (path: string) =
    use document =
        PdfDocument.Open (File.ReadAllBytes path, ParsingOptions (Password = password))

    [ for page in document.GetPages () -> page.Text ]

let private fileTests =
    testList
        "PdfFile"
        [ sameFile "load and save" (fun w -> PdfFile.load w.Source) (fun w -> DocumentOperation.LoadFile w.Source)
          sameFile
              "takePages"
              (fun w ->
                  PdfFile.load w.Source
                  |> PdfFile.takePages "r1,1-2")
              (fun w -> DocumentOperation.LoadFile(w.Source).TakePages "r1,1-2")
          sameFile "merge" (fun w -> PdfFile.load w.Source |> PdfFile.merge w.Stamp) (fun w -> DocumentOperation.LoadFile(w.Source).MergeFile w.Stamp)
          sameFile
              "mergePages"
              (fun w ->
                  PdfFile.load w.Source
                  |> PdfFile.mergePages w.Stamp "2")
              (fun w -> DocumentOperation.LoadFile(w.Source).MergeFile (w.Stamp, "2"))
          sameFile
              "overlay"
              (fun w ->
                  PdfFile.load w.Source
                  |> PdfFile.overlay
                      w.Stamp
                      [ PdfLayer.targetPages "2-4"
                        PdfLayer.sourcePages "1"
                        PdfLayer.repeatSourcePages "2" ])
              (fun w ->
                  DocumentOperation
                      .LoadFile(w.Source)
                      .OverlayFile (
                          DocumentOperation.LayerConfiguration (FilePath = w.Stamp, TargetPages = "2-4", SourcePages = "1", RepeatSourcePages = "2")
                      ))
          sameFile
              "underlay"
              (fun w ->
                  PdfFile.load w.Source
                  |> PdfFile.underlay w.Stamp [ PdfLayer.targetPages "1" ])
              (fun w ->
                  DocumentOperation.LoadFile(w.Source).UnderlayFile (DocumentOperation.LayerConfiguration (FilePath = w.Stamp, TargetPages = "1")))
          sameFile
              "extendMetadata"
              (fun w ->
                  PdfFile.load w.Source
                  |> PdfFile.extendMetadata "<dc:source>tests</dc:source>")
              (fun w -> DocumentOperation.LoadFile(w.Source).ExtendMetadata "<dc:source>tests</dc:source>")
          sameFile
              "attach"
              (fun w ->
                  PdfFile.load w.Source
                  |> PdfFile.attach
                      w.Notes
                      [ Attachment.key "k"
                        Attachment.name "notes.txt"
                        Attachment.description "the notes"
                        Attachment.mimeType "text/plain"
                        Attachment.created fixedDate.UtcDateTime
                        Attachment.modified fixedDate.UtcDateTime
                        Attachment.replace false
                        Attachment.relationship DocumentOperation.DocumentAttachmentRelationship.Source ])
              (fun w ->
                  DocumentOperation
                      .LoadFile(w.Source)
                      .AddAttachment (
                          DocumentOperation.DocumentAttachment (
                              FilePath = w.Notes,
                              Key = "k",
                              AttachmentName = "notes.txt",
                              Description = "the notes",
                              MimeType = "text/plain",
                              CreationDate = Nullable fixedDate.UtcDateTime,
                              ModificationDate = Nullable fixedDate.UtcDateTime,
                              Replace = false,
                              Relationship = Nullable DocumentOperation.DocumentAttachmentRelationship.Source
                          )
                      ))
          sameFile "linearize" (fun w -> PdfFile.load w.Source |> PdfFile.linearize) (fun w -> DocumentOperation.LoadFile(w.Source).Linearize ())
          sameFile
              "removeRestrictions"
              (fun w ->
                  PdfFile.load w.Source
                  |> PdfFile.removeRestrictions)
              (fun w -> DocumentOperation.LoadFile(w.Source).RemoveRestrictions ())
          test "the comparison of sameFile tells different page selections apart" {
              use w = new Workspace ()
              let first, second = w.Path "first.pdf", w.Path "second.pdf"

              PdfFile.load w.Source
              |> PdfFile.takePages "1"
              |> PdfFile.save first

              DocumentOperation.LoadFile(w.Source).TakePages("2").Save second
              let a, b = File.ReadAllBytes first, File.ReadAllBytes second
              Expect.isFalse (normalizeIds a = normalizeIds b) "different pages"
          }
          test "operations apply in pipeline order" {
              use w = new Workspace ()
              let output = w.Path "out.pdf"

              PdfFile.load w.Source
              |> PdfFile.takePages "3"
              |> PdfFile.merge w.Stamp
              |> PdfFile.save output

              Expect.equal (textsWith null output) [ "three 3"; "stamp 1"; "seal 2" ] "pages"
          }
          test "a PdfFile value is reusable" {
              use w = new Workspace ()
              let source = PdfFile.load w.Source

              source
              |> PdfFile.takePages "1"
              |> PdfFile.save (w.Path "first.pdf")

              source |> PdfFile.save (w.Path "all.pdf")
              Expect.equal (textsWith null (w.Path "first.pdf")) [ "one 1" ] "first"
              Expect.equal (textsWith null (w.Path "all.pdf")).Length 4 "all"
          }
          test "encrypt256 sets the passwords, and loadProtected with decrypt removes them" {
              use w = new Workspace ()
              let encrypted, decrypted = w.Path "encrypted.pdf", w.Path "decrypted.pdf"

              PdfFile.load w.Source
              |> PdfFile.encrypt256
                  [ Encryption.userPassword "user"
                    Encryption.ownerPassword "owner"
                    Encryption.allowPrinting false
                    Encryption.allowAssembly false
                    Encryption.encryptMetadata true ]
              |> PdfFile.save encrypted

              Expect.throws (fun () -> textsWith null encrypted |> ignore) "a password is required"
              Expect.equal (textsWith "user" encrypted).Length 4 "the user password opens the file"

              PdfFile.loadProtected "owner" encrypted
              |> PdfFile.decrypt
              |> PdfFile.save decrypted

              Expect.equal (textsWith null decrypted).Length 4 "the decrypted file opens without a password"
          }
          test "encrypt40 and encrypt128 take their permissions" {
              use w = new Workspace ()

              PdfFile.load w.Source
              |> PdfFile.encrypt40
                  [ Encryption.userPassword "user"
                    Encryption.ownerPassword "owner"
                    Encryption.allowAnnotation false
                    Encryption.allowContentExtraction false
                    Encryption.allowModification false
                    Encryption.allowPrinting false ]
              |> PdfFile.save (w.Path "40.pdf")

              PdfFile.load w.Source
              |> PdfFile.encrypt128
                  [ Encryption.userPassword "user"
                    Encryption.ownerPassword "owner"
                    Encryption.allowFillingForms false
                    Encryption.allowAssembly false
                    Encryption.encryptMetadata false ]
              |> PdfFile.save (w.Path "128.pdf")

              for name in [ "40.pdf"; "128.pdf" ] do
                  Expect.throws (fun () -> textsWith null (w.Path name) |> ignore) $"{name}: a password is required"
                  Expect.equal (textsWith "user" (w.Path name)).Length 4 $"{name}: the user password opens the file"
          }
          test "encryption parts set the properties of each encryption" {
              let e40 = DocumentOperation.Encryption40Bit ()
              let e256 = DocumentOperation.Encryption256Bit ()

              for part in [ Encryption.ownerPassword "o"; Encryption.allowModification false ] do
                  part e40

              for part in
                  [ Encryption.userPassword "u"
                    Encryption.allowFillingForms false
                    Encryption.allowContentExtraction false ] do
                  part e256

              Expect.equal (e40.OwnerPassword, e40.AllowModification, e40.AllowPrinting) ("o", false, true) "40-bit"

              Expect.equal
                  (e256.UserPassword, e256.AllowFillingForms, e256.AllowContentExtraction, e256.AllowAnnotation)
                  ("u", false, false, true)
                  "256-bit"
          } ]

[<Tests>]
let tests = testList "Operations" [ mergeTests; fileTests ]
