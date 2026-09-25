namespace FSharp.QuestPDF

open System
open QuestPDF.Fluent
open QuestPDF.Infrastructure

/// <summary>
/// The settings a span inherits from the <c>Text.withStyle</c> and <c>Text.formatPage</c> calls around it, with the
/// styles outermost first.
/// </summary>
type internal SpanSettings =
    { Styles: Style list
      Format: PageNumberFormatter option }

/// <summary>An element of a <c>richText</c> block: a span, a page number, or a block setting.</summary>
/// <remarks>
/// A span carries the styles applied by <c>Text.withStyle</c>, and a page number also carries the formatter applied by
/// <c>Text.formatPage</c>; block settings ignore both.
/// </remarks>
[<Sealed>]
type TextPart internal (draw: SpanSettings -> TextDescriptor -> unit) =
    member internal _.Draw = draw

/// <summary>Text content.</summary>
[<AutoOpen>]
module TextElements =
    /// <summary>A text block in the inherited style.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextExtensions.Text(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let text (value: string) : Content =
        closure (fun (Slot container) -> container.Text value |> ignore)

    /// <summary>A text block in a style.</summary>
    /// <remarks>
    /// Maps to <see cref="M:QuestPDF.Fluent.TextExtensions.Text(QuestPDF.Infrastructure.IContainer,System.String)"/>
    /// followed by <c>Style</c> on the returned descriptor.
    /// </remarks>
    let styledText (style: Style) (value: string) : Content =
        closure (fun (Slot container) ->
            container.Text(value).Style (Style.toTextStyle style)
            |> ignore)

    /// <summary>A text block of spans, page numbers and block settings, in list order.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextExtensions.Text(QuestPDF.Infrastructure.IContainer,System.Action{QuestPDF.Fluent.TextDescriptor})"/>.</remarks>
    let richText (parts: TextPart list) : Content =
        closure (fun (Slot container) ->
            container.Text (fun descriptor ->
                for part in parts do
                    part.Draw { Styles = []; Format = None } descriptor))

