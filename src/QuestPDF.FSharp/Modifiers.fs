namespace QuestPDF.FSharp

open System
open QuestPDF.Fluent
open QuestPDF.Infrastructure

/// <summary>Modifiers: each wraps a slot and passes the inner slot on to the next modifier or content.</summary>
[<AutoOpen>]
module Modifiers =
    /// <summary>Pads the content on all sides; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.Padding(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline padding value : Modifier =
        Measured.padding (len value)

    /// <summary>Pads the content above and below; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.PaddingVertical(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline paddingV value : Modifier =
        Measured.paddingV (len value)

    /// <summary>Pads the content left and right; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.PaddingHorizontal(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline paddingH value : Modifier =
        Measured.paddingH (len value)

    /// <summary>Pads the content above; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.PaddingTop(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline paddingTop value : Modifier =
        Measured.paddingTop (len value)

    /// <summary>Pads the content below; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.PaddingBottom(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline paddingBottom value : Modifier =
        Measured.paddingBottom (len value)

    /// <summary>Pads the content on the left; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.PaddingLeft(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline paddingLeft value : Modifier =
        Measured.paddingLeft (len value)

    /// <summary>Pads the content on the right; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.PaddingRight(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline paddingRight value : Modifier =
        Measured.paddingRight (len value)

    /// <summary>Fills the area behind the content with a colour.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.Background(QuestPDF.Infrastructure.IContainer,QuestPDF.Infrastructure.Color)"/>.</remarks>
    let background (color: Color) : Modifier =
        fun (Slot container) -> Slot (container.Background color)

    /// <summary>Sets the default text style of every text in the content.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.DefaultTextStyle(QuestPDF.Infrastructure.IContainer,System.Func{QuestPDF.Infrastructure.TextStyle,QuestPDF.Infrastructure.TextStyle})"/>.</remarks>
    let textStyle (style: Style) : Modifier =
        fun (Slot container) -> Slot (container.DefaultTextStyle (Func<TextStyle, TextStyle> (Style.apply style)))

    /// <summary>Places the content at the left edge, at its own width.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.AlignmentExtensions.AlignLeft(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let alignLeft: Modifier = fun (Slot container) -> Slot (container.AlignLeft ())

    /// <summary>Centres the content horizontally, at its own width.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.AlignmentExtensions.AlignCenter(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let alignCenter: Modifier = fun (Slot container) -> Slot (container.AlignCenter ())

    /// <summary>Places the content at the right edge, at its own width.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.AlignmentExtensions.AlignRight(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let alignRight: Modifier = fun (Slot container) -> Slot (container.AlignRight ())

    /// <summary>Places the content at the top edge, at its own height.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.AlignmentExtensions.AlignTop(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let alignTop: Modifier = fun (Slot container) -> Slot (container.AlignTop ())

    /// <summary>Centres the content vertically, at its own height.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.AlignmentExtensions.AlignMiddle(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let alignMiddle: Modifier = fun (Slot container) -> Slot (container.AlignMiddle ())

    /// <summary>Places the content at the bottom edge, at its own height.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.AlignmentExtensions.AlignBottom(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let alignBottom: Modifier = fun (Slot container) -> Slot (container.AlignBottom ())

    /// <summary>Stretches the content over all the available space.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ExtendExtensions.Extend(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let extend: Modifier = fun (Slot container) -> Slot (container.Extend ())

    /// <summary>Stretches the content over the available width.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ExtendExtensions.ExtendHorizontal(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let extendH: Modifier = fun (Slot container) -> Slot (container.ExtendHorizontal ())

    /// <summary>Stretches the content over the available height.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ExtendExtensions.ExtendVertical(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let extendV: Modifier = fun (Slot container) -> Slot (container.ExtendVertical ())

    /// <summary>Sizes the content to its own width and height.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ShrinkExtensions.Shrink(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let shrink: Modifier = fun (Slot container) -> Slot (container.Shrink ())

    /// <summary>Sizes the content to its own width.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ShrinkExtensions.ShrinkHorizontal(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let shrinkH: Modifier = fun (Slot container) -> Slot (container.ShrinkHorizontal ())

    /// <summary>Sizes the content to its own height.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ShrinkExtensions.ShrinkVertical(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let shrinkV: Modifier = fun (Slot container) -> Slot (container.ShrinkVertical ())

    /// <summary>Lays the content out free of size limits, at zero size in the parent.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.Unconstrained(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let unconstrained: Modifier =
        fun (Slot container) -> Slot (container.Unconstrained ())

    /// <summary>Scales the content down until it fits the available space.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.ScaleToFit(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let scaleToFit: Modifier = fun (Slot container) -> Slot (container.ScaleToFit ())

    /// <summary>Draws the border inside the edge of the box.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderAlignmentInside(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let borderInside: Modifier =
        fun (Slot container) -> Slot (container.BorderAlignmentInside ())

    /// <summary>Centres the border on the edge of the box.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderAlignmentMiddle(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let borderMiddle: Modifier =
        fun (Slot container) -> Slot (container.BorderAlignmentMiddle ())

    /// <summary>Draws the border outside the edge of the box.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderAlignmentOutside(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let borderOutside: Modifier =
        fun (Slot container) -> Slot (container.BorderAlignmentOutside ())

    /// <summary>Keeps the content on one page, moving it to the next page when the rest of the current page is too short.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.ShowEntire(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let showEntire: Modifier = fun (Slot container) -> Slot (container.ShowEntire ())

    /// <summary>Sets the exact width; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ConstrainedExtensions.Width(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline width value : Modifier =
        Measured.width (len value)

    /// <summary>Sets the minimum width; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ConstrainedExtensions.MinWidth(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline minWidth value : Modifier =
        Measured.minWidth (len value)

    /// <summary>Sets the maximum width; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ConstrainedExtensions.MaxWidth(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline maxWidth value : Modifier =
        Measured.maxWidth (len value)

    /// <summary>Sets the exact height; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ConstrainedExtensions.Height(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline height value : Modifier =
        Measured.height (len value)

    /// <summary>Sets the minimum height; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ConstrainedExtensions.MinHeight(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline minHeight value : Modifier =
        Measured.minHeight (len value)

    /// <summary>Sets the maximum height; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ConstrainedExtensions.MaxHeight(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline maxHeight value : Modifier =
        Measured.maxHeight (len value)

    /// <summary>Draws a border of the given thickness on all sides; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.Border(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline border value : Modifier =
        Measured.border (len value)

    /// <summary>Draws a border of the given thickness on the left and right; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderVertical(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline borderV value : Modifier =
        Measured.borderV (len value)

    /// <summary>Draws a border of the given thickness above and below; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderHorizontal(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline borderH value : Modifier =
        Measured.borderH (len value)

    /// <summary>Draws a border of the given thickness above; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderTop(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline borderTop value : Modifier =
        Measured.borderTop (len value)

    /// <summary>Draws a border of the given thickness below; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderBottom(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline borderBottom value : Modifier =
        Measured.borderBottom (len value)

    /// <summary>Draws a border of the given thickness on the left; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderLeft(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline borderLeft value : Modifier =
        Measured.borderLeft (len value)

    /// <summary>Draws a border of the given thickness on the right; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderRight(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline borderRight value : Modifier =
        Measured.borderRight (len value)

    /// <summary>Rounds every corner of the background and border; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.CornerRadius(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline cornerRadius value : Modifier =
        Measured.cornerRadius (len value)

    /// <summary>Rounds the top-left corner of the background and border; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.CornerRadiusTopLeft(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline cornerRadiusTopLeft value : Modifier =
        Measured.cornerRadiusTopLeft (len value)

    /// <summary>Rounds the top-right corner of the background and border; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.CornerRadiusTopRight(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline cornerRadiusTopRight value : Modifier =
        Measured.cornerRadiusTopRight (len value)

    /// <summary>Rounds the bottom-left corner of the background and border; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.CornerRadiusBottomLeft(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline cornerRadiusBottomLeft value : Modifier =
        Measured.cornerRadiusBottomLeft (len value)

    /// <summary>Rounds the bottom-right corner of the background and border; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.CornerRadiusBottomRight(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline cornerRadiusBottomRight value : Modifier =
        Measured.cornerRadiusBottomRight (len value)

    /// <summary>Sizes the content to a width-to-height ratio, fitting the available width; accepts int, int64, float, float32 or decimal.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.AspectRatio(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.AspectRatioOption)"/>.</remarks>
    let inline aspectRatio value : Modifier =
        Measured.aspectRatio (toFloatWith NumberWitness value)

    /// <summary>Sizes the content to a width-to-height ratio, fitting the available width, height or area.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.AspectRatio(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.AspectRatioOption)"/>.</remarks>
    let aspectRatioWith (option: AspectRatioOption) (ratio: float) : Modifier =
        fun (Slot container) -> Slot (container.AspectRatio (float32 ratio, option))

    /// <summary>Sets the border colour.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderColor(QuestPDF.Infrastructure.IContainer,QuestPDF.Infrastructure.Color)"/>.</remarks>
    let borderColor (color: Color) : Modifier =
        fun (Slot container) -> Slot (container.BorderColor color)

    /// <summary>Makes the content a link to a URL.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.Hyperlink(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let hyperlink (url: string) : Modifier =
        fun (Slot container) -> Slot (container.Hyperlink url)

    /// <summary>Marks the content as the named section, a target for <c>sectionLink</c> and the section page numbers.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.Section(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let section (name: string) : Modifier =
        fun (Slot container) -> Slot (container.Section name)

    /// <summary>Makes the content a link to the named section.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.SectionLink(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let sectionLink (name: string) : Modifier =
        fun (Slot container) -> Slot (container.SectionLink name)
