namespace QuestPDF.FSharp

open System
open QuestPDF.Fluent
open QuestPDF.Infrastructure

type internal DocumentPartKind =
    | PageDefinition of PagePart list
    | MetadataSetter of (DocumentMetadata -> unit)
    | SettingsSetter of (DocumentSettings -> unit)
    | MergeSetter of (MergedDocument -> MergedDocument)

/// <summary>
/// An element of a <c>document [ ... ]</c> list: a page, a metadata item or a settings item. The list of
/// <c>Pdf.merge</c> takes metadata items, settings items and <c>Merge.*</c> items.
/// </summary>
[<Sealed>]
type DocumentPart internal (kind: DocumentPartKind) =
    member internal _.Kind = kind

/// <summary>Documents and pages.</summary>
[<AutoOpen>]
module DocumentElements =
    /// <summary>A page definition. QuestPDF repeats it over as many pages as its content needs.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageExtensions.Page(QuestPDF.Infrastructure.IDocumentContainer,System.Action{QuestPDF.Fluent.PageDescriptor})"/>.</remarks>
    let page (parts: PagePart list) : DocumentPart =
        DocumentPart (PageDefinition parts)

    /// <summary>
    /// A document of pages, metadata and settings. The pages compose on every generation; metadata and
    /// settings are fixed when the document is built.
    /// </summary>
    /// <remarks>
    /// Maps to <see cref="M:QuestPDF.Fluent.Document.Create(System.Action{QuestPDF.Infrastructure.IDocumentContainer})"/>,
    /// <see cref="M:QuestPDF.Fluent.Document.WithMetadata(QuestPDF.Infrastructure.DocumentMetadata)"/> and
    /// <see cref="M:QuestPDF.Fluent.Document.WithSettings(QuestPDF.Infrastructure.DocumentSettings)"/>.
    /// Without <c>Meta.dated</c>, both dates are the time the document is built.
    /// </remarks>
    let document (parts: DocumentPart list) : Document =
        let metadata = DocumentMetadata ()
        let settings = DocumentSettings ()

        let pages =
            parts
            |> List.choose (fun part ->
                match part.Kind with
                | PageDefinition pageParts -> Some pageParts
                | MetadataSetter _
                | SettingsSetter _ -> None
                | MergeSetter _ -> invalidArg (nameof parts) "Merge.* items belong in the list of Pdf.merge.")

        for part in parts do
            match part.Kind with
            | MetadataSetter set -> set metadata
            | SettingsSetter set -> set settings
            | PageDefinition _
            | MergeSetter _ -> ()

        let rightToLeft = settings.ContentDirection = ContentDirection.RightToLeft

        Document
            .Create(fun container ->
                for pageParts in pages do
                    container.Page (fun descriptor ->
                        if rightToLeft then
                            descriptor.ContentFromRightToLeft ()

                        for part in pageParts do
                            part descriptor)
                    |> ignore)
            .WithMetadata(metadata)
            .WithSettings (settings)

