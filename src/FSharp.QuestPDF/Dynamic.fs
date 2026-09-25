namespace FSharp.QuestPDF

open QuestPDF.Elements
open QuestPDF.Fluent
open QuestPDF.Infrastructure

/// <summary>Deferred content and content position capture.</summary>
[<AutoOpen>]
module Deferred =
    /// <summary>
    /// Content built during generation, when the layout reaches it, and released after drawing. Lowers the memory use
    /// of documents with thousands of pages, at the cost of generation time.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.Lazy(QuestPDF.Infrastructure.IContainer,System.Action{QuestPDF.Infrastructure.IContainer})"/>.</remarks>
    let lazyContent (content: Content) : Content =
        closure (fun (Slot container) -> container.Lazy (fun inner -> content (Slot inner)))

    /// <summary>
    /// Content built during generation, as <c>lazyContent</c> is, with the built layout cached. Generates faster than
    /// <c>lazyContent</c> and keeps more native memory.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.LazyWithCache(QuestPDF.Infrastructure.IContainer,System.Action{QuestPDF.Infrastructure.IContainer})"/>.</remarks>
    let lazyContentCached (content: Content) : Content =
        closure (fun (Slot container) -> container.LazyWithCache (fun inner -> content (Slot inner)))

    /// <summary>
    /// Records the position and size of the content on each page under an id. A dynamic component reads the records
    /// with <c>DynamicContext.GetContentCapturedPositions</c>.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.ElementExtensions.CaptureContentPosition(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let capturePosition (id: string) : Modifier =
        closure (fun (Slot container) -> Slot (container.CaptureContentPosition id))

/// <summary>Dynamic components: page-aware content composed anew for each page.</summary>
[<RequireQualifiedAccess>]
module Dynamic =
    /// <summary>Content composed page by page by a dynamic component.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DynamicComponentExtensions.Dynamic(QuestPDF.Infrastructure.IContainer,QuestPDF.Infrastructure.IDynamicComponent)"/>.</remarks>
    let ofComponent (source: IDynamicComponent) : Content =
        closure (fun (Slot container) -> container.Dynamic source)

    /// <summary>Content composed page by page by a dynamic component that keeps its progress in its <c>State</c> property.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DynamicComponentExtensions.Dynamic``1(QuestPDF.Infrastructure.IContainer,QuestPDF.Infrastructure.IDynamicComponent{``0})"/>.</remarks>
    let ofStateful (source: IDynamicComponent<'State>) : Content =
        closure (fun (Slot container) -> container.Dynamic<'State> source)

    /// <summary>
    /// An element measured by a dynamic component, laid out unattached to the document. Draw it with
    /// <c>Dynamic.element</c>, or return it as the content of the composition result.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Elements.DynamicContext.CreateElement(System.Action{QuestPDF.Infrastructure.IContainer})"/>.</remarks>
    let createElement (context: DynamicContext) (content: Content) : IDynamicElement =
        context.CreateElement (fun container -> content (Slot container))

    /// <summary>An element made by <c>Dynamic.createElement</c>, drawn within the content of a dynamic component.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DynamicComponentExtensions.Element(QuestPDF.Infrastructure.IContainer,QuestPDF.Elements.IDynamicElement)"/>.</remarks>
    let element (element: IDynamicElement) : Content =
        closure (fun (Slot container) -> container.Element element)

/// <summary>A setting of a dynamic image, such as its resolution; compose settings with <c>&gt;&gt;</c>.</summary>
type DynamicImageOption = DynamicImageDescriptor -> DynamicImageDescriptor

/// <summary>The settings of an image generated for its drawn size, by <c>Image.dynamicWith</c>.</summary>
[<RequireQualifiedAccess>]
module DynamicImage =
    /// <summary>Embeds the generated image data unchanged, skipping resampling and recompression.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DynamicImageDescriptor.UseOriginalImage(System.Boolean)"/>.</remarks>
    let original: DynamicImageOption =
        closure (fun image -> image.UseOriginalImage ())

    /// <summary>Sets the resolution the image is generated at, in dots per inch.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DynamicImageDescriptor.WithRasterDpi(System.Int32)"/>.</remarks>
    let dpi (value: int) : DynamicImageOption =
        closure (fun image -> image.WithRasterDpi value)

    /// <summary>Sets the compression quality of the embedded image.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DynamicImageDescriptor.WithCompressionQuality(QuestPDF.Infrastructure.ImageCompressionQuality)"/>.</remarks>
    let quality (value: ImageCompressionQuality) : DynamicImageOption =
        closure (fun image -> image.WithCompressionQuality value)
