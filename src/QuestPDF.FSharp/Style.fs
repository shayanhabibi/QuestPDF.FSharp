namespace QuestPDF.FSharp

open QuestPDF.Fluent
open QuestPDF.Infrastructure

/// <summary>A text style transformation; compose with <c>&gt;&gt;</c>, left to right.</summary>
/// <remarks>Operates on <see cref="T:QuestPDF.Infrastructure.TextStyle"/>.</remarks>
type Style = TextStyle -> TextStyle

/// <summary>Text styles. Compose them with <c>&gt;&gt;</c>; a later style overrides an earlier one.</summary>
[<RequireQualifiedAccess>]
module Style =
    /// <summary>Leaves the style unchanged.</summary>
    let none: Style = id

    /// <summary>Sets the font size in points; accepts any numeric type.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.FontSize(QuestPDF.Infrastructure.TextStyle,System.Single)"/>.</remarks>
    let inline size value : Style =
        Measured.fontSize (float value)

    /// <summary>Sets the font colour.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.FontColor(QuestPDF.Infrastructure.TextStyle,QuestPDF.Infrastructure.Color)"/>.</remarks>
    let color (color: Color) : Style =
        fun style -> style.FontColor color

    /// <summary>Sets the weight to Bold (700).</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.Bold(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let bold: Style = fun style -> style.Bold ()

    /// <summary>Sets the weight to SemiBold (600).</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.SemiBold(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let semiBold: Style = fun style -> style.SemiBold ()

    /// <summary>Sets the italic face.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.Italic(QuestPDF.Infrastructure.TextStyle,System.Boolean)"/>.</remarks>
    let italic: Style = fun style -> style.Italic ()

    /// <summary>Underlines the text.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.Underline(QuestPDF.Infrastructure.TextStyle,System.Boolean)"/>.</remarks>
    let underline: Style = fun style -> style.Underline ()

    /// <summary>Sets the font family.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.FontFamily(QuestPDF.Infrastructure.TextStyle,System.String[])"/>.</remarks>
    let family (name: string) : Style =
        fun style -> style.FontFamily name

    /// <summary>Sets the line height as a multiple of the font size; accepts any numeric type.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.LineHeight(QuestPDF.Infrastructure.TextStyle,System.Nullable{System.Single})"/>.</remarks>
    let inline lineHeight value : Style =
        Measured.lineHeight (float value)

    /// <summary>The QuestPDF text style of a style applied to the default style.</summary>
    /// <remarks>Starts from <see cref="P:QuestPDF.Infrastructure.TextStyle.Default"/>.</remarks>
    let toTextStyle (style: Style) : TextStyle =
        style TextStyle.Default
