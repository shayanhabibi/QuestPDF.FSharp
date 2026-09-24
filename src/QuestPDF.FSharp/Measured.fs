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