/// <summary>The parts of a <c>richText</c> block.</summary>
[<RequireQualifiedAccess>]
module Text =
    let private applyStyle (settings: SpanSettings) (span: TextSpanDescriptor) =
        for style in settings.Styles do
            span.Style (Style.toTextStyle style) |> ignore

    let private spanOf (create: TextDescriptor -> TextSpanDescriptor) =
        TextPart (fun settings descriptor -> applyStyle settings (create descriptor))

    let private pageNumberOf (create: TextDescriptor -> TextPageNumberDescriptor) =
        TextPart (fun settings descriptor ->
            let pageNumber = create descriptor

            match settings.Format with
            | Some format -> pageNumber.Format format |> ignore
            | None -> ()

            applyStyle settings pageNumber)

    let private block (apply: TextDescriptor -> unit) =
        TextPart (fun _ descriptor -> apply descriptor)

    /// <summary>A span of text.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.Span(System.String)"/>.</remarks>
    let span (value: string) : TextPart =
        spanOf (fun descriptor -> descriptor.Span value)

    /// <summary>
    /// Applies a style to a span or a page number. With nested calls, the outer style applies first and the inner style
    /// is merged over it, as chained span calls are.
    /// </summary>
    /// <remarks>
    /// Maps to one <see cref="M:QuestPDF.Fluent.TextSpanDescriptorExtensions.Style``1(``0,QuestPDF.Infrastructure.TextStyle)"/>
    /// call per nested style on the span descriptor, outermost first.
    /// </remarks>
    let withStyle (style: Style) (part: TextPart) : TextPart =
        TextPart (fun settings descriptor ->
            part.Draw
                { settings with
                    Styles = settings.Styles @ [ style ] }
                descriptor)

    /// <summary>A span of text in a style; equal to <c>span value |&gt; withStyle style</c>.</summary>
    /// <remarks>
    /// Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.Span(System.String)"/> followed by
    /// <see cref="M:QuestPDF.Fluent.TextSpanDescriptorExtensions.Style``1(``0,QuestPDF.Infrastructure.TextStyle)"/>.
    /// </remarks>
    let styled (style: Style) (value: string) : TextPart =
        span value |> withStyle style

    /// <summary>The number of the current page.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.CurrentPageNumber"/>.</remarks>
    let pageNumber: TextPart =
        pageNumberOf (fun descriptor -> descriptor.CurrentPageNumber ())

    /// <summary>The number of pages in the document.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.TotalPages"/>.</remarks>
    let totalPages: TextPart = pageNumberOf (fun descriptor -> descriptor.TotalPages ())

    /// <summary>A line break within the paragraph.</summary>
    /// <remarks>A span of <c>"\n"</c>, the same span that <see cref="M:QuestPDF.Fluent.TextDescriptor.EmptyLine"/> adds.</remarks>
    let lineBreak: TextPart = span "\n"

    /// <summary>Sets the default style of every span in the block.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.DefaultTextStyle(System.Func{QuestPDF.Infrastructure.TextStyle,QuestPDF.Infrastructure.TextStyle})"/>.</remarks>
    let style (style: Style) : TextPart =
        block (fun descriptor -> descriptor.DefaultTextStyle (Func<TextStyle, TextStyle> (Style.apply style)))

    /// <summary>Aligns the block to the left.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.AlignLeft"/>.</remarks>
    let alignLeft: TextPart = block (fun descriptor -> descriptor.AlignLeft ())

    /// <summary>Centres the block.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.AlignCenter"/>.</remarks>
    let alignCenter: TextPart = block (fun descriptor -> descriptor.AlignCenter ())

    /// <summary>Aligns the block to the right.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.AlignRight"/>.</remarks>
    let alignRight: TextPart = block (fun descriptor -> descriptor.AlignRight ())

    /// <summary>Aligns the block to the start of the content direction.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.AlignStart"/>.</remarks>
    let alignStart: TextPart = block (fun descriptor -> descriptor.AlignStart ())

    /// <summary>Aligns the block to the end of the content direction.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.AlignEnd"/>.</remarks>
    let alignEnd: TextPart = block (fun descriptor -> descriptor.AlignEnd ())

    /// <summary>Justifies the block.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.Justify"/>.</remarks>
    let justify: TextPart = block (fun descriptor -> descriptor.Justify ())

    /// <summary>A span that links to a URL.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.Hyperlink(System.String,System.String)"/>.</remarks>
    let link (label: string) (url: string) : TextPart =
        spanOf (fun descriptor -> descriptor.Hyperlink (label, url))

    /// <summary>A span that links to a section marked with the <c>section</c> modifier.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.SectionLink(System.String,System.String)"/>.</remarks>
    let sectionLink (label: string) (section: string) : TextPart =
        spanOf (fun descriptor -> descriptor.SectionLink (label, section))

    /// <summary>A span followed by a line break.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.Line(System.String)"/>.</remarks>
    let line (value: string) : TextPart =
        spanOf (fun descriptor -> descriptor.Line value)

    /// <summary>A line break, equal to <c>lineBreak</c>; after a <c>line</c> or a line break it leaves a blank line.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.EmptyLine"/>.</remarks>
    let emptyLine: TextPart = spanOf (fun descriptor -> descriptor.EmptyLine ())

    /// <summary>The number of the current page, counted from the first page of a section.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.PageNumberWithinSection(System.String)"/>.</remarks>
    let sectionPageNumber (section: string) : TextPart =
        pageNumberOf (fun descriptor -> descriptor.PageNumberWithinSection section)

    /// <summary>The number of pages a section spans.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.TotalPagesWithinSection(System.String)"/>.</remarks>
    let sectionTotalPages (section: string) : TextPart =
        pageNumberOf (fun descriptor -> descriptor.TotalPagesWithinSection section)

    /// <summary>The number of the first page of a section.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.BeginPageNumberOfSection(System.String)"/>.</remarks>
    let sectionBeginPage (section: string) : TextPart =
        pageNumberOf (fun descriptor -> descriptor.BeginPageNumberOfSection section)

    /// <summary>The number of the last page of a section.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.EndPageNumberOfSection(System.String)"/>.</remarks>
    let sectionEndPage (section: string) : TextPart =
        pageNumberOf (fun descriptor -> descriptor.EndPageNumberOfSection section)

    /// <summary>
    /// Formats the page numbers in a part; the formatter receives <c>None</c> for an unknown number. Spans and block
    /// settings are unchanged. With nested calls, the inner formatter applies.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextPageNumberDescriptor.Format(QuestPDF.Fluent.PageNumberFormatter)"/>.</remarks>
    let formatPage (format: int option -> string) (part: TextPart) : TextPart =
        let formatter =
            PageNumberFormatter (fun number -> format (Option.ofNullable number))

        TextPart (fun settings descriptor ->
            part.Draw
                { settings with
                    Format = Some formatter }
                descriptor)

    /// <summary>Limits the block to a number of lines, ending the last line with "…".</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.ClampLines(System.Int32,System.String)"/>.</remarks>
    let clampLines (maxLines: int) : TextPart =
        block (fun descriptor -> descriptor.ClampLines maxLines)

    /// <summary>Limits the block to a number of lines, ending the last line with an ellipsis text.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.ClampLines(System.Int32,System.String)"/>.</remarks>
    let clampLinesWith (maxLines: int) (ellipsis: string) : TextPart =
        block (fun descriptor -> descriptor.ClampLines (maxLines, ellipsis))

    /// <summary>
    /// A part drawn by fluent QuestPDF code on the text descriptor, such as <c>fun t -&gt; t.Span("x").Italic() |&gt; ignore</c>.
    /// The part is drawn as written: <c>Text.withStyle</c> and <c>Text.formatPage</c> leave it unchanged, so style its
    /// spans in the fluent code.
    /// </summary>
    let raw (apply: TextDescriptor -> unit) : TextPart =
        block apply

    /// <summary>
    /// Content drawn inline within the text, with its bottom edge on the baseline. The content fits within one line;
    /// <c>Text.withStyle</c> leaves it unchanged, so style its text with <c>textStyle</c>.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.Element(System.Action{QuestPDF.Infrastructure.IContainer},QuestPDF.Infrastructure.TextInjectedElementAlignment)"/>.</remarks>
    let element (content: Content) : TextPart =
        block (fun descriptor -> descriptor.Element (fun container -> content (Slot container)))

    /// <summary>Content drawn inline within the text, aligned to the line by its alignment.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.Element(System.Action{QuestPDF.Infrastructure.IContainer},QuestPDF.Infrastructure.TextInjectedElementAlignment)"/>.</remarks>
    let elementWith (alignment: TextInjectedElementAlignment) (content: Content) : TextPart =
        block (fun descriptor -> descriptor.Element ((fun container -> content (Slot container)), alignment))

    /// <summary>Sets the space between paragraphs of the block; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.ParagraphSpacing(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline paragraphSpacing value : TextPart =
        raw (Measured.textParagraphSpacing (len value))

    /// <summary>Indents the first line of each paragraph of the block; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.ParagraphFirstLineIndentation(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline firstLineIndent value : TextPart =
        raw (Measured.textFirstLineIndent (len value))
