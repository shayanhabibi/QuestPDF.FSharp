namespace FSharp.QuestPDF

open QuestPDF.Helpers
open QuestPDF.Infrastructure

/// <summary>A QuestPDF colour.</summary>
/// <remarks>An alias of <see cref="T:QuestPDF.Infrastructure.Color"/>.</remarks>
type Color = QuestPDF.Infrastructure.Color

/// <summary>The Material colour palette, for example <c>Colors.Blue.Medium</c>.</summary>
/// <remarks>An alias of <see cref="T:QuestPDF.Helpers.Colors"/>.</remarks>
type Colors = QuestPDF.Helpers.Colors

/// <summary>A page size in points.</summary>
/// <remarks>An alias of <see cref="T:QuestPDF.Helpers.PageSize"/>.</remarks>
type PageSize = QuestPDF.Helpers.PageSize

/// <summary>The standard page sizes, for example <c>PageSizes.A4</c>.</summary>
/// <remarks>An alias of <see cref="T:QuestPDF.Helpers.PageSizes"/>.</remarks>
type PageSizes = QuestPDF.Helpers.PageSizes

/// <summary>A font weight, from Thin (100) to ExtraBlack (1000).</summary>
/// <remarks>An alias of <see cref="T:QuestPDF.Infrastructure.FontWeight"/>.</remarks>
type FontWeight = QuestPDF.Infrastructure.FontWeight

/// <summary>How an aspect ratio fits the available space: FitWidth, FitHeight or FitArea.</summary>
/// <remarks>An alias of <see cref="T:QuestPDF.Infrastructure.AspectRatioOption"/>.</remarks>
type AspectRatioOption = QuestPDF.Infrastructure.AspectRatioOption

/// <summary>A PDF/A conformance level, for example <c>PDFA_Conformance.PDFA_3B</c>.</summary>
/// <remarks>An alias of <see cref="T:QuestPDF.Infrastructure.PDFA_Conformance"/>.</remarks>
type PDFA_Conformance = QuestPDF.Infrastructure.PDFA_Conformance

/// <summary>An image compression quality, from Best to VeryLow.</summary>
/// <remarks>An alias of <see cref="T:QuestPDF.Infrastructure.ImageCompressionQuality"/>.</remarks>
type ImageCompressionQuality = QuestPDF.Infrastructure.ImageCompressionQuality

/// <summary>An image file format: Jpeg, Png or Webp.</summary>
/// <remarks>An alias of <see cref="T:QuestPDF.Infrastructure.ImageFormat"/>.</remarks>
type ImageFormat = QuestPDF.Infrastructure.ImageFormat

/// <summary>
/// The alignment of inline content to its line: AboveBaseline, BelowBaseline, Top, Bottom or Middle.
/// </summary>
/// <remarks>An alias of <see cref="T:QuestPDF.Infrastructure.TextInjectedElementAlignment"/>.</remarks>
type TextInjectedElementAlignment = QuestPDF.Infrastructure.TextInjectedElementAlignment

/// <summary>
/// The relationship of an attached file to the document: Data, Source, Alternative, Supplement or Unspecified.
/// </summary>
/// <remarks>An alias of <see cref="T:QuestPDF.Fluent.DocumentOperation.DocumentAttachmentRelationship"/>.</remarks>
type DocumentAttachmentRelationship = QuestPDF.Fluent.DocumentOperation.DocumentAttachmentRelationship

/// <summary>Colour constructors.</summary>
[<RequireQualifiedAccess>]
module Color =
    /// <summary>The colour of a <c>#RGB</c>, <c>#ARGB</c>, <c>#RRGGBB</c> or <c>#AARRGGBB</c> string.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Infrastructure.Color.FromHex(System.String)"/>.</remarks>
    let hex (value: string) : Color =
        QuestPDF.Infrastructure.Color.FromHex value

    /// <summary>An opaque colour of red, green and blue components.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Infrastructure.Color.FromRGB(System.Byte,System.Byte,System.Byte)"/>.</remarks>
    let rgb (red: byte) (green: byte) (blue: byte) : Color =
        QuestPDF.Infrastructure.Color.FromRGB (red, green, blue)

    /// <summary>A colour of alpha, red, green and blue components.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Infrastructure.Color.FromARGB(System.Byte,System.Byte,System.Byte,System.Byte)"/>.</remarks>
    let argb (alpha: byte) (red: byte) (green: byte) (blue: byte) : Color =
        QuestPDF.Infrastructure.Color.FromARGB (alpha, red, green, blue)

    /// <summary>The colour with its alpha replaced; 0.0 is transparent and 1.0 is opaque.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Infrastructure.Color.WithAlpha(System.Single)"/>.</remarks>
    let withAlpha (alpha: float) (color: Color) : Color =
        color.WithAlpha (float32 alpha)

/// <summary>Page size operations.</summary>
[<RequireQualifiedAccess>]
module PageSize =
    /// <summary>The size with its longer side horizontal.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Helpers.PageSizeExtensions.Landscape(QuestPDF.Helpers.PageSize)"/>.</remarks>
    let landscape (size: PageSize) : PageSize =
        size.Landscape ()

    /// <summary>The size with its longer side vertical.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Helpers.PageSizeExtensions.Portrait(QuestPDF.Helpers.PageSize)"/>.</remarks>
    let portrait (size: PageSize) : PageSize =
        size.Portrait ()

    /// <summary>
    /// A page size of a width and a height; each accepts int, int64, float, float32 or decimal (points) or Length.
    /// </summary>
    /// <remarks>
    /// When both lengths share a unit, QuestPDF converts them; otherwise both are converted with
    /// <see cref="M:FSharp.QuestPDF.LengthModule.points(FSharp.QuestPDF.Length)"/>. Maps to the
    /// <see cref="T:QuestPDF.Helpers.PageSize"/> constructor.
    /// </remarks>
    let inline custom width height : PageSize =
        Measured.pageSizeOf (len width) (len height)
