// The script recipe: evaluate this file once in a SageFs REPL session (for example with #load "invoice.fsx"), open
// http://localhost:5800/ and edit this file or parts/header.fsx. Every save reloads the script and the page.
//
// Outside this repository, replace the three #r lines with:
// #r "nuget: FSharp.QuestPDF.Preview, 0.1.0"
#r "nuget: QuestPDF, 2026.9.0"
#r "../../src/FSharp.QuestPDF.Preview/bin/Release/net10.0/FSharp.QuestPDF.dll"
#r "../../src/FSharp.QuestPDF.Preview/bin/Release/net10.0/FSharp.QuestPDF.Preview.dll"
#load "parts/header.fsx"

open FSharp.QuestPDF

License.community ()
Font.useSystemFonts false
Font.registerDirectory (System.IO.Path.Combine (__SOURCE_DIRECTORY__, "../../fonts"))

let invoice () =
    document [
        page [
            Page.size PageSizes.A5
            Page.margin (1 * cm)
            Page.header (Header.title 1)
            Page.content (column [
                text "Thank you for your business."
                text "Payment is due within 30 days."
            ])
        ]
    ]

Preview.Live invoice
