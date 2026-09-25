namespace FSharp.QuestPDF

open QuestPDF.Elements.Table
open QuestPDF.Fluent

/// <summary>Layout containers: columns, rows, tables, layers and decorations.</summary>
[<AutoOpen>]
module Layout =
    /// <summary>Stacks the items vertically, in list order.</summary>
    /// <remarks>
    /// Maps to <see cref="M:QuestPDF.Fluent.ColumnExtensions.Column(QuestPDF.Infrastructure.IContainer,System.Action{QuestPDF.Fluent.ColumnDescriptor})"/>
    /// with <see cref="M:QuestPDF.Fluent.ColumnDescriptor.Item"/> per item.
    /// </remarks>
    let column (items: Content list) : Content =
        closure (fun (Slot container) ->
            container.Column (fun column ->
                for item in items do
                    item (Slot (column.Item ()))))

    /// <summary>Stacks the items vertically with a gap between neighbours; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>
    /// Maps to <see cref="M:QuestPDF.Fluent.ColumnExtensions.Column(QuestPDF.Infrastructure.IContainer,System.Action{QuestPDF.Fluent.ColumnDescriptor})"/>
    /// with <see cref="M:QuestPDF.Fluent.ColumnDescriptor.Spacing(System.Single,QuestPDF.Infrastructure.Unit)"/>.
    /// </remarks>
    let inline columnSpaced spacing (items: Content list) : Content =
        Measured.columnSpaced (len spacing) items

    /// <summary>Places the items side by side, in list order; row parts also set the spacing.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.RowExtensions.Row(QuestPDF.Infrastructure.IContainer,System.Action{QuestPDF.Fluent.RowDescriptor})"/>.</remarks>
    let row (parts: RowPart list) : Content =
        closure (fun (Slot container) ->
            container.Row (fun row ->
                for part in parts do
                    part row))

    /// <summary>A grid of cells in defined columns, with an optional header and footer repeated on every page.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TableExtensions.Table(QuestPDF.Infrastructure.IContainer,System.Action{QuestPDF.Fluent.TableDescriptor})"/>.</remarks>
    let table (parts: TablePart list) : Content =
        closure (fun (Slot container) ->
            container.Table (fun table ->
                for part in parts do
                    part table))

    /// <summary>Stacks the layers on top of each other, in list order; the primary layer sets the size and paging.</summary>
    /// <remarks>
    /// Maps to <see cref="M:QuestPDF.Fluent.LayerExtensions.Layers(QuestPDF.Infrastructure.IContainer,System.Action{QuestPDF.Fluent.LayersDescriptor})"/>.
    /// Generation throws <see cref="T:QuestPDF.Drawing.Exceptions.DocumentComposeException"/> unless exactly one layer is primary.
    /// </remarks>
    let layers (parts: LayerPart list) : Content =
        closure (fun (Slot container) ->
            container.Layers (fun layers ->
                for part in parts do
                    part layers))

    /// <summary>Content framed by a part before and a part after it, both repeated on every page the content spans.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DecorationExtensions.Decoration(QuestPDF.Infrastructure.IContainer,System.Action{QuestPDF.Fluent.DecorationDescriptor})"/>.</remarks>
    let decoration (parts: DecorationPart list) : Content =
        closure (fun (Slot container) ->
            container.Decoration (fun decoration ->
                for part in parts do
                    part decoration))

/// <summary>The items and settings of a <c>row</c>.</summary>
[<RequireQualifiedAccess>]
module Row =
    /// <summary>An item sharing the remaining width with weight 1.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.RowDescriptor.RelativeItem(System.Single)"/>.</remarks>
    let fill (content: Content) : RowPart =
        closure (fun row -> content (Slot (row.RelativeItem ())))

    /// <summary>An item sharing the remaining width in proportion to a weight; accepts int, int64, float, float32 or decimal.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.RowDescriptor.RelativeItem(System.Single)"/>.</remarks>
    let inline relative weight (content: Content) : RowPart =
        Measured.rowRelative (toFloatWith NumberWitness weight) content

    /// <summary>An item of a fixed width; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.RowDescriptor.ConstantItem(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline constant width (content: Content) : RowPart =
        Measured.rowConstant (len width) content

    /// <summary>An item as wide as its content.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.RowDescriptor.AutoItem"/>.</remarks>
    let auto (content: Content) : RowPart =
        closure (fun row -> content (Slot (row.AutoItem ())))

    /// <summary>Sets the gap between neighbouring items; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.RowDescriptor.Spacing(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline spacing value : RowPart =
        Measured.rowSpacing (len value)

/// <summary>
/// The span, position or header role of a table cell, made by the <c>Cell</c> functions. Rows and columns are numbered from 1.
/// </summary>
[<RequireQualifiedAccess>]
type CellOption =
    /// <summary>The cell spans a number of columns.</summary>
    | ColumnSpan of int
    /// <summary>The cell spans a number of rows.</summary>
    | RowSpan of int
    /// <summary>The cell starts at a row and a column; other cells continue after it.</summary>
    | At of row: int * column: int
    /// <summary>The cell is a header of its row, for assistive technology.</summary>
    | HorizontalHeader

/// <summary>A table cell: its content and its span or position. One value can appear in the header, body and footer.</summary>
type TableCell =
    { Options: CellOption list
      Content: Content }

