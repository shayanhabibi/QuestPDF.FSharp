namespace QuestPDF.FSharp

open QuestPDF.Fluent
open QuestPDF.Infrastructure

/// <summary>Binding helpers for the functions of the library.</summary>
[<AutoOpen>]
module internal Binding =
    /// <summary>
    /// The function itself. A module function whose body is <c>closure (fun ...)</c> compiles with its declared
    /// parameters only, so its reference page shows its declared return type.
    /// </summary>
    let closure (f: 'a -> 'b) : 'a -> 'b = f

/// <summary>A place in the layout that holds exactly one child element.</summary>
/// <remarks>Wraps a QuestPDF <see cref="T:QuestPDF.Infrastructure.IContainer"/>.</remarks>
[<Struct>]
type Slot = Slot of IContainer

/// <summary>Fills a slot. Compose modifiers in front of it with <c>&gt;&gt;</c>: <c>padding 10 &gt;&gt; text "x"</c>.</summary>
type Content = Slot -> unit

/// <summary>Wraps a slot, returning the inner slot. <c>&gt;&gt;</c> applies modifiers from the outside in.</summary>
type Modifier = Slot -> Slot

/// <summary>A text style within a style composition.</summary>
/// <remarks>Wraps a QuestPDF <see cref="T:QuestPDF.Infrastructure.TextStyle"/>.</remarks>
[<Struct>]
type Styled = Styled of TextStyle

/// <summary>A page setting or a page slot filling, listed in <c>page [ ... ]</c>.</summary>
type PagePart = PageDescriptor -> unit

/// <summary>An item or a setting of a row, listed in <c>row [ ... ]</c>.</summary>
type RowPart = RowDescriptor -> unit

/// <summary>A column definition, cell group or setting of a table, listed in <c>table [ ... ]</c>.</summary>
type TablePart = TableDescriptor -> unit

/// <summary>A table column, listed in <c>Table.columns [ ... ]</c>.</summary>
type ColumnDef = TableColumnsDefinitionDescriptor -> unit

/// <summary>A layer of a <c>layers [ ... ]</c> stack.</summary>
type LayerPart = LayersDescriptor -> unit

/// <summary>A slot of a <c>decoration [ ... ]</c>: the content, or what repeats before or after it on every page.</summary>
type DecorationPart = DecorationDescriptor -> unit

/// <summary>Content constructors and bridges from fluent QuestPDF code.</summary>
[<AutoOpen>]
module CoreOps =
    /// <summary>Leaves the slot empty.</summary>
    let empty: Content = ignore

    /// <summary>Content drawn by fluent QuestPDF code.</summary>
    let raw (draw: IContainer -> unit) : Content =
        closure (fun (Slot container) -> draw container)

    /// <summary>Content drawn by a fluent QuestPDF chain; the descriptor the chain returns is discarded.</summary>
    let fluent (draw: IContainer -> 'a) : Content =
        closure (fun (Slot container) -> draw container |> ignore)

    /// <summary>A modifier of a fluent QuestPDF container chain, such as <c>fun c -&gt; c.Padding(5f)</c>.</summary>
    let modify (wrap: IContainer -> IContainer) : Modifier =
        closure (fun (Slot container) -> Slot (wrap container))

/// <summary>Bridges from wrapper content to fluent QuestPDF code.</summary>
[<RequireQualifiedAccess>]
module Content =
    /// <summary>Draws wrapper content into a QuestPDF container.</summary>
    let run (content: Content) (container: IContainer) : unit =
        content (Slot container)

    /// <summary>Content drawn by a QuestPDF component.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ComponentExtensions.Component``1(QuestPDF.Infrastructure.IContainer,``0)"/>.</remarks>
    let ofComponent (source: IComponent) : Content =
        closure (fun (Slot container) -> container.Component source)
