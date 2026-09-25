module QuestPDF.FSharp.Preview.Tests.FontFaceTests

open System.IO
open System.Text.RegularExpressions
open Expecto
open QuestPDF.FSharp
open QuestPDF.FSharp.PreviewServer
open QuestPDF.FSharp.Preview.Tests.Support

let private faceRules (css: string) =
    Regex.Matches(css, "@font-face").Count

[<Tests>]
let tests =
    testList
        "FontFace"
        [ testList
              "read"
              [ for file, weight, italic in
                    [ "Lato-Regular.ttf", 400, false
                      "Lato-Italic.ttf", 400, true
                      "Lato-SemiBold.ttf", 600, false
                      "Lato-Bold.ttf", 700, false
                      "Lato-BoldItalic.ttf", 700, true ] do
                    test file {
                        Expect.equal
                            (FontFace.readFile (repoFont file))
                            (Some
                                { Family = "Lato"
                                  Weight = weight
                                  Italic = italic })
                            "family, weight and italic"
                    }
                test "a text file is no font" { Expect.isNone (FontFace.readFile (repoFont "OFL.txt")) "OFL.txt" }
                test "truncated data is no font" { Expect.isNone (FontFace.read (File.ReadAllBytes(repoFont "Lato-Regular.ttf")[..200])) "200 bytes" } ]
          testList
              "css"
              [ test "one @font-face per distinct file" {
                    let file = repoFont "Lato-Regular.ttf"

                    let fonts =
                        FontFace.expand [ FontFile file; FontFile file; FontDirectory fontsDirectory ]

                    Expect.equal fonts.Length 5 "the five Lato files"
                    Expect.equal (faceRules (FontFace.css fonts)) 5 "@font-face rules"
                }
                test "a FontDirectory expands to its .ttf and .otf files" {
                    let directory = tempDirectory ()

                    try
                        File.Copy (repoFont "Lato-Regular.ttf", Path.Combine (directory, "a.ttf"))
                        File.Copy (repoFont "Lato-Bold.ttf", Path.Combine (directory, "b.otf"))
                        File.Copy (repoFont "Lato-Italic.ttf", Path.Combine (directory, "c.woff"))
                        let fonts = FontFace.expand [ FontDirectory directory ]

                        Expect.equal
                            (fonts |> List.map _.Content)
                            [ FromFile (Path.Combine (directory, "a.ttf"))
                              FromFile (Path.Combine (directory, "b.otf")) ]
                            "the .ttf and .otf files in name order"
                    finally
                        Directory.Delete (directory, true)
                }
                test "FontData is served by index" {
                    let data = File.ReadAllBytes (repoFont "Lato-Bold.ttf")

                    let fonts =
                        FontFace.expand [ FontFile (repoFont "Lato-Regular.ttf"); FontData data ]

                    Expect.equal fonts[1].Content (FromData data) "the data is the second font"
                    let css = FontFace.css fonts
                    Expect.stringContains css "url(/font/0)" "the file"
                    Expect.stringContains css "url(/font/1)" "the data"
                    Expect.stringContains css "font-weight:700" "the weight of the data"
                }
                test "a non-font file in a directory is skipped" {
                    let directory = tempDirectory ()

                    try
                        File.Copy (repoFont "Lato-Regular.ttf", Path.Combine (directory, "a.ttf"))
                        File.WriteAllText (Path.Combine (directory, "b.ttf"), "not a font")
                        File.WriteAllText (Path.Combine (directory, "notes.txt"), "not a font")
                        let fonts = FontFace.expand [ FontDirectory directory ]
                        Expect.equal (fonts |> List.map _.Content) [ FromFile (Path.Combine (directory, "a.ttf")) ] "fonts"
                    finally
                        Directory.Delete (directory, true)
                }
                test "a rule names the family, weight and style" {
                    let css =
                        FontFace.css (FontFace.expand [ FontFile (repoFont "Lato-BoldItalic.ttf") ])

                    Expect.equal css "@font-face{font-family:\"Lato\";font-weight:700;font-style:italic;src:url(/font/0);font-display:block}" "rule"
                } ] ]
