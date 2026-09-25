/// Top-level style compositions without type annotations. The file compiles only while composed styles are free of
/// the value restriction; StyleTests renders each value.
module FSharp.QuestPDF.Tests.StyleProbe

open QuestPDF.Helpers
open FSharp.QuestPDF

let muted = Style.color Colors.Grey.Darken1 >> Style.size 9

let boldItalic = Style.bold >> Style.italic

let sizedBold = Style.size 12 >> Style.bold

let boldSized = Style.bold >> Style.size 12

let spaced = Style.lineHeight 1.5 >> Style.size 11
