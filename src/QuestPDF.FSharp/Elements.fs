namespace QuestPDF.FSharp

open QuestPDF.Fluent
open QuestPDF.Infrastructure

/// <summary>Leaf elements: lines, page breaks and placeholders.</summary>
[<AutoOpen>]
module Elements =
    /// <summary>A horizontal line of a thickness and colour, as wide as the slot; the thickness accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>
    /// Maps to <see cref="M:QuestPDF.Fluent.LineExtensions.LineHorizontal(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>
    /// and <see cref="M:QuestPDF.Fluent.LineDescriptor.LineColor(QuestPDF.Infrastructure.Color)"/>.
    /// </remarks>
    let inline lineH thickness (color: Color) : Content =
        Measured.lineH (len thickness) color

    /// <summary>A vertical line of a thickness and colour, as tall as the slot; the thickness accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>
    /// Maps to <see cref="M:QuestPDF.Fluent.LineExtensions.LineVertical(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>
    /// and <see cref="M:QuestPDF.Fluent.LineDescriptor.LineColor(QuestPDF.Infrastructure.Color)"/>.
    /// </remarks>
    let inline lineV thickness (color: Color) : Content =
        Measured.lineV (len thickness) color

    /// <summary>
    /// A horizontal line of a thickness, styled by a fluent chain on the line descriptor such as
    /// <c>fun l -&gt; l.LineColor(c).LineDashPattern [| 4f; 2f |]</c>; the thickness accepts int, int64, float, float32
    /// or decimal (points) or Length. The option takes the place of the colour of <c>lineH</c>.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.LineExtensions.LineHorizontal(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline lineHWith thickness (option: LineDescriptor -> LineDescriptor) : Content =
        Measured.lineHWith (len thickness) option

    /// <summary>
    /// A vertical line of a thickness, styled by a fluent chain on the line descriptor; the thickness accepts int, int64,
    /// float, float32 or decimal (points) or Length. The option takes the place of the colour of <c>lineV</c>.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.LineExtensions.LineVertical(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Unit)"/>.</remarks>
    let inline lineVWith thickness (option: LineDescriptor -> LineDescriptor) : Content =
        Measured.lineVWith (len thickness) option

    /// <summary>Ends the current page; the content after it starts on the next page.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.PageBreak(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let pageBreak: Content = closure (fun (Slot container) -> container.PageBreak ())

    /// <summary>A grey box with a label, standing in for content that is not yet designed.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.Placeholder(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let placeholder (label: string) : Content =
        closure (fun (Slot container) -> container.Placeholder label)

/// <summary>A setting of an image, such as its fit or resolution; compose settings with <c>&gt;&gt;</c>.</summary>
type ImageOption = ImageDescriptor -> ImageDescriptor

