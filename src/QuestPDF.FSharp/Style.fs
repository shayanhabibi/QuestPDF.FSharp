namespace QuestPDF.FSharp

open QuestPDF.Fluent
open QuestPDF.Infrastructure

/// <summary>A text style transformation; compose with <c>&gt;&gt;</c>, left to right.</summary>
type Style = Styled -> Styled

/// <summary>Text styles. Compose them with <c>&gt;&gt;</c>; a later style overrides an earlier one.</summary>
[<RequireQualifiedAccess>]
module Style =
    /// <summary>Leaves the style unchanged.</summary>
    let none: Style = id

    /// <summary>A style of a fluent QuestPDF text style chain, such as <c>fun s -&gt; s.Underline()</c>.</summary>
    let fluent (apply: TextStyle -> TextStyle) : Style =
        fun (Styled style) -> Styled (apply style)

    /// <summary>The QuestPDF text style of a style applied to a QuestPDF text style.</summary>
    let apply (style: Style) (textStyle: TextStyle) : TextStyle =
        let (Styled result) = style (Styled textStyle)
        result

    /// <summary>Sets the font size in points; accepts int, int64, float, float32 or decimal.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.FontSize(QuestPDF.Infrastructure.TextStyle,System.Single)"/>.</remarks>
    let inline size value : Style =
        Measured.fontSize (toFloatWith NumberWitness value)

    /// <summary>Sets the font colour.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.FontColor(QuestPDF.Infrastructure.TextStyle,QuestPDF.Infrastructure.Color)"/>.</remarks>
    let color (color: Color) : Style =
        fluent (fun style -> style.FontColor color)

    /// <summary>Sets the weight to Bold (700).</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.Bold(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let bold: Style = fluent (fun style -> style.Bold ())

    /// <summary>Sets the weight to SemiBold (600).</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.SemiBold(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let semiBold: Style = fluent (fun style -> style.SemiBold ())

    /// <summary>Sets the italic face.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.Italic(QuestPDF.Infrastructure.TextStyle,System.Boolean)"/>.</remarks>
    let italic: Style = fluent (fun style -> style.Italic ())

    /// <summary>Underlines the text.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.Underline(QuestPDF.Infrastructure.TextStyle,System.Boolean)"/>.</remarks>
    let underline: Style = fluent (fun style -> style.Underline ())

    /// <summary>Sets the font family.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.FontFamily(QuestPDF.Infrastructure.TextStyle,System.String[])"/>.</remarks>
    let family (name: string) : Style =
        fluent (fun style -> style.FontFamily name)

    /// <summary>Sets the line height as a multiple of the font size; accepts int, int64, float, float32 or decimal.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.LineHeight(QuestPDF.Infrastructure.TextStyle,System.Nullable{System.Single})"/>.</remarks>
    let inline lineHeight value : Style =
        Measured.lineHeight (toFloatWith NumberWitness value)

    /// <summary>The QuestPDF text style of a style applied to the default style.</summary>
    /// <remarks>Starts from <see cref="P:QuestPDF.Infrastructure.TextStyle.Default"/>.</remarks>
    let toTextStyle (style: Style) : TextStyle =
        apply style TextStyle.Default
