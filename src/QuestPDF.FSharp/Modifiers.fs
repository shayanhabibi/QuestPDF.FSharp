namespace QuestPDF.FSharp

open System
open QuestPDF.Elements
open QuestPDF.Fluent
open QuestPDF.Infrastructure

/// <summary>Modifiers: each wraps a slot and passes the inner slot on to the next modifier or content.</summary>
[<AutoOpen>]
module Modifiers =
    /// <summary>Pads the content on all sides; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.Padding(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline padding value : Modifier =
        Measured.padding (len value)

    /// <summary>Pads the content above and below; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.PaddingVertical(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline paddingV value : Modifier =
        Measured.paddingV (len value)

    /// <summary>Pads the content left and right; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.PaddingHorizontal(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline paddingH value : Modifier =
        Measured.paddingH (len value)

    /// <summary>Pads the content above; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.PaddingTop(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline paddingTop value : Modifier =
        Measured.paddingTop (len value)

    /// <summary>Pads the content below; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.PaddingBottom(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline paddingBottom value : Modifier =
        Measured.paddingBottom (len value)

    /// <summary>Pads the content on the left; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.PaddingLeft(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline paddingLeft value : Modifier =
        Measured.paddingLeft (len value)

    /// <summary>Pads the content on the right; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.PaddingExtensions.PaddingRight(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline paddingRight value : Modifier =
        Measured.paddingRight (len value)

    /// <summary>Fills the area behind the content with a colour.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.Background(QuestPDF.Infrastructure.IContainer,QuestPDF.Infrastructure.Color)"/>.</remarks>
    let background (color: Color) : Modifier =
        closure (fun (Slot container) -> Slot (container.Background color))

    /// <summary>Sets the default text style of every text in the content.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.DefaultTextStyle(QuestPDF.Infrastructure.IContainer,System.Func{QuestPDF.Infrastructure.TextStyle,QuestPDF.Infrastructure.TextStyle})"/>.</remarks>
    let textStyle (style: Style) : Modifier =
        closure (fun (Slot container) -> Slot (container.DefaultTextStyle (Func<TextStyle, TextStyle> (Style.apply style))))

    /// <summary>Places the content at the left edge, at its own width.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.AlignmentExtensions.AlignLeft(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let alignLeft: Modifier =
        closure (fun (Slot container) -> Slot (container.AlignLeft ()))

    /// <summary>Centres the content horizontally, at its own width.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.AlignmentExtensions.AlignCenter(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let alignCenter: Modifier =
        closure (fun (Slot container) -> Slot (container.AlignCenter ()))

    /// <summary>Places the content at the right edge, at its own width.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.AlignmentExtensions.AlignRight(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let alignRight: Modifier =
        closure (fun (Slot container) -> Slot (container.AlignRight ()))

    /// <summary>Places the content at the top edge, at its own height.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.AlignmentExtensions.AlignTop(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let alignTop: Modifier =
        closure (fun (Slot container) -> Slot (container.AlignTop ()))

    /// <summary>Centres the content vertically, at its own height.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.AlignmentExtensions.AlignMiddle(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let alignMiddle: Modifier =
        closure (fun (Slot container) -> Slot (container.AlignMiddle ()))

    /// <summary>Places the content at the bottom edge, at its own height.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.AlignmentExtensions.AlignBottom(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let alignBottom: Modifier =
        closure (fun (Slot container) -> Slot (container.AlignBottom ()))

    /// <summary>Stretches the content over all the available space.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ExtendExtensions.Extend(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let extend: Modifier = closure (fun (Slot container) -> Slot (container.Extend ()))

    /// <summary>Stretches the content over the available width.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ExtendExtensions.ExtendHorizontal(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let extendH: Modifier =
        closure (fun (Slot container) -> Slot (container.ExtendHorizontal ()))

    /// <summary>Stretches the content over the available height.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ExtendExtensions.ExtendVertical(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let extendV: Modifier =
        closure (fun (Slot container) -> Slot (container.ExtendVertical ()))

    /// <summary>Sizes the content to its own width and height.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ShrinkExtensions.Shrink(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let shrink: Modifier = closure (fun (Slot container) -> Slot (container.Shrink ()))

    /// <summary>Sizes the content to its own width.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ShrinkExtensions.ShrinkHorizontal(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let shrinkH: Modifier =
        closure (fun (Slot container) -> Slot (container.ShrinkHorizontal ()))

    /// <summary>Sizes the content to its own height.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ShrinkExtensions.ShrinkVertical(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let shrinkV: Modifier =
        closure (fun (Slot container) -> Slot (container.ShrinkVertical ()))

    /// <summary>Lays the content out free of size limits, at zero size in the parent.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.Unconstrained(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let unconstrained: Modifier =
        closure (fun (Slot container) -> Slot (container.Unconstrained ()))

    /// <summary>Scales the content down until it fits the available space.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.ScaleToFit(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let scaleToFit: Modifier =
        closure (fun (Slot container) -> Slot (container.ScaleToFit ()))

    /// <summary>Draws the border inside the edge of the box.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderAlignmentInside(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let borderInside: Modifier =
        closure (fun (Slot container) -> Slot (container.BorderAlignmentInside ()))

    /// <summary>Centres the border on the edge of the box.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderAlignmentMiddle(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let borderMiddle: Modifier =
        closure (fun (Slot container) -> Slot (container.BorderAlignmentMiddle ()))

    /// <summary>Draws the border outside the edge of the box.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderAlignmentOutside(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let borderOutside: Modifier =
        closure (fun (Slot container) -> Slot (container.BorderAlignmentOutside ()))

    /// <summary>Keeps the content on one page, moving it to the next page when the rest of the current page is too short.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.ShowEntire(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let showEntire: Modifier =
        closure (fun (Slot container) -> Slot (container.ShowEntire ()))

    /// <summary>Sets the exact width; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ConstrainedExtensions.Width(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline width value : Modifier =
        Measured.width (len value)

    /// <summary>Sets the minimum width; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ConstrainedExtensions.MinWidth(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline minWidth value : Modifier =
        Measured.minWidth (len value)

    /// <summary>Sets the maximum width; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ConstrainedExtensions.MaxWidth(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline maxWidth value : Modifier =
        Measured.maxWidth (len value)

    /// <summary>Sets the exact height; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ConstrainedExtensions.Height(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline height value : Modifier =
        Measured.height (len value)

    /// <summary>Sets the minimum height; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ConstrainedExtensions.MinHeight(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline minHeight value : Modifier =
        Measured.minHeight (len value)

    /// <summary>Sets the maximum height; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ConstrainedExtensions.MaxHeight(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline maxHeight value : Modifier =
        Measured.maxHeight (len value)

    /// <summary>Draws a border of the given thickness on all sides; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.Border(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline border value : Modifier =
        Measured.border (len value)

    /// <summary>Draws a border of the given thickness on the left and right; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderVertical(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline borderV value : Modifier =
        Measured.borderV (len value)

    /// <summary>Draws a border of the given thickness above and below; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderHorizontal(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline borderH value : Modifier =
        Measured.borderH (len value)

    /// <summary>Draws a border of the given thickness above; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderTop(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline borderTop value : Modifier =
        Measured.borderTop (len value)

    /// <summary>Draws a border of the given thickness below; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderBottom(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline borderBottom value : Modifier =
        Measured.borderBottom (len value)

    /// <summary>Draws a border of the given thickness on the left; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderLeft(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline borderLeft value : Modifier =
        Measured.borderLeft (len value)

    /// <summary>Draws a border of the given thickness on the right; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderRight(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline borderRight value : Modifier =
        Measured.borderRight (len value)

    /// <summary>Rounds every corner of the background and border; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.CornerRadius(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline cornerRadius value : Modifier =
        Measured.cornerRadius (len value)

    /// <summary>Rounds the top-left corner of the background and border; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.CornerRadiusTopLeft(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline cornerRadiusTopLeft value : Modifier =
        Measured.cornerRadiusTopLeft (len value)

    /// <summary>Rounds the top-right corner of the background and border; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.CornerRadiusTopRight(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline cornerRadiusTopRight value : Modifier =
        Measured.cornerRadiusTopRight (len value)

    /// <summary>Rounds the bottom-left corner of the background and border; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.CornerRadiusBottomLeft(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline cornerRadiusBottomLeft value : Modifier =
        Measured.cornerRadiusBottomLeft (len value)

    /// <summary>Rounds the bottom-right corner of the background and border; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
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
        closure (fun (Slot container) -> Slot (container.AspectRatio (float32 ratio, option)))

    /// <summary>Sets the border colour.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderColor(QuestPDF.Infrastructure.IContainer,QuestPDF.Infrastructure.Color)"/>.</remarks>
    let borderColor (color: Color) : Modifier =
        closure (fun (Slot container) -> Slot (container.BorderColor color))

    /// <summary>Makes the content a link to a URL.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.Hyperlink(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let hyperlink (url: string) : Modifier =
        closure (fun (Slot container) -> Slot (container.Hyperlink url))

    /// <summary>Marks the content as the named section, a target for <c>sectionLink</c> and the section page numbers.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.Section(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let section (name: string) : Modifier =
        closure (fun (Slot container) -> Slot (container.Section name))

    /// <summary>Makes the content a link to the named section.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.SectionLink(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let sectionLink (name: string) : Modifier =
        closure (fun (Slot container) -> Slot (container.SectionLink name))

    /// <summary>Moves the content to the next page when it would otherwise break across the current one; taller content still pages.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.PreventPageBreak(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let preventPageBreak: Modifier =
        closure (fun (Slot container) -> Slot (container.PreventPageBreak ()))

    /// <summary>Draws the content on the first page its parent occupies only, such as the first page of a repeated header.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.ShowOnce(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let showOnce: Modifier =
        closure (fun (Slot container) -> Slot (container.ShowOnce ()))

    /// <summary>Hides the content on the first page its parent occupies and draws it on the following pages.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.SkipOnce(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let skipOnce: Modifier =
        closure (fun (Slot container) -> Slot (container.SkipOnce ()))

    /// <summary>Draws the content again on every page its parent occupies, after the content is complete.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.Repeat(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let repeat: Modifier = closure (fun (Slot container) -> Slot (container.Repeat ()))

    /// <summary>Draws the part of the content that fits on the current page and drops the rest.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.StopPaging(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let stopPaging: Modifier =
        closure (fun (Slot container) -> Slot (container.StopPaging ()))

    /// <summary>
    /// Moves content that would break across pages to the next page when less than a height remains on the current page.
    /// Content that fits whole in the remaining space stays on the current page. The height accepts int, int64, float,
    /// float32 or decimal (points) or Length.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.EnsureSpace(QuestPDF.Infrastructure.IContainer,System.Single)"/>.</remarks>
    let inline ensureSpace minHeight : Modifier =
        Measured.ensureSpace (len minHeight)

    /// <summary>Draws the content when the condition is true.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.ShowIf(QuestPDF.Infrastructure.IContainer,System.Boolean)"/>.</remarks>
    let showIf (condition: bool) : Modifier =
        closure (fun (Slot container) -> Slot (container.ShowIf condition))

    /// <summary>
    /// Draws the content on the pages where the predicate is true. The predicate receives the page number, and the total
    /// page count once it is known.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.ShowIf(QuestPDF.Infrastructure.IContainer,System.Predicate{QuestPDF.Elements.ShowIfContext})"/>.</remarks>
    let showWhen (predicate: ShowIfContext -> bool) : Modifier =
        closure (fun (Slot container) -> Slot (container.ShowIf (Predicate predicate)))

    /// <summary>Rotates the drawing of the content clockwise by an angle in degrees, around its top-left corner; accepts int, int64, float, float32 or decimal.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.RotateExtensions.Rotate(QuestPDF.Infrastructure.IContainer,System.Single)"/>.</remarks>
    let inline rotate degrees : Modifier =
        Measured.rotate (toFloatWith NumberWitness degrees)

    /// <summary>Turns the content a quarter turn clockwise, swapping the available width and height.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.RotateExtensions.RotateLayoutClockwise(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let rotateLayoutCw: Modifier =
        closure (fun (Slot container) -> Slot (container.RotateLayoutClockwise ()))

    /// <summary>Turns the content a quarter turn counterclockwise, swapping the available width and height.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.RotateExtensions.RotateLayoutCounterclockwise(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let rotateLayoutCcw: Modifier =
        closure (fun (Slot container) -> Slot (container.RotateLayoutCounterclockwise ()))

    /// <summary>Scales the content in both directions by a factor; accepts int, int64, float, float32 or decimal.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ScaleExtensions.Scale(QuestPDF.Infrastructure.IContainer,System.Single)"/>.</remarks>
    let inline scale factor : Modifier =
        Measured.scale (toFloatWith NumberWitness factor)

    /// <summary>Scales the content horizontally by a factor; accepts int, int64, float, float32 or decimal.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ScaleExtensions.ScaleHorizontal(QuestPDF.Infrastructure.IContainer,System.Single)"/>.</remarks>
    let inline scaleH factor : Modifier =
        Measured.scaleH (toFloatWith NumberWitness factor)

    /// <summary>Scales the content vertically by a factor; accepts int, int64, float, float32 or decimal.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ScaleExtensions.ScaleVertical(QuestPDF.Infrastructure.IContainer,System.Single)"/>.</remarks>
    let inline scaleV factor : Modifier =
        Measured.scaleV (toFloatWith NumberWitness factor)

    /// <summary>Mirrors the content left to right.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ScaleExtensions.FlipHorizontal(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let flipH: Modifier =
        closure (fun (Slot container) -> Slot (container.FlipHorizontal ()))

    /// <summary>Mirrors the content top to bottom.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ScaleExtensions.FlipVertical(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let flipV: Modifier =
        closure (fun (Slot container) -> Slot (container.FlipVertical ()))

    /// <summary>Mirrors the content in both directions, a half turn.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ScaleExtensions.FlipOver(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let flipOver: Modifier =
        closure (fun (Slot container) -> Slot (container.FlipOver ()))

    /// <summary>Shifts the drawing of the content to the right, leaving the layout unchanged; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.OffsetExtensions.OffsetX(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline offsetX value : Modifier =
        Measured.offsetX (len value)

    /// <summary>Shifts the drawing of the content down, leaving the layout unchanged; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.OffsetExtensions.OffsetY(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline offsetY value : Modifier =
        Measured.offsetY (len value)

    /// <summary>Sets the drawing order among overlapping content: a higher index is drawn later, on top.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.ZIndex(QuestPDF.Infrastructure.IContainer,System.Int32)"/>.</remarks>
    let zIndex (index: int) : Modifier =
        closure (fun (Slot container) -> Slot (container.ZIndex index))

    /// <summary>Lays out the content from left to right.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ContentDirectionExtensions.ContentFromLeftToRight(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let contentLtr: Modifier =
        closure (fun (Slot container) -> Slot (container.ContentFromLeftToRight ()))

    /// <summary>Lays out the content from right to left.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ContentDirectionExtensions.ContentFromRightToLeft(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let contentRtl: Modifier =
        closure (fun (Slot container) -> Slot (container.ContentFromRightToLeft ()))

    /// <summary>Outlines the content area with a labelled debug frame.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DebugExtensions.DebugArea(QuestPDF.Infrastructure.IContainer,System.String,System.Nullable{QuestPDF.Infrastructure.Color})"/>.</remarks>
    let debugArea (label: string) : Modifier =
        closure (fun (Slot container) -> Slot (container.DebugArea label))