/// <summary>Page settings and page slots, listed in <c>page [ ... ]</c>.</summary>
[<RequireQualifiedAccess>]
module Page =
    /// <summary>Sets the page size.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.Size(QuestPDF.Helpers.PageSize)"/>.</remarks>
    let size (size: PageSize) : PagePart =
        closure (fun page -> page.Size size)

    /// <summary>Sets the page width and height; each accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.Size(System.Single,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline sizeOf width height : PagePart =
        Measured.pageSize (len width) (len height)

    /// <summary>Sets the smallest page size; each page shrinks to its content down to this size.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.MinSize(QuestPDF.Helpers.PageSize)"/>.</remarks>
    let minSize (size: PageSize) : PagePart =
        closure (fun page -> page.MinSize size)

    /// <summary>Sets the largest page size; each page grows with its content up to this size.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.MaxSize(QuestPDF.Helpers.PageSize)"/>.</remarks>
    let maxSize (size: PageSize) : PagePart =
        closure (fun page -> page.MaxSize size)

    /// <summary>Makes a single page of a width, as tall as its content; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.ContinuousSize(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline continuous width : PagePart =
        Measured.pageContinuous (len width)

    /// <summary>Sets the margin on all sides; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.Margin(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline margin value : PagePart =
        Measured.pageMargin (len value)

    /// <summary>Sets the top and bottom margins; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.MarginVertical(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline marginV value : PagePart =
        Measured.pageMarginV (len value)

    /// <summary>Sets the left and right margins; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.MarginHorizontal(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline marginH value : PagePart =
        Measured.pageMarginH (len value)

    /// <summary>Sets the top margin; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.MarginTop(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline marginTop value : PagePart =
        Measured.pageMarginTop (len value)

    /// <summary>Sets the bottom margin; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.MarginBottom(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline marginBottom value : PagePart =
        Measured.pageMarginBottom (len value)

    /// <summary>Sets the left margin; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.MarginLeft(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline marginLeft value : PagePart =
        Measured.pageMarginLeft (len value)

    /// <summary>Sets the right margin; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.MarginRight(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline marginRight value : PagePart =
        Measured.pageMarginRight (len value)

    /// <summary>Sets the page colour.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.PageColor(QuestPDF.Infrastructure.Color)"/>.</remarks>
    let color (color: Color) : PagePart =
        closure (fun page -> page.PageColor color)

    /// <summary>Sets the default text style of the page.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.DefaultTextStyle(System.Func{QuestPDF.Infrastructure.TextStyle,QuestPDF.Infrastructure.TextStyle})"/>.</remarks>
    let textStyle (style: Style) : PagePart =
        closure (fun page -> page.DefaultTextStyle (Func<TextStyle, TextStyle> (Style.apply style)))

    /// <summary>Lays out content from right to left.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.ContentFromRightToLeft"/>.</remarks>
    let rightToLeft: PagePart = closure (fun page -> page.ContentFromRightToLeft ())

    /// <summary>Lays out content from left to right, overriding <c>Output.rightToLeft</c> for this page.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.ContentFromLeftToRight"/>.</remarks>
    let leftToRight: PagePart = closure (fun page -> page.ContentFromLeftToRight ())

    /// <summary>Fills the header, repeated at the top of every page.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.Header"/>.</remarks>
    let header (content: Content) : PagePart =
        closure (fun page -> content (Slot (page.Header ())))

    /// <summary>Fills the main content, which flows across pages. A page has one content slot.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.Content"/>.</remarks>
    let content (content: Content) : PagePart =
        closure (fun page -> content (Slot (page.Content ())))

    /// <summary>Fills the footer, repeated at the bottom of every page.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.Footer"/>.</remarks>
    let footer (content: Content) : PagePart =
        closure (fun page -> content (Slot (page.Footer ())))

    /// <summary>Fills a layer behind the whole page, margins included.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.Background"/>.</remarks>
    let background (content: Content) : PagePart =
        closure (fun page -> content (Slot (page.Background ())))

    /// <summary>Fills a layer over the whole page, margins included.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.Foreground"/>.</remarks>
    let foreground (content: Content) : PagePart =
        closure (fun page -> content (Slot (page.Foreground ())))

/// <summary>Document metadata, listed in <c>document [ ... ]</c>.</summary>
/// <remarks>Each item sets a property of <see cref="T:QuestPDF.Infrastructure.DocumentMetadata"/>.</remarks>
[<RequireQualifiedAccess>]
module Meta =
    let private set (apply: DocumentMetadata -> unit) =
        DocumentPart (MetadataSetter apply)

    /// <summary>Sets the title.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Infrastructure.DocumentMetadata.Title"/>.</remarks>
    let title (value: string) : DocumentPart =
        set (fun metadata -> metadata.Title <- value)

    /// <summary>Sets the author.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Infrastructure.DocumentMetadata.Author"/>.</remarks>
    let author (value: string) : DocumentPart =
        set (fun metadata -> metadata.Author <- value)

    /// <summary>Sets the subject.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Infrastructure.DocumentMetadata.Subject"/>.</remarks>
    let subject (value: string) : DocumentPart =
        set (fun metadata -> metadata.Subject <- value)

    /// <summary>Sets the keywords.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Infrastructure.DocumentMetadata.Keywords"/>.</remarks>
    let keywords (value: string) : DocumentPart =
        set (fun metadata -> metadata.Keywords <- value)

    /// <summary>Sets the creator application.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Infrastructure.DocumentMetadata.Creator"/>.</remarks>
    let creator (value: string) : DocumentPart =
        set (fun metadata -> metadata.Creator <- value)

    /// <summary>Sets the producer application.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Infrastructure.DocumentMetadata.Producer"/>.</remarks>
    let producer (value: string) : DocumentPart =
        set (fun metadata -> metadata.Producer <- value)

    /// <summary>Sets the language, as a tag such as <c>en-US</c>.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Infrastructure.DocumentMetadata.Language"/>.</remarks>
    let language (value: string) : DocumentPart =
        set (fun metadata -> metadata.Language <- value)

    /// <summary>Sets the creation date.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Infrastructure.DocumentMetadata.CreationDate"/>.</remarks>
    let created (date: DateTimeOffset) : DocumentPart =
        set (fun metadata -> metadata.CreationDate <- date)

    /// <summary>Sets the modification date.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Infrastructure.DocumentMetadata.ModifiedDate"/>.</remarks>
    let modified (date: DateTimeOffset) : DocumentPart =
        set (fun metadata -> metadata.ModifiedDate <- date)

    /// <summary>Sets the creation and modification dates. Documents with the same content and date generate identical bytes.</summary>
    /// <remarks>
    /// Sets <see cref="P:QuestPDF.Infrastructure.DocumentMetadata.CreationDate"/> and
    /// <see cref="P:QuestPDF.Infrastructure.DocumentMetadata.ModifiedDate"/>.
    /// </remarks>
    let dated (date: DateTimeOffset) : DocumentPart =
        set (fun metadata ->
            metadata.CreationDate <- date
            metadata.ModifiedDate <- date)

