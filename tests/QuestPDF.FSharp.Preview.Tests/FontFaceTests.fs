module QuestPDF.FSharp.Preview.Tests.FontFaceTests

open System.IO
open System.Text.RegularExpressions
open System.Xml
open Expecto
open QuestPDF.FSharp
open QuestPDF.FSharp.PreviewServer
open QuestPDF.FSharp.Preview.Tests.Support

let private faceRules (css: string) =
    Regex.Matches(css, "@font-face").Count

/// The font URLs of the rules of a style sheet.
let private urls (css: string) =
    [ for m in Regex.Matches (css, @"url\(([^)]*)\)") -> m.Groups[1].Value ]

/// A served font of a family whose contents are not read.
let private named (family: string) : ServedFont =
    { Face =
        { Family = family
          Weight = 400
          Italic = false }
      Content = FromData [||]
      Hash = "0" }

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
                    Expect.stringContains css "url(/font/0?h=" "the file"
                    Expect.stringContains css "url(/font/1?h=" "the data"
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

                    Expect.isMatch
                        css
                        @"^@font-face\{font-family:""Lato"";font-weight:700;font-style:italic;src:url\(/font/0\?h=[0-9a-f]{16}\);font-display:block\}$"
                        "rule"
                }
                test "a font URL changes with the contents at its index" {
                    let directory = tempDirectory ()
                    let file = Path.Combine (directory, "a.ttf")

                    try
                        File.Copy (repoFont "Lato-Regular.ttf", file)
                        let regular = urls (FontFace.css (FontFace.expand [ FontDirectory directory ]))
                        let again = urls (FontFace.css (FontFace.expand [ FontDirectory directory ]))
                        File.Copy (repoFont "Lato-Bold.ttf", file, true)
                        let bold = urls (FontFace.css (FontFace.expand [ FontDirectory directory ]))
                        Expect.equal again regular "the same contents give the same URL"
                        Expect.notEqual bold regular "other contents at index 0 give another URL"
                    finally
                        Directory.Delete (directory, true)
                } ]
          testList
              "embed"
              [ for family in [ "A&Bc"; "a<b"; "x]]>y" ] do
                    test $"a family named {family} keeps the page well-formed" {
                        let css = FontFace.css [ named family ]

                        let svg =
                            FontFace.embed css "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\"><text>t</text></svg>"

                        let xml = XmlDocument ()
                        xml.LoadXml svg
                        let style = xml.DocumentElement.FirstChild
                        Expect.equal style.LocalName "style" "the first child"
                        Expect.equal style.InnerText css "the rules as text"
                    }
                test "no rules leave the page unchanged" { Expect.equal (FontFace.embed "" "<svg></svg>") "<svg></svg>" "unchanged" } ]
          testList
              "weights"
              [ test "the weights Skia writes one step low are raised to the weights of the text" {
                    let text (weight: string) =
                        $"<text font-size=\"12\" font-weight=\"{weight}\" font-family=\"Lato\">t</text>"

                    for written, meant in [ "400", "500"; "500", "600"; "600", "700"; "bold", "800"; "800", "900" ] do
                        Expect.equal (FontFace.weights $"<svg>{text written}</svg>") $"<svg>{text meant}</svg>" written
                }
                test "text without a weight and the light weights are kept" {
                    let svg =
                        "<svg><text font-family=\"Lato\">400 600 bold</text><text font-weight=\"300\">t</text></svg>"

                    Expect.equal (FontFace.weights svg) svg "unchanged"
                } ] ]
