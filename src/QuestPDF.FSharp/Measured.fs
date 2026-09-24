namespace QuestPDF.FSharp

open System
open System.ComponentModel
open QuestPDF.Fluent
open QuestPDF.Infrastructure

/// <summary>
/// The implementations behind the <c>inline</c> shims, taking a <see cref="T:QuestPDF.FSharp.Length"/> or a
/// <c>float</c> in place of a numeric argument. Callers with a <c>Length</c> available may use them directly.
/// </summary>
[<EditorBrowsable(EditorBrowsableState.Never)>]
[<RequireQualifiedAccess>]
module Measured =
    /// <summary>Implements <c>Style.size</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.FontSize(QuestPDF.Infrastructure.TextStyle,System.Single)"/>.</remarks>
    let fontSize (size: float) : Styled -> Styled =
        fun (Styled style) -> Styled (style.FontSize (float32 size))

    /// <summary>Implements <c>Style.lineHeight</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.LineHeight(QuestPDF.Infrastructure.TextStyle,System.Nullable{System.Single})"/>.</remarks>
    let lineHeight (factor: float) : Styled -> Styled =
        fun (Styled style) -> Styled (style.LineHeight (Nullable (float32 factor)))

    /// <summary>Implements <c>Style.decorationThickness</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.DecorationThickness(QuestPDF.Infrastructure.TextStyle,System.Single)"/>.</remarks>
    let decorationThickness (factor: float) : Styled -> Styled =
        fun (Styled style) -> Styled (style.DecorationThickness (float32 factor))

    /// <summary>Implements <c>Style.letterSpacing</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.LetterSpacing(QuestPDF.Infrastructure.TextStyle,System.Single)"/>.</remarks>
    let letterSpacing (factor: float) : Styled -> Styled =
        fun (Styled style) -> Styled (style.LetterSpacing (float32 factor))

    /// <summary>Implements <c>Style.wordSpacing</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.WordSpacing(QuestPDF.Infrastructure.TextStyle,System.Single)"/>.</remarks>
    let wordSpacing (factor: float) : Styled -> Styled =
        fun (Styled style) -> Styled (style.WordSpacing (float32 factor))

    /// <summary>Implements <c>padding</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.Padding(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let padding (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.Padding (length.Value, length.Unit))

    /// <summary>Implements <c>paddingV</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.PaddingVertical(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let paddingV (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.PaddingVertical (length.Value, length.Unit))

    /// <summary>Implements <c>paddingH</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.PaddingHorizontal(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let paddingH (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.PaddingHorizontal (length.Value, length.Unit))

    /// <summary>Implements <c>paddingTop</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.PaddingTop(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let paddingTop (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.PaddingTop (length.Value, length.Unit))

    /// <summary>Implements <c>paddingBottom</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.PaddingBottom(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let paddingBottom (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.PaddingBottom (length.Value, length.Unit))

    /// <summary>Implements <c>paddingLeft</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.PaddingLeft(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let paddingLeft (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.PaddingLeft (length.Value, length.Unit))

    /// <summary>Implements <c>paddingRight</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.PaddingRight(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let paddingRight (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.PaddingRight (length.Value, length.Unit))

    /// <summary>Implements <c>width</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ConstrainedExtensions.Width(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let width (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.Width (length.Value, length.Unit))

    /// <summary>Implements <c>minWidth</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ConstrainedExtensions.MinWidth(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let minWidth (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.MinWidth (length.Value, length.Unit))

    /// <summary>Implements <c>maxWidth</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ConstrainedExtensions.MaxWidth(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let maxWidth (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.MaxWidth (length.Value, length.Unit))

    /// <summary>Implements <c>height</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ConstrainedExtensions.Height(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let height (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.Height (length.Value, length.Unit))

    /// <summary>Implements <c>minHeight</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ConstrainedExtensions.MinHeight(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let minHeight (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.MinHeight (length.Value, length.Unit))

    /// <summary>Implements <c>maxHeight</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ConstrainedExtensions.MaxHeight(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let maxHeight (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.MaxHeight (length.Value, length.Unit))

    /// <summary>Implements <c>border</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.Border(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let border (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.Border (length.Value, length.Unit))

    /// <summary>Implements <c>borderV</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderVertical(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let borderV (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.BorderVertical (length.Value, length.Unit))

    /// <summary>Implements <c>borderH</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderHorizontal(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let borderH (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.BorderHorizontal (length.Value, length.Unit))

    /// <summary>Implements <c>borderTop</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderTop(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let borderTop (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.BorderTop (length.Value, length.Unit))

    /// <summary>Implements <c>borderBottom</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderBottom(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let borderBottom (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.BorderBottom (length.Value, length.Unit))

    /// <summary>Implements <c>borderLeft</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderLeft(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let borderLeft (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.BorderLeft (length.Value, length.Unit))

    /// <summary>Implements <c>borderRight</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderRight(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let borderRight (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.BorderRight (length.Value, length.Unit))

    /// <summary>Implements <c>cornerRadius</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.CornerRadius(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let cornerRadius (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.CornerRadius (length.Value, length.Unit))

    /// <summary>Implements <c>cornerRadiusTopLeft</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.CornerRadiusTopLeft(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let cornerRadiusTopLeft (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.CornerRadiusTopLeft (length.Value, length.Unit))

    /// <summary>Implements <c>cornerRadiusTopRight</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.CornerRadiusTopRight(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let cornerRadiusTopRight (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.CornerRadiusTopRight (length.Value, length.Unit))

    /// <summary>Implements <c>cornerRadiusBottomLeft</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.CornerRadiusBottomLeft(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let cornerRadiusBottomLeft (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.CornerRadiusBottomLeft (length.Value, length.Unit))

    /// <summary>Implements <c>cornerRadiusBottomRight</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.CornerRadiusBottomRight(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let cornerRadiusBottomRight (length: Length) : Modifier =
        fun (Slot container) -> Slot (container.CornerRadiusBottomRight (length.Value, length.Unit))

    /// <summary>Implements <c>aspectRatio</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.AspectRatio(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.AspectRatioOption)"/>.</remarks>
    let aspectRatio (ratio: float) : Modifier =
        fun (Slot container) -> Slot (container.AspectRatio (float32 ratio))

    /// <summary>Implements <c>columnSpaced</c>.</summary>
    /// <remarks>
    /// Maps to <see cref="M:QuestPDF.Fluent.ColumnDescriptor.Spacing(System.Single,QuestPDF.Infrastructure.Unit)"/>
    /// followed by <see cref="M:QuestPDF.Fluent.ColumnDescriptor.Item"/> per item.
    /// </remarks>
    let columnSpaced (spacing: Length) (items: Content list) : Content =
        fun (Slot container) ->
            container.Column (fun column ->
                column.Spacing (spacing.Value, spacing.Unit)

                for item in items do
                    item (Slot (column.Item ())))

    /// <summary>Implements <c>Row.relative</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.RowDescriptor.RelativeItem(System.Single)"/>.</remarks>
    let rowRelative (weight: float) (content: Content) : RowPart =
        fun row -> content (Slot (row.RelativeItem (float32 weight)))

    /// <summary>Implements <c>Row.constant</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.RowDescriptor.ConstantItem(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let rowConstant (width: Length) (content: Content) : RowPart =
        fun row -> content (Slot (row.ConstantItem (width.Value, width.Unit)))

    /// <summary>Implements <c>Row.spacing</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.RowDescriptor.Spacing(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let rowSpacing (spacing: Length) : RowPart =
        fun row -> row.Spacing (spacing.Value, spacing.Unit)

    /// <summary>Implements <c>Text.paragraphSpacing</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.ParagraphSpacing(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let textParagraphSpacing (spacing: Length) : TextDescriptor -> unit =
        fun descriptor -> descriptor.ParagraphSpacing (spacing.Value, spacing.Unit)

    /// <summary>Implements <c>Text.firstLineIndent</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextDescriptor.ParagraphFirstLineIndentation(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let textFirstLineIndent (indent: Length) : TextDescriptor -> unit =
        fun descriptor -> descriptor.ParagraphFirstLineIndentation (indent.Value, indent.Unit)

    /// <summary>Implements <c>Page.sizeOf</c>. Lengths in different units are both converted to points.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.Size(System.Single,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let pageSize (width: Length) (height: Length) : PagePart =
        fun page ->
            if width.Unit = height.Unit then
                page.Size (width.Value, height.Value, width.Unit)
            else
                page.Size (Length.points width, Length.points height, Unit.Point)

    /// <summary>Implements <c>Page.margin</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.Margin(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let pageMargin (length: Length) : PagePart =
        fun page -> page.Margin (length.Value, length.Unit)

    /// <summary>Implements <c>Page.marginV</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.MarginVertical(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let pageMarginV (length: Length) : PagePart =
        fun page -> page.MarginVertical (length.Value, length.Unit)

    /// <summary>Implements <c>Page.marginH</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.MarginHorizontal(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let pageMarginH (length: Length) : PagePart =
        fun page -> page.MarginHorizontal (length.Value, length.Unit)

    /// <summary>Implements <c>Page.marginTop</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.MarginTop(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let pageMarginTop (length: Length) : PagePart =
        fun page -> page.MarginTop (length.Value, length.Unit)

    /// <summary>Implements <c>Page.marginBottom</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.MarginBottom(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let pageMarginBottom (length: Length) : PagePart =
        fun page -> page.MarginBottom (length.Value, length.Unit)

    /// <summary>Implements <c>Page.marginLeft</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.MarginLeft(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let pageMarginLeft (length: Length) : PagePart =
        fun page -> page.MarginLeft (length.Value, length.Unit)

    /// <summary>Implements <c>Page.marginRight</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PageDescriptor.MarginRight(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let pageMarginRight (length: Length) : PagePart =
        fun page -> page.MarginRight (length.Value, length.Unit)