/// <summary>Raster and vector images from files or bytes.</summary>
[<RequireQualifiedAccess>]
module Image =
    /// <summary>The image in a file.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ImageExtensions.Image(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let file (path: string) : Content =
        closure (fun (Slot container) -> container.Image path |> ignore)

    /// <summary>The image in a file, with settings.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ImageExtensions.Image(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let fileWith (option: ImageOption) (path: string) : Content =
        closure (fun (Slot container) -> container.Image path |> option |> ignore)

    /// <summary>The image encoded in bytes, such as the contents of a PNG or JPEG file.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ImageExtensions.Image(QuestPDF.Infrastructure.IContainer,System.Byte[])"/>.</remarks>
    let bytes (data: byte[]) : Content =
        closure (fun (Slot container) -> container.Image data |> ignore)

    /// <summary>The image encoded in bytes, with settings.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ImageExtensions.Image(QuestPDF.Infrastructure.IContainer,System.Byte[])"/>.</remarks>
    let bytesWith (option: ImageOption) (data: byte[]) : Content =
        closure (fun (Slot container) -> container.Image data |> option |> ignore)

    /// <summary>
    /// A loaded image, embedded once however often it is drawn. The caller disposes of the image after the last
    /// generation.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ImageExtensions.Image(QuestPDF.Infrastructure.IContainer,QuestPDF.Infrastructure.Image)"/>.</remarks>
    let shared (image: QuestPDF.Infrastructure.Image) : Content =
        closure (fun (Slot container) -> container.Image image |> ignore)

    /// <summary>
    /// A loaded image with settings, embedded once however often it is drawn. The caller disposes of the image after
    /// the last generation.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ImageExtensions.Image(QuestPDF.Infrastructure.IContainer,QuestPDF.Infrastructure.Image)"/>.</remarks>
    let sharedWith (option: ImageOption) (image: QuestPDF.Infrastructure.Image) : Content =
        closure (fun (Slot container) -> container.Image image |> option |> ignore)

    /// <summary>Scales the image to the available width, keeping its aspect ratio.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ImageDescriptor.FitWidth"/>.</remarks>
    let fitWidth: ImageOption = closure (fun image -> image.FitWidth ())

    /// <summary>Scales the image to the available height, keeping its aspect ratio.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ImageDescriptor.FitHeight"/>.</remarks>
    let fitHeight: ImageOption = closure (fun image -> image.FitHeight ())

    /// <summary>Scales the image to the largest size inside the available area, keeping its aspect ratio.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ImageDescriptor.FitArea"/>.</remarks>
    let fitArea: ImageOption = closure (fun image -> image.FitArea ())

    /// <summary>Stretches the image over the available area.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ImageDescriptor.FitUnproportionally"/>.</remarks>
    let fitUnproportionally: ImageOption =
        closure (fun image -> image.FitUnproportionally ())

    /// <summary>Embeds the image data unchanged, skipping resampling and recompression.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ImageDescriptor.UseOriginalImage(System.Boolean)"/>.</remarks>
    let original: ImageOption = closure (fun image -> image.UseOriginalImage ())

    /// <summary>Sets the resolution the image is resampled to, in dots per inch.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ImageDescriptor.WithRasterDpi(System.Int32)"/>.</remarks>
    let dpi (value: int) : ImageOption =
        closure (fun image -> image.WithRasterDpi value)

    /// <summary>Sets the compression quality of the embedded image.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ImageDescriptor.WithCompressionQuality(QuestPDF.Infrastructure.ImageCompressionQuality)"/>.</remarks>
    let quality (value: ImageCompressionQuality) : ImageOption =
        closure (fun image -> image.WithCompressionQuality value)

    /// <summary>
    /// An image generated for the size it is drawn at: the function receives the resolution in pixels and returns the
    /// bytes of a PNG, JPEG or WEBP image.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ImageExtensions.Image(QuestPDF.Infrastructure.IContainer,System.Func{QuestPDF.Infrastructure.ImageSize,System.Byte[]})"/>.</remarks>
    let dynamic (generate: ImageSize -> byte[]) : Content =
        closure (fun (Slot container) -> container.Image (System.Func<ImageSize, byte[]> generate) |> ignore)

    /// <summary>An image generated for the size it is drawn at, with settings.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ImageExtensions.Image(QuestPDF.Infrastructure.IContainer,System.Func{QuestPDF.Infrastructure.ImageSize,System.Byte[]})"/>.</remarks>
    let dynamicWith (option: DynamicImageOption) (generate: ImageSize -> byte[]) : Content =
        closure (fun (Slot container) ->
            container.Image (System.Func<ImageSize, byte[]> generate)
            |> option
            |> ignore)

/// <summary>A setting of an SVG image, such as its fit; compose settings with <c>&gt;&gt;</c>.</summary>
type SvgOption = SvgImageDescriptor -> SvgImageDescriptor

/// <summary>SVG images.</summary>
[<RequireQualifiedAccess>]
module Svg =
    /// <summary>The image described by SVG markup.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SvgExtensions.Svg(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let text (markup: string) : Content =
        closure (fun (Slot container) -> container.Svg markup |> ignore)

    /// <summary>The image described by SVG markup, with settings.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SvgExtensions.Svg(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let textWith (option: SvgOption) (markup: string) : Content =
        closure (fun (Slot container) -> container.Svg markup |> option |> ignore)

    /// <summary>Scales the image to the available width, keeping its aspect ratio.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SvgImageDescriptor.FitWidth"/>.</remarks>
    let fitWidth: SvgOption = closure (fun image -> image.FitWidth ())

    /// <summary>Scales the image to the available height, keeping its aspect ratio.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SvgImageDescriptor.FitHeight"/>.</remarks>
    let fitHeight: SvgOption = closure (fun image -> image.FitHeight ())

    /// <summary>Scales the image to the largest size inside the available area, keeping its aspect ratio.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SvgImageDescriptor.FitArea"/>.</remarks>
    let fitArea: SvgOption = closure (fun image -> image.FitArea ())