/// <summary>Document settings, listed in <c>document [ ... ]</c>. A later item overrides an earlier one.</summary>
/// <remarks>Each item sets a property of <see cref="T:QuestPDF.Infrastructure.DocumentSettings"/>.</remarks>
[<RequireQualifiedAccess>]
module Output =
    let private set (apply: DocumentSettings -> unit) =
        DocumentPart (SettingsSetter apply)

    /// <summary>Generates a PDF/A file of a conformance level, for example <c>PDFA_Conformance.PDFA_3B</c>.</summary>
    /// <remarks>
    /// Sets <see cref="P:QuestPDF.Infrastructure.DocumentSettings.PDFA_Conformance"/>. Two generations differ in the
    /// bytes of a random document ID.
    /// </remarks>
    let pdfA (conformance: PDFA_Conformance) : DocumentPart =
        set (fun settings -> settings.PDFA_Conformance <- conformance)

    /// <summary>Generates a PDF/UA-1 (accessible) file.</summary>
    /// <remarks>
    /// Sets <see cref="P:QuestPDF.Infrastructure.DocumentSettings.PDFUA_Conformance"/> to
    /// <see cref="F:QuestPDF.Infrastructure.PDFUA_Conformance.PDFUA_1"/>. Two generations differ in the bytes of a
    /// random document ID.
    /// </remarks>
    let pdfUA: DocumentPart =
        set (fun settings -> settings.PDFUA_Conformance <- PDFUA_Conformance.PDFUA_1)

    /// <summary>Turns compression of the PDF file on or off. Compression is on by default.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Infrastructure.DocumentSettings.CompressDocument"/>.</remarks>
    let compress (enabled: bool) : DocumentPart =
        set (fun settings -> settings.CompressDocument <- enabled)

    /// <summary>Sets the default compression quality of embedded images. The default is <c>High</c>.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Infrastructure.DocumentSettings.ImageCompressionQuality"/>.</remarks>
    let imageQuality (quality: ImageCompressionQuality) : DocumentPart =
        set (fun settings -> settings.ImageCompressionQuality <- quality)

    /// <summary>Sets the default resolution, in dots per inch, of embedded images. The default is 288.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Infrastructure.DocumentSettings.ImageRasterDpi"/>.</remarks>
    let imageDpi (dpi: int) : DocumentPart =
        set (fun settings -> settings.ImageRasterDpi <- dpi)

    /// <summary>
    /// Lays out every page of the document from right to left. <c>Page.leftToRight</c> restores left to right for
    /// one page.
    /// </summary>
    /// <remarks>
    /// Sets <see cref="P:QuestPDF.Infrastructure.DocumentSettings.ContentDirection"/> and calls
    /// <see cref="M:QuestPDF.Fluent.PageDescriptor.ContentFromRightToLeft"/> on each page before its parts.
    /// </remarks>
    let rightToLeft: DocumentPart =
        set (fun settings -> settings.ContentDirection <- ContentDirection.RightToLeft)
