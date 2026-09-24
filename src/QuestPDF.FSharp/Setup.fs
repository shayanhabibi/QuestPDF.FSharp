namespace QuestPDF.FSharp

open System
open QuestPDF.Drawing
open QuestPDF.Infrastructure

/// <summary>The QuestPDF license declaration, required before generation and chosen by the caller.</summary>
/// <remarks>Sets <see cref="P:QuestPDF.Settings.License"/>.</remarks>
[<RequireQualifiedAccess>]
module License =
    /// <summary>Declares the Community license.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Settings.License"/> to <see cref="F:QuestPDF.Infrastructure.LicenseType.Community"/>.</remarks>
    let community () : unit =
        QuestPDF.Settings.License <- Nullable LicenseType.Community

    /// <summary>Declares the Professional license.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Settings.License"/> to <see cref="F:QuestPDF.Infrastructure.LicenseType.Professional"/>.</remarks>
    let professional () : unit =
        QuestPDF.Settings.License <- Nullable LicenseType.Professional

    /// <summary>Declares the Enterprise license.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Settings.License"/> to <see cref="F:QuestPDF.Infrastructure.LicenseType.Enterprise"/>.</remarks>
    let enterprise () : unit =
        QuestPDF.Settings.License <- Nullable LicenseType.Enterprise

/// <summary>Font registration and font settings. Each function changes process-wide QuestPDF state.</summary>
[<RequireQualifiedAccess>]
module Font =
    /// <summary>Registers the font of a file. The family name comes from the file.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Drawing.FontManager.RegisterFontFromFile(System.String)"/>.</remarks>
    let registerFile (path: string) : unit =
        FontManager.RegisterFontFromFile path

    /// <summary>Registers every font file of a directory.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Drawing.FontManager.RegisterFontsFromDirectory(System.String)"/>.</remarks>
    let registerDirectory (path: string) : unit =
        FontManager.RegisterFontsFromDirectory path

    /// <summary>Registers a font from the contents of a font file.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Drawing.FontManager.RegisterFontFromBinaryData(System.Byte[])"/>.</remarks>
    let registerBytes (data: byte[]) : unit =
        FontManager.RegisterFontFromBinaryData data

    /// <summary>Makes the fonts installed on the machine available.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Settings.UseSystemFonts"/>.</remarks>
    let useSystemFonts (enabled: bool) : unit =
        QuestPDF.Settings.UseSystemFonts <- enabled

    /// <summary>Makes generation raise on a missing font family or a missing glyph.</summary>
    /// <remarks>
    /// Sets <see cref="P:QuestPDF.Settings.ThrowOnMissingFontFamilies"/> and
    /// <see cref="P:QuestPDF.Settings.ThrowOnMissingTextGlyphs"/>.
    /// </remarks>
    let strict (enabled: bool) : unit =
        QuestPDF.Settings.ThrowOnMissingFontFamilies <- enabled
        QuestPDF.Settings.ThrowOnMissingTextGlyphs <- enabled

    /// <summary>The registered fonts.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Drawing.FontManager.GetRegisteredFonts"/>.</remarks>
    let registered () : FontInfo list =
        FontManager.GetRegisteredFonts () |> List.ofSeq
