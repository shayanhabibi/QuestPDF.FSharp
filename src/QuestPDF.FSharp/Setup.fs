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

/// <summary>A font registration made through <c>Font.register*</c>.</summary>
type FontSource =
    /// <summary>A font file, by its full path.</summary>
    | FontFile of path: string
    /// <summary>A directory of font files, by its full path.</summary>
    | FontDirectory of path: string
    /// <summary>The contents of a font file.</summary>
    | FontData of data: byte[]

/// <summary>Font registration and font settings. Each function changes process-wide QuestPDF state.</summary>
[<RequireQualifiedAccess>]
module Font =
    /// The registrations in order, with the keys of the registered paths and contents.
    let private registry = ResizeArray<FontSource> ()

    let private keys = Collections.Generic.HashSet<string> StringComparer.Ordinal

    let private pathKey (path: string) =
        let full =
            IO.Path.TrimEndingDirectorySeparator (IO.Path.GetFullPath path)

        if OperatingSystem.IsWindows () || OperatingSystem.IsMacOS () then
            full.ToUpperInvariant ()
        else
            full

    /// Registers a source unless its key is registered; the key is added only after the registration succeeds.
    let private registerOnce (key: string) (source: FontSource) (register: unit -> unit) =
        lock registry (fun () ->
            if not (keys.Contains key) then
                register ()
                keys.Add key |> ignore
                registry.Add source)

    /// <summary>
    /// Registers the font of a file. The family name comes from the file. Registering the same path again has no
    /// effect.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Drawing.FontManager.RegisterFontFromFile(System.String)"/>.</remarks>
    let registerFile (path: string) : unit =
        let full = IO.Path.GetFullPath path
        registerOnce ("file:" + pathKey full) (FontFile full) (fun () -> FontManager.RegisterFontFromFile full)

    /// <summary>
    /// Registers every font file of a directory. Registering the same path again has no effect.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Drawing.FontManager.RegisterFontsFromDirectory(System.String)"/>.</remarks>
    let registerDirectory (path: string) : unit =
        let full =
            IO.Path.TrimEndingDirectorySeparator (IO.Path.GetFullPath path)

        registerOnce
            ("directory:" + pathKey full)
            (FontDirectory full)
            (fun () -> FontManager.RegisterFontsFromDirectory full)

    /// <summary>
    /// Registers a font from the contents of a font file. Registering the same bytes again has no effect.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Drawing.FontManager.RegisterFontFromBinaryData(System.Byte[])"/>.</remarks>
    let registerBytes (data: byte[]) : unit =
        let key =
            "data:" + Convert.ToHexString (Security.Cryptography.SHA256.HashData data)

        let copy = Array.copy data
        registerOnce key (FontData copy) (fun () -> FontManager.RegisterFontFromBinaryData copy)

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

    /// <summary>
    /// The fonts registered through <c>Font.register*</c> in this process, in registration order, one entry per
    /// distinct path or content.
    /// </summary>
    let sources () : FontSource list =
        lock registry (fun () -> List.ofSeq registry)
