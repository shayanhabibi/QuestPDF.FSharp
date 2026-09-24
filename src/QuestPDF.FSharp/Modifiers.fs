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
