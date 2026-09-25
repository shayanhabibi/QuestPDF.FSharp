namespace QuestPDF.FSharp

open System
open System.IO
open QuestPDF.Companion
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

    /// <summary>
    /// An image of each page, in page order, in a format and at a resolution in dots per inch. The settings of the
    /// document are unchanged afterwards.
    /// </summary>
    /// <remarks>
    /// Maps to <see cref="M:QuestPDF.Fluent.GenerateExtensions.GenerateImages(QuestPDF.Infrastructure.IDocument,QuestPDF.Infrastructure.ImageGenerationSettings)"/>,
    /// which sets <see cref="P:QuestPDF.Infrastructure.DocumentSettings.ImageRasterDpi"/> of the document to the
    /// image resolution; the previous value is restored.
    /// </remarks>
    let images (format: ImageFormat) (dpi: int) (document: IDocument) : byte[] list =
        let settings = (licensed document).GetSettings ()
        let rasterDpi = settings.ImageRasterDpi

        try
            document.GenerateImages (ImageGenerationSettings (ImageFormat = format, RasterDpi = dpi))
            |> List.ofSeq
        finally
            settings.ImageRasterDpi <- rasterDpi

    /// <summary>An SVG document of each page, in page order.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.GenerateExtensions.GenerateSvg(QuestPDF.Infrastructure.IDocument)"/>.</remarks>
    let svgs (document: IDocument) : string list =
        (licensed document).GenerateSvg () |> List.ofSeq

    /// <summary>
    /// Sends a document to the QuestPDF Companion app on its default port, 12500, for a live preview. The Companion
    /// app must be running. Blocks until the Companion app closes. While running, it sets
    /// <see cref="P:QuestPDF.Settings.EnableCaching"/> to false and
    /// <see cref="P:QuestPDF.Settings.EnableDetailedLayoutErrors"/> to true for the process.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Companion.CompanionExtensions.ShowInCompanion(QuestPDF.Infrastructure.IDocument,System.Int32)"/>.</remarks>
    let companion (document: IDocument) : unit =
        (licensed document).ShowInCompanion ()

    /// <summary>
    /// Sends a document to the QuestPDF Companion app on its default port, 12500, for a live preview; the
    /// asynchronous form of <c>Pdf.companion</c>. The computation completes when the Companion app closes and stops on
    /// cancellation.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Companion.CompanionExtensions.ShowInCompanionAsync(QuestPDF.Infrastructure.IDocument,System.Int32,System.Threading.CancellationToken)"/>.</remarks>
    let companionAsync (document: IDocument) : Async<unit> =
        async {
            let document = licensed document
            let! token = Async.CancellationToken

            do!
                document.ShowInCompanionAsync (12500, token)
                |> Async.AwaitTask
        }

    /// <summary>
    /// A document of the pages of several documents, in order, with the metadata, settings and <c>Merge.*</c>
    /// numbering items of a list. Without <c>Meta.dated</c>, both dates are the time the document is built.
    /// </summary>
    /// <remarks>
    /// Maps to <see cref="M:QuestPDF.Fluent.Document.Merge(System.Collections.Generic.IEnumerable{QuestPDF.Infrastructure.IDocument})"/>,
    /// <see cref="M:QuestPDF.Infrastructure.MergedDocument.WithMetadata(QuestPDF.Infrastructure.DocumentMetadata)"/> and
    /// <see cref="M:QuestPDF.Infrastructure.MergedDocument.WithSettings(QuestPDF.Infrastructure.DocumentSettings)"/>.
    /// <c>Output.rightToLeft</c> sets the content direction setting only; each page keeps the direction of its
    /// document. A <c>page</c> in the list raises <see cref="T:System.ArgumentException"/>.
    /// </remarks>
    let merge (parts: DocumentPart list) (documents: IDocument list) : MergedDocument =
        Merging.merge parts documents
