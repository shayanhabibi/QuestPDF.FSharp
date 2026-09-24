namespace QuestPDF.FSharp

open QuestPDF.Fluent

/// <summary>Layout containers: columns and rows.</summary>
[<AutoOpen>]
module Layout =
    /// <summary>Stacks the items vertically, in list order.</summary>
    /// <remarks>
    /// Maps to <see cref="M:QuestPDF.Fluent.ColumnExtensions.Column(QuestPDF.Infrastructure.IContainer,System.Action{QuestPDF.Fluent.ColumnDescriptor})"/>
    /// with <see cref="M:QuestPDF.Fluent.ColumnDescriptor.Item"/> per item.
    /// </remarks>
    let column (items: Content list) : Content =
        fun (Slot container) ->
            container.Column (fun column ->
                for item in items do
                    item (Slot (column.Item ())))

    /// <summary>Stacks the items vertically with a gap between neighbours; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>
    /// Maps to <see cref="M:QuestPDF.Fluent.ColumnExtensions.Column(QuestPDF.Infrastructure.IContainer,System.Action{QuestPDF.Fluent.ColumnDescriptor})"/>
    /// with <see cref="M:QuestPDF.Fluent.ColumnDescriptor.Spacing(System.Single,QuestPDF.Infrastructure.Unit)"/>.
    /// </remarks>
    let inline columnSpaced spacing (items: Content list) : Content =
        Measured.columnSpaced (len spacing) items

    /// <summary>Places the items side by side, in list order; row parts also set the spacing.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.RowExtensions.Row(QuestPDF.Infrastructure.IContainer,System.Action{QuestPDF.Fluent.RowDescriptor})"/>.</remarks>
    let row (parts: RowPart list) : Content =
        fun (Slot container) ->
            container.Row (fun row ->
                for part in parts do
                    part row)

/// <summary>The items and settings of a <c>row</c>.</summary>
[<RequireQualifiedAccess>]
module Row =
    /// <summary>An item sharing the remaining width with weight 1.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.RowDescriptor.RelativeItem(System.Single)"/>.</remarks>
    let fill (content: Content) : RowPart =
        fun row -> content (Slot (row.RelativeItem ()))

    /// <summary>An item sharing the remaining width in proportion to a weight; accepts int, int64, float, float32 or decimal.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.RowDescriptor.RelativeItem(System.Single)"/>.</remarks>
    let inline relative weight (content: Content) : RowPart =
        Measured.rowRelative (toFloatWith NumberWitness weight) content

    /// <summary>An item of a fixed width; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.RowDescriptor.ConstantItem(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline constant width (content: Content) : RowPart =
        Measured.rowConstant (len width) content

    /// <summary>An item as wide as its content.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.RowDescriptor.AutoItem"/>.</remarks>
    let auto (content: Content) : RowPart =
        fun row -> content (Slot (row.AutoItem ()))

    /// <summary>Sets the gap between neighbouring items; accepts int, float, float32 (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.RowDescriptor.Spacing(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline spacing value : RowPart =
        Measured.rowSpacing (len value)
