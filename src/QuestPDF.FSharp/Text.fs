namespace QuestPDF.FSharp

open System
open QuestPDF.Fluent
open QuestPDF.Infrastructure

/// <summary>An element of a <c>richText</c> block: a span, a page number, or a block setting.</summary>
/// <remarks>A span carries the styles applied by <c>Text.withStyle</c>; block settings ignore them.</remarks>
[<Sealed>]
type TextPart internal (draw: Style option -> TextDescriptor -> unit) =
    member internal _.Draw = draw

/// <summary>Text content.</summary>
[<AutoOpen>]
module TextElements =
    /// <summary>A text block in the inherited style.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextExtensions.Text(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let text (value: string) : Content =
        fun (Slot container) -> container.Text value |> ignore

    /// <summary>A text block in a style.</summary>
    /// <remarks>
    /// Maps to <see cref="M:QuestPDF.Fluent.TextExtensions.Text(QuestPDF.Infrastructure.IContainer,System.String)"/>
    /// followed by <c>Style</c> on the returned descriptor.
    /// </remarks>
    let styledText (style: Style) (value: string) : Content =
        fun (Slot container) ->
            container.Text(value).Style (Style.toTextStyle style)
            |> ignore

    /// <summary>A text block of spans, page numbers and block settings, in list order.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextExtensions.Text(QuestPDF.Infrastructure.IContainer,System.Action{QuestPDF.Fluent.TextDescriptor})"/>.</remarks>
    let richText (parts: TextPart list) : Content =
        fun (Slot container) ->
            container.Text (fun descriptor ->
                for part in parts do
                    part.Draw None descriptor)

/// <summary>The parts of a <c>richText</c> block.</summary>
[<RequireQualifiedAccess>]
module Text =
    let private applyStyle (style: Style option) (span: TextSpanDescriptor) =
        match style with
        | Some style -> span.Style (Style.toTextStyle style) |> ignore
        | None -> ()

    let private block (apply: TextDescriptor -> unit) =
        TextPart (fun _ descriptor -> apply descriptor)

    /// <summary>A span of text.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.Span(System.String)"/>.</remarks>
    let span (value: string) : TextPart =
        TextPart (fun style descriptor -> applyStyle style (descriptor.Span value))

    /// <summary>Applies a style to a span or a page number. With nested calls, the outer style applies first and the inner style overrides it.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextSpanDescriptorExtensions.Style``1(``0,QuestPDF.Infrastructure.TextStyle)"/> on the span descriptor.</remarks>
    let withStyle (style: Style) (part: TextPart) : TextPart =
        TextPart (fun outer descriptor ->
            let combined =
                match outer with
                | Some outer -> outer >> style
                | None -> style

            part.Draw (Some combined) descriptor)

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
        TextPart (fun style descriptor -> applyStyle style (descriptor.CurrentPageNumber ()))

    /// <summary>The number of pages in the document.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.TotalPages"/>.</remarks>
    let totalPages: TextPart =
        TextPart (fun style descriptor -> applyStyle style (descriptor.TotalPages ()))

    /// <summary>A line break within the paragraph.</summary>
    /// <remarks>A span of <c>"\n"</c>. <see cref="M:QuestPDF.Fluent.TextDescriptor.EmptyLine"/> adds a blank line instead.</remarks>
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
