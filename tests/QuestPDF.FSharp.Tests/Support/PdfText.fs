/// Text and link extraction from generated PDFs (PdfPig).
[<AutoOpen>]
module QuestPDF.FSharp.Tests.Support.PdfText

open UglyToad.PdfPig

/// The text of each page, in page order.
let pageTexts (pdf: byte[]) : string list =
    use document = PdfDocument.Open pdf
    [ for page in document.GetPages () -> page.Text ]

let pageCount (pdf: byte[]) : int =
    use document = PdfDocument.Open pdf
    document.NumberOfPages

/// Every hyperlink as (1-based page number, link text, URI).
let hyperlinks (pdf: byte[]) : (int * string * string) list =
    use document = PdfDocument.Open pdf

    [ for page in document.GetPages () do
          for link in page.GetHyperlinks () -> page.Number, link.Text, link.Uri ]

/// The words of a 1-based page, in extraction order.
let wordsInOrder (pdf: byte[]) (page: int) : string list =
    use document = PdfDocument.Open pdf
    [ for word in document.GetPage(page).GetWords () -> word.Text ]

/// Every internal link as (1-based source page, 1-based destination page).
let internalLinks (pdf: byte[]) : (int * int) list =
    use document = PdfDocument.Open pdf

    [ for page in document.GetPages () do
          for annotation in page.GetAnnotations () do
              match annotation.Action with
              | :? Actions.GoToAction as goTo -> page.Number, goTo.Destination.PageNumber
              | _ -> () ]
