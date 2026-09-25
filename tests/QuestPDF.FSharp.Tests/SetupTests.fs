module QuestPDF.FSharp.Tests.SetupTests

open System
open System.IO
open Expecto
open QuestPDF.FSharp
open QuestPDF.FSharp.Tests.Support

/// A new empty directory under the temporary directory.
let private tempDirectory () =
    Directory.CreateDirectory(Path.Combine (Path.GetTempPath (), $"qpdf-setup-{Guid.NewGuid ():N}")).FullName

/// The number of registered fonts of a family.
let private countOf (family: string) =
    Font.registered ()
    |> List.filter (fun font -> font.FamilyName = family)
    |> List.length

let private isSource (source: FontSource) =
    Font.sources ()
    |> List.filter ((=) source)
    |> List.length

[<Tests>]
let tests =
    testList
        "Setup"
        [ test "registering a directory twice gives one source and one registration" {
              let directory = tempDirectory ()

              try
                  File.WriteAllBytes (Path.Combine (directory, "font.ttf"), renamedLato "Sd01")
                  Font.registerDirectory directory
                  Font.registerDirectory directory
                  Font.registerDirectory (directory + string Path.DirectorySeparatorChar)
                  Expect.equal (countOf "Sd01") 1 "registrations of the family"
                  Expect.equal (isSource (FontDirectory directory)) 1 "FontDirectory entries"
              finally
                  Directory.Delete (directory, true)
          }
          test "registering a file twice gives one source and one registration" {
              let directory = tempDirectory ()
              let path = Path.Combine (directory, "font.ttf")

              try
                  File.WriteAllBytes (path, renamedLato "Sf01")
                  Font.registerFile path
                  Font.registerFile path
                  Font.registerFile (Path.Combine (directory, ".", "font.ttf"))
                  Expect.equal (countOf "Sf01") 1 "registrations of the family"
                  Expect.equal (isSource (FontFile path)) 1 "FontFile entries"
              finally
                  Directory.Delete (directory, true)
          }
          test "registering equal bytes twice gives one source; different bytes give two" {
              let data = renamedLato "Sb01"
              Font.registerBytes data
              Font.registerBytes (Array.copy data)
              Expect.equal (countOf "Sb01") 1 "registrations of the family"
              Expect.equal (isSource (FontData data)) 1 "FontData entries of equal bytes"

              let other = renamedLato "Sb02"
              Font.registerBytes other

              let entries =
                  Font.sources ()
                  |> List.filter (fun source -> source = FontData data || source = FontData other)

              Expect.equal entries [ FontData data; FontData other ] "one entry per distinct content"
          }
          test "changing an array returned by sources leaves the registered bytes unchanged" {
              let data = renamedLato "Sm01"
              Font.registerBytes data

              match
                  Font.sources ()
                  |> List.tryFind ((=) (FontData data))
              with
              | Some (FontData returned) -> returned[0] <- returned[0] + 1uy
              | other -> failtest $"expected the FontData entry, got {other}"

              Expect.equal (isSource (FontData data)) 1 "the entry still holds the registered bytes"
          }
          test "sources keeps registration order" {
              let directory = tempDirectory ()

              let fontDirectory =
                  Directory.CreateDirectory(Path.Combine (directory, "fonts")).FullName

              let file = Path.Combine (directory, "font.ttf")

              try
                  File.WriteAllBytes (file, renamedLato "So01")
                  File.WriteAllBytes (Path.Combine (fontDirectory, "font.ttf"), renamedLato "So02")
                  let data = renamedLato "So03"
                  Font.registerFile file
                  Font.registerBytes data
                  Font.registerDirectory fontDirectory

                  let mine =
                      Font.sources ()
                      |> List.filter (fun source ->
                          source = FontFile file
                          || source = FontData data
                          || source = FontDirectory fontDirectory)

                  Expect.equal mine [ FontFile file; FontData data; FontDirectory fontDirectory ] "registration order"
              finally
                  Directory.Delete (directory, true)
          } ]
