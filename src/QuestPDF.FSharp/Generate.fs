namespace QuestPDF.FSharp

open System
open System.IO
open QuestPDF.Fluent
open QuestPDF.Infrastructure

/// <summary>
/// PDF generation of any <see cref="T:QuestPDF.Infrastructure.IDocument"/>. Each function requires a license set by
/// <c>License.*</c> and otherwise raises <see cref="T:System.InvalidOperationException"/>.
/// </summary>
[<RequireQualifiedAccess>]
module Pdf =
    let private licensed (document: IDocument) =
        if not QuestPDF.Settings.License.HasValue then
            invalidOp "Call License.community () (or professional/enterprise) before generating."

        document

    /// <summary>The PDF file of a document.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.GenerateExtensions.GeneratePdf(QuestPDF.Infrastructure.IDocument)"/>.</remarks>
    let bytes (document: IDocument) : byte[] =
        (licensed document).GeneratePdf ()

    /// <summary>Writes the PDF file of a document to a path.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.GenerateExtensions.GeneratePdf(QuestPDF.Infrastructure.IDocument,System.String)"/>.</remarks>
    let save (path: string) (document: IDocument) : unit =
        (licensed document).GeneratePdf (path)

    /// <summary>Writes the PDF file of a document to a stream.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.GenerateExtensions.GeneratePdf(QuestPDF.Infrastructure.IDocument,System.IO.Stream)"/>.</remarks>
    let write (stream: Stream) (document: IDocument) : unit =
        (licensed document).GeneratePdf (stream)

    /// <summary>Writes the PDF file of a document to a temporary file and opens it in the default viewer.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.GenerateExtensions.GeneratePdfAndShow(QuestPDF.Infrastructure.IDocument)"/>.</remarks>
    let show (document: IDocument) : unit =
        (licensed document).GeneratePdfAndShow ()
