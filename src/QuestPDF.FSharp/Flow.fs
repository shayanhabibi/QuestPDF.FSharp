namespace QuestPDF.FSharp

open QuestPDF.Fluent

/// <summary>Flowing layout containers: inlined items that wrap into lines, and content split across columns.</summary>
[<AutoOpen>]
module Flow =
    /// <summary>Places the items in a line, in list order, wrapping to the next line when the width runs out; inlined parts also set the spacing and alignment.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.InlinedExtensions.Inlined(QuestPDF.Infrastructure.IContainer,System.Action{QuestPDF.Fluent.InlinedDescriptor})"/>.</remarks>
    let inlined (parts: InlinedPart list) : Content =
        closure (fun (Slot container) ->
            container.Inlined (fun inlined ->
                for part in parts do
                    part inlined))

    /// <summary>Flows the content through side-by-side columns, like a newspaper page; multi-column parts set the content, spacer and settings.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.MultiColumnExtensions.MultiColumn(QuestPDF.Infrastructure.IContainer,System.Action{QuestPDF.Fluent.MultiColumnDescriptor})"/>.</remarks>
    let multiColumn (parts: MultiColumnPart list) : Content =
        closure (fun (Slot container) ->
            container.MultiColumn (fun multiColumn ->
                for part in parts do
                    part multiColumn))

/// <summary>The items and settings of an <c>inlined</c> layout.</summary>
[<RequireQualifiedAccess>]
module Inlined =
    /// <summary>An item as wide as its content.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.InlinedDescriptor.Item"/>.</remarks>
    let item (content: Content) : InlinedPart =
        closure (fun inlined -> content (Slot (inlined.Item ())))

    /// <summary>Sets both the horizontal and the vertical gap between items; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.InlinedDescriptor.Spacing(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline spacing value : InlinedPart =
        Measured.inlinedSpacing (len value)

    /// <summary>Sets the gap between neighbouring items of a line; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.InlinedDescriptor.HorizontalSpacing(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline spacingH value : InlinedPart =
        Measured.inlinedSpacingH (len value)

    /// <summary>Sets the gap between lines; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.InlinedDescriptor.VerticalSpacing(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline spacingV value : InlinedPart =
        Measured.inlinedSpacingV (len value)

    /// <summary>Aligns the items of a line to their top edges.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.InlinedDescriptor.BaselineTop"/>.</remarks>
    let baselineTop: InlinedPart =
        closure (fun inlined -> inlined.BaselineTop ())

    /// <summary>Aligns the items of a line to their vertical centres.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.InlinedDescriptor.BaselineMiddle"/>.</remarks>
    let baselineMiddle: InlinedPart =
        closure (fun inlined -> inlined.BaselineMiddle ())

    /// <summary>Aligns the items of a line to their bottom edges.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.InlinedDescriptor.BaselineBottom"/>.</remarks>
    let baselineBottom: InlinedPart =
        closure (fun inlined -> inlined.BaselineBottom ())

    /// <summary>Places each line at the left edge.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.InlinedDescriptor.AlignLeft"/>.</remarks>
    let alignLeft: InlinedPart =
        closure (fun inlined -> inlined.AlignLeft ())

    /// <summary>Centres each line horizontally.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.InlinedDescriptor.AlignCenter"/>.</remarks>
    let alignCenter: InlinedPart =
        closure (fun inlined -> inlined.AlignCenter ())

    /// <summary>Places each line at the right edge.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.InlinedDescriptor.AlignRight"/>.</remarks>
    let alignRight: InlinedPart =
        closure (fun inlined -> inlined.AlignRight ())

    /// <summary>Spreads the items of each line evenly from the left edge to the right edge.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.InlinedDescriptor.AlignJustify"/>.</remarks>
    let alignJustify: InlinedPart =
        closure (fun inlined -> inlined.AlignJustify ())

    /// <summary>Spreads the items of each line with equal space between them and at both ends.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.InlinedDescriptor.AlignSpaceAround"/>.</remarks>
    let alignSpaceAround: InlinedPart =
        closure (fun inlined -> inlined.AlignSpaceAround ())

/// <summary>The content, spacer and settings of a <c>multiColumn</c> layout.</summary>
[<RequireQualifiedAccess>]
module MultiColumn =
    /// <summary>Fills the content that flows through the columns.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.MultiColumnDescriptor.Content"/>.</remarks>
    let content (content: Content) : MultiColumnPart =
        closure (fun multiColumn -> content (Slot (multiColumn.Content ())))

    /// <summary>Fills the gap between neighbouring columns, as tall as the columns and as wide as the spacing.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.MultiColumnDescriptor.Spacer"/>.</remarks>
    let spacer (content: Content) : MultiColumnPart =
        closure (fun multiColumn -> content (Slot (multiColumn.Spacer ())))

    /// <summary>Sets the number of columns. Without it, the layout has 2 columns.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.MultiColumnDescriptor.Columns(System.Int32)"/>.</remarks>
    let columns (count: int) : MultiColumnPart =
        closure (fun multiColumn -> multiColumn.Columns count)

    /// <summary>Sets the gap between neighbouring columns; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.MultiColumnDescriptor.Spacing(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline spacing value : MultiColumnPart =
        Measured.multiColumnSpacing (len value)

    /// <summary>Distributes the content so that the columns end at about the same height, in place of filling each column in turn.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.MultiColumnDescriptor.BalanceHeight(System.Boolean)"/>.</remarks>
    let balanceHeight: MultiColumnPart =
        closure (fun multiColumn -> multiColumn.BalanceHeight true)
