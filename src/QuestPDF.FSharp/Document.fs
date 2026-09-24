namespace QuestPDF.FSharp

open System
open QuestPDF.Fluent
open QuestPDF.Infrastructure

type internal DocumentPartKind =
    | PageDefinition of PagePart list
    | MetadataSetter of (DocumentMetadata -> unit)
    | SettingsSetter of (DocumentSettings -> unit)

/// <summary>An element of a <c>document [ ... ]</c> list: a page, a metadata item or a settings item.</summary>
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
                | SettingsSetter _ -> None)

        for part in parts do
            match part.Kind with
            | MetadataSetter set -> set metadata
            | SettingsSetter set -> set settings
            | PageDefinition _ -> ()

        Document
            .Create(fun container ->
                for pageParts in pages do
                    container.Page (fun descriptor ->
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
        fun page -> page.Size size

    /// <summary>Sets the page width and height; each accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.Size(System.Single,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline sizeOf width height : PagePart =
        Measured.pageSize (len width) (len height)

    /// <summary>Sets the margin on all sides; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.Margin(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline margin value : PagePart =
        Measured.pageMargin (len value)

    /// <summary>Sets the top and bottom margins; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.MarginVertical(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline marginV value : PagePart =
        Measured.pageMarginV (len value)

    /// <summary>Sets the left and right margins; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.MarginHorizontal(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline marginH value : PagePart =
        Measured.pageMarginH (len value)

    /// <summary>Sets the top margin; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.MarginTop(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline marginTop value : PagePart =
        Measured.pageMarginTop (len value)

    /// <summary>Sets the bottom margin; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.MarginBottom(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline marginBottom value : PagePart =
        Measured.pageMarginBottom (len value)

    /// <summary>Sets the left margin; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.MarginLeft(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline marginLeft value : PagePart =
        Measured.pageMarginLeft (len value)

    /// <summary>Sets the right margin; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.MarginRight(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline marginRight value : PagePart =
        Measured.pageMarginRight (len value)

    /// <summary>Sets the page colour.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.PageColor(QuestPDF.Infrastructure.Color)"/>.</remarks>
    let color (color: Color) : PagePart =
        fun page -> page.PageColor color

    /// <summary>Sets the default text style of the page.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.DefaultTextStyle(System.Func{QuestPDF.Infrastructure.TextStyle,QuestPDF.Infrastructure.TextStyle})"/>.</remarks>
    let textStyle (style: Style) : PagePart =
        fun page -> page.DefaultTextStyle (Func<TextStyle, TextStyle> style)

    /// <summary>Lays out content from right to left.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.ContentFromRightToLeft"/>.</remarks>
    let rightToLeft: PagePart = fun page -> page.ContentFromRightToLeft ()

    /// <summary>Fills the header, repeated at the top of every page.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.Header"/>.</remarks>
    let header (content: Content) : PagePart =
        fun page -> content (Slot (page.Header ()))

    /// <summary>Fills the main content, which flows across pages. A page has one content slot.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.Content"/>.</remarks>
    let content (content: Content) : PagePart =
        fun page -> content (Slot (page.Content ()))

    /// <summary>Fills the footer, repeated at the bottom of every page.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.Footer"/>.</remarks>
    let footer (content: Content) : PagePart =
        fun page -> content (Slot (page.Footer ()))

    /// <summary>Fills a layer behind the whole page, margins included.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.Background"/>.</remarks>
    let background (content: Content) : PagePart =
        fun page -> content (Slot (page.Background ()))

    /// <summary>Fills a layer over the whole page, margins included.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.Foreground"/>.</remarks>
    let foreground (content: Content) : PagePart =
        fun page -> content (Slot (page.Foreground ()))

/// <summary>Document metadata, listed in <c>document [ ... ]</c>.</summary>
/// <remarks>Each item sets a property of <see cref="T:QuestPDF.Infrastructure.DocumentMetadata"/>.</remarks>
[<RequireQualifiedAccess>]
module Meta =
    let private set (apply: DocumentMetadata -> unit) =
        DocumentPart (MetadataSetter apply)

    /// <summary>Sets the title.</summary>
    let title (value: string) : DocumentPart =
        set (fun metadata -> metadata.Title <- value)

    /// <summary>Sets the author.</summary>
    let author (value: string) : DocumentPart =
        set (fun metadata -> metadata.Author <- value)

    /// <summary>Sets the subject.</summary>
    let subject (value: string) : DocumentPart =
        set (fun metadata -> metadata.Subject <- value)

    /// <summary>Sets the keywords.</summary>
    let keywords (value: string) : DocumentPart =
        set (fun metadata -> metadata.Keywords <- value)

    /// <summary>Sets the creator application.</summary>
    let creator (value: string) : DocumentPart =
        set (fun metadata -> metadata.Creator <- value)

    /// <summary>Sets the producer application.</summary>
    let producer (value: string) : DocumentPart =
        set (fun metadata -> metadata.Producer <- value)

    /// <summary>Sets the language, as a tag such as <c>en-US</c>.</summary>
    let language (value: string) : DocumentPart =
        set (fun metadata -> metadata.Language <- value)

    /// <summary>Sets the creation date.</summary>
    let created (date: DateTimeOffset) : DocumentPart =
        set (fun metadata -> metadata.CreationDate <- date)

    /// <summary>Sets the modification date.</summary>
    let modified (date: DateTimeOffset) : DocumentPart =
        set (fun metadata -> metadata.ModifiedDate <- date)

    /// <summary>Sets the creation and modification dates. Documents with the same content and date generate identical bytes.</summary>
    let dated (date: DateTimeOffset) : DocumentPart =
        set (fun metadata ->
            metadata.CreationDate <- date
            metadata.ModifiedDate <- date)