/// <summary>The columns, cell groups and settings of a <c>table</c>.</summary>
[<RequireQualifiedAccess>]
module Table =
    let private place (container: ITableCellContainer) (cell: TableCell) =
        let placed =
            (container, cell.Options)
            ||> List.fold (fun placed option ->
                match option with
                | CellOption.ColumnSpan count -> placed.ColumnSpan (uint32 count)
                | CellOption.RowSpan count -> placed.RowSpan (uint32 count)
                | CellOption.At (row, column) -> placed.Row(uint32 row).Column (uint32 column)
                | CellOption.HorizontalHeader -> placed.SemanticHorizontalHeader ())

        cell.Content (Slot placed)

    /// <summary>Defines the columns, in list order.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TableDescriptor.ColumnsDefinition(System.Action{QuestPDF.Fluent.TableColumnsDefinitionDescriptor})"/>.</remarks>
    let columns (definitions: ColumnDef list) : TablePart =
        closure (fun table ->
            table.ColumnsDefinition (fun columns ->
                for definition in definitions do
                    definition columns))

    /// <summary>A column sharing the remaining width in proportion to a weight; accepts int, int64, float, float32 or decimal.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TableColumnsDefinitionDescriptor.RelativeColumn(System.Single)"/>.</remarks>
    let inline relative weight : ColumnDef =
        Measured.tableRelative (toFloatWith NumberWitness weight)

    /// <summary>A column of a fixed width; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TableColumnsDefinitionDescriptor.ConstantColumn(System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline constant width : ColumnDef =
        Measured.tableConstant (len width)

    /// <summary>A cell that fills the next free place.</summary>
    let cell (content: Content) : TableCell =
        { Options = []; Content = content }

    /// <summary>A cell with a span or position.</summary>
    let cellWith (options: CellOption list) (content: Content) : TableCell =
        { Options = options; Content = content }

    /// <summary>The header cells, repeated at the top of the table on every page.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TableDescriptor.Header(System.Action{QuestPDF.Fluent.TableCellDescriptor})"/>.</remarks>
    let header (cells: TableCell list) : TablePart =
        closure (fun table ->
            table.Header (fun header ->
                for cell in cells do
                    place (header.Cell ()) cell))

    /// <summary>The footer cells, repeated at the bottom of the table on every page.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TableDescriptor.Footer(System.Action{QuestPDF.Fluent.TableCellDescriptor})"/>.</remarks>
    let footer (cells: TableCell list) : TablePart =
        closure (fun table ->
            table.Footer (fun footer ->
                for cell in cells do
                    place (footer.Cell ()) cell))

    /// <summary>Body cells, each placed in the next free place unless positioned with <c>Cell.at</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TableDescriptor.Cell"/> per cell.</remarks>
    let cells (cells: TableCell list) : TablePart =
        closure (fun table ->
            for cell in cells do
                place (table.Cell ()) cell)

    /// <summary>Stretches the cells of the last row to the bottom of the page.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TableDescriptor.ExtendLastCellsToTableBottom"/>.</remarks>
    let extendLastCellsToBottom: TablePart =
        closure (fun table -> table.ExtendLastCellsToTableBottom ())

/// <summary>The span, position and header role options of a table cell.</summary>
[<RequireQualifiedAccess>]
module Cell =
    /// <summary>Spans a number of columns.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TableCellExtensions.ColumnSpan(QuestPDF.Elements.Table.ITableCellContainer,System.UInt32)"/>.</remarks>
    let columnSpan (count: int) : CellOption =
        CellOption.ColumnSpan count

    /// <summary>Spans a number of rows.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TableCellExtensions.RowSpan(QuestPDF.Elements.Table.ITableCellContainer,System.UInt32)"/>.</remarks>
    let rowSpan (count: int) : CellOption =
        CellOption.RowSpan count

    /// <summary>Starts at a row and a column, both numbered from 1.</summary>
    /// <remarks>
    /// Maps to <see cref="M:QuestPDF.Fluent.TableCellExtensions.Row(QuestPDF.Elements.Table.ITableCellContainer,System.UInt32)"/>
    /// and <see cref="M:QuestPDF.Fluent.TableCellExtensions.Column(QuestPDF.Elements.Table.ITableCellContainer,System.UInt32)"/>.
    /// </remarks>
    let at (row: int) (column: int) : CellOption =
        CellOption.At (row, column)

    /// <summary>Tags the cell as the header of its row in a tagged PDF, such as a document with <c>Output.pdfUA</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TableCellExtensions.SemanticHorizontalHeader(QuestPDF.Elements.Table.ITableCellContainer)"/>.</remarks>
    let horizontalHeader: CellOption = CellOption.HorizontalHeader

/// <summary>The layers of a <c>layers</c> stack.</summary>
[<RequireQualifiedAccess>]
module Layers =
    /// <summary>A layer drawn over the layers before it, sized to the primary layer.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.LayersDescriptor.Layer"/>.</remarks>
    let layer (content: Content) : LayerPart =
        closure (fun layers -> content (Slot (layers.Layer ())))

    /// <summary>The layer that sets the size of the stack and pages with it.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.LayersDescriptor.PrimaryLayer"/>.</remarks>
    let primary (content: Content) : LayerPart =
        closure (fun layers -> content (Slot (layers.PrimaryLayer ())))

/// <summary>The slots of a <c>decoration</c>.</summary>
[<RequireQualifiedAccess>]
module Decoration =
    /// <summary>Fills the slot above the content, repeated on every page the content spans.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DecorationDescriptor.Before"/>.</remarks>
    let before (content: Content) : DecorationPart =
        closure (fun decoration -> content (Slot (decoration.Before ())))

    /// <summary>Fills the main slot, which flows across pages.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DecorationDescriptor.Content"/>.</remarks>
    let content (content: Content) : DecorationPart =
        closure (fun decoration -> content (Slot (decoration.Content ())))

    /// <summary>Fills the slot below the content, repeated on every page the content spans.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DecorationDescriptor.After"/>.</remarks>
    let after (content: Content) : DecorationPart =
        closure (fun decoration -> content (Slot (decoration.After ())))
