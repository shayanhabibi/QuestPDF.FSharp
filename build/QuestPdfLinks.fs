/// Rewrites of the generated API reference, loaded by build.fsx and tested by the test project.
module QuestPDF.FSharp.Build.QuestPdfLinks

open System.Text.RegularExpressions

let private questPdfLink =
    Regex ("<a href=\"https://learn\\.microsoft\\.com/dotnet/api/questpdf\\.[^\"]*\">(.*?)</a>", RegexOptions.Singleline)

/// <summary>
/// The HTML with each link to a QuestPDF page of Microsoft Learn, the target fsdocs gives every QuestPDF cref, replaced
/// by its text in a <c>code</c> element.
/// </summary>
let unlink (html: string) : string =
    questPdfLink.Replace (html, "<code>$1</code>")
