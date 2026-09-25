namespace FSharp.QuestPDF

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
        closure (fun (Styled style) -> Styled (apply style))

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


    /// <summary>Sets a list of font families; each later family supplies the glyphs missing from the earlier ones.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.FontFamily(QuestPDF.Infrastructure.TextStyle,System.String[])"/>.</remarks>
    let families (names: string list) : Style =
        fluent (fun style -> style.FontFamily (Array.ofList names))

    /// <summary>Fills the area behind the text with a colour.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.BackgroundColor(QuestPDF.Infrastructure.TextStyle,QuestPDF.Infrastructure.Color)"/>.</remarks>
    let background (color: Color) : Style =
        fluent (fun style -> style.BackgroundColor color)

    /// <summary>Sets the font weight.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.Weight(QuestPDF.Infrastructure.TextStyle,QuestPDF.Infrastructure.FontWeight)"/>.</remarks>
    let weight (weight: FontWeight) : Style =
        fluent (fun style -> style.Weight weight)

    /// <summary>Sets the weight to Thin (100).</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.Thin(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let thin: Style = fluent (fun style -> style.Thin ())

    /// <summary>Sets the weight to ExtraLight (200).</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.ExtraLight(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let extraLight: Style = fluent (fun style -> style.ExtraLight ())

    /// <summary>Sets the weight to Light (300).</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.Light(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let light: Style = fluent (fun style -> style.Light ())

    /// <summary>Sets the weight to Normal (400).</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.NormalWeight(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let normalWeight: Style = fluent (fun style -> style.NormalWeight ())

    /// <summary>Sets the weight to Medium (500).</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.Medium(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let medium: Style = fluent (fun style -> style.Medium ())

    /// <summary>Sets the weight to ExtraBold (800).</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.ExtraBold(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let extraBold: Style = fluent (fun style -> style.ExtraBold ())

    /// <summary>Sets the weight to Black (900).</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.Black(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let black: Style = fluent (fun style -> style.Black ())

    /// <summary>Sets the weight to ExtraBlack (1000).</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.ExtraBlack(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let extraBlack: Style = fluent (fun style -> style.ExtraBlack ())

    /// <summary>Draws a line through the text.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.Strikethrough(QuestPDF.Infrastructure.TextStyle,System.Boolean)"/>.</remarks>
    let strikethrough: Style = fluent (fun style -> style.Strikethrough ())

    /// <summary>Draws a line above the text.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.Overline(QuestPDF.Infrastructure.TextStyle,System.Boolean)"/>.</remarks>
    let overline: Style = fluent (fun style -> style.Overline ())

    /// <summary>Sets the colour of underlines, strikethroughs and overlines.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.DecorationColor(QuestPDF.Infrastructure.TextStyle,QuestPDF.Infrastructure.Color)"/>.</remarks>
    let decorationColor (color: Color) : Style =
        fluent (fun style -> style.DecorationColor color)

    /// <summary>Sets the thickness of decoration lines as a multiple of the default; accepts int, int64, float, float32 or decimal.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.DecorationThickness(QuestPDF.Infrastructure.TextStyle,System.Single)"/>.</remarks>
    let inline decorationThickness value : Style =
        Measured.decorationThickness (toFloatWith NumberWitness value)

    /// <summary>Draws decoration lines solid.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.DecorationSolid(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let decorationSolid: Style = fluent (fun style -> style.DecorationSolid ())

    /// <summary>Draws decoration lines doubled.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.DecorationDouble(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let decorationDouble: Style = fluent (fun style -> style.DecorationDouble ())

    /// <summary>Draws decoration lines wavy.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.DecorationWavy(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let decorationWavy: Style = fluent (fun style -> style.DecorationWavy ())

    /// <summary>Draws decoration lines dotted.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.DecorationDotted(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let decorationDotted: Style = fluent (fun style -> style.DecorationDotted ())

    /// <summary>Draws decoration lines dashed.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.DecorationDashed(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let decorationDashed: Style = fluent (fun style -> style.DecorationDashed ())

    /// <summary>Adds space between letters, as a multiple of the font size; accepts int, int64, float, float32 or decimal.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.LetterSpacing(QuestPDF.Infrastructure.TextStyle,System.Single)"/>.</remarks>
    let inline letterSpacing value : Style =
        Measured.letterSpacing (toFloatWith NumberWitness value)

    /// <summary>Adds space between words, as a multiple of the font size; accepts int, int64, float, float32 or decimal.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.WordSpacing(QuestPDF.Infrastructure.TextStyle,System.Single)"/>.</remarks>
    let inline wordSpacing value : Style =
        Measured.wordSpacing (toFloatWith NumberWitness value)

    /// <summary>Lowers the text to the subscript position.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.Subscript(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let subscript: Style = fluent (fun style -> style.Subscript ())

    /// <summary>Raises the text to the superscript position.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.Superscript(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let superscript: Style = fluent (fun style -> style.Superscript ())

    /// <summary>Places the text on the baseline.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.NormalPosition(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let normalPosition: Style = fluent (fun style -> style.NormalPosition ())

    /// <summary>Turns on an OpenType feature, such as <c>"tnum"</c>; <c>QuestPDF.Helpers.FontFeatures</c> lists the tags.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.EnableFontFeature(QuestPDF.Infrastructure.TextStyle,System.String)"/>.</remarks>
    let enableFeature (feature: string) : Style =
        fluent (fun style -> style.EnableFontFeature feature)

    /// <summary>Turns off an OpenType feature, such as <c>"kern"</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.DisableFontFeature(QuestPDF.Infrastructure.TextStyle,System.String)"/>.</remarks>
    let disableFeature (feature: string) : Style =
        fluent (fun style -> style.DisableFontFeature feature)

    /// <summary>Takes the text direction from the characters of the text.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.DirectionAuto(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let directionAuto: Style = fluent (fun style -> style.DirectionAuto ())

    /// <summary>Lays the text out left to right.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.DirectionFromLeftToRight(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let leftToRight: Style = fluent (fun style -> style.DirectionFromLeftToRight ())

    /// <summary>Lays the text out right to left.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.DirectionFromRightToLeft(QuestPDF.Infrastructure.TextStyle)"/>.</remarks>
    let rightToLeft: Style = fluent (fun style -> style.DirectionFromRightToLeft ())

    /// <summary>Allows line breaks between any two characters, in addition to breaks between words.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.TextStyleExtensions.BreakAnywhere(QuestPDF.Infrastructure.TextStyle,System.Boolean)"/>.</remarks>
    let breakAnywhere: Style = fluent (fun style -> style.BreakAnywhere ())

    /// <summary>
    /// Replaces the style with a QuestPDF text style; styles composed before it with <c>&gt;&gt;</c> are discarded. As the
    /// style of a <c>Text.withStyle</c> call, it is merged over the styles of the enclosing calls, as
    /// <c>Span(...).Style(textStyle)</c> is.
    /// </summary>
    let ofTextStyle (textStyle: TextStyle) : Style =
        closure (fun _ -> Styled textStyle)

    /// <summary>The QuestPDF text style of a style applied to the default style.</summary>
    /// <remarks>Starts from <see cref="P:QuestPDF.Infrastructure.TextStyle.Default"/>.</remarks>
    let toTextStyle (style: Style) : TextStyle =
        apply style TextStyle.Default
