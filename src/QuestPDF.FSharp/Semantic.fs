namespace QuestPDF.FSharp

open QuestPDF.Fluent

/// <summary>
/// Structure tags of the content, read by assistive technology. The tags are written to a tagged PDF, such as a
/// document with <c>Output.pdfUA</c>; the layout of the content is unchanged.
/// </summary>
[<RequireQualifiedAccess>]
module Semantic =
    /// <summary>Tags the content as an article: a self-contained composition, such as a news story or a blog post.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticArticle(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let article: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticArticle ()))

    /// <summary>Tags the content as a section: a thematic group of content, usually with a heading.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticSection(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let section: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticSection ()))

    /// <summary>Tags the content as a division: a generic group of content.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticDivision(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let division: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticDivision ()))

    /// <summary>Tags the content as a block quotation: one or more paragraphs quoted from another source.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticBlockQuotation(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let blockQuotation: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticBlockQuotation ()))

    /// <summary>Tags the content as a caption of a figure, table or list.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticCaption(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let caption: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticCaption ()))

    /// <summary>Tags the content as an index: a list of terms with references to the pages where they appear.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticIndex(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let index: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticIndex ()))

    /// <summary>Tags the content as a table of contents. Tag each of its entries with <c>Semantic.tableOfContentsItem</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticTableOfContents(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let tableOfContents: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticTableOfContents ()))

    /// <summary>Tags the content as an entry of a table of contents.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticTableOfContentsItem(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let tableOfContentsItem: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticTableOfContentsItem ()))

    /// <summary>Tags the content as a level 1 heading.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticHeading1(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let heading1: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticHeading1 ()))

    /// <summary>Tags the content as a level 2 heading.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticHeading2(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let heading2: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticHeading2 ()))

    /// <summary>Tags the content as a level 3 heading.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticHeading3(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let heading3: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticHeading3 ()))

    /// <summary>Tags the content as a level 4 heading.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticHeading4(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let heading4: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticHeading4 ()))

    /// <summary>Tags the content as a level 5 heading.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticHeading5(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let heading5: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticHeading5 ()))

    /// <summary>Tags the content as a level 6 heading.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticHeading6(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let heading6: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticHeading6 ()))

    /// <summary>Tags the content as a paragraph.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticParagraph(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let paragraph: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticParagraph ()))

    /// <summary>Tags the content as a list. Tag each of its items with <c>Semantic.listItem</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticList(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let list: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticList ()))

    /// <summary>Tags the content as a list item, holding a <c>Semantic.listLabel</c> and a <c>Semantic.listItemBody</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticListItem(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let listItem: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticListItem ()))

    /// <summary>Tags the content as the label of a list item, such as a bullet or a number.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticListLabel(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let listLabel: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticListLabel ()))

    /// <summary>Tags the content as the body of a list item.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticListItemBody(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let listItemBody: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticListItemBody ()))

    /// <summary>Tags the content as a table.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticTable(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let table: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticTable ()))

    /// <summary>Tags the content as an inline span of text.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticSpan(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let span: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticSpan ()))

    /// <summary>Tags the content as an inline quotation.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticQuote(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let quote: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticQuote ()))

    /// <summary>Tags the content as computer code.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticCode(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let code: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticCode ()))

    /// <summary>Tags the content as an inline span of text, read by assistive technology as the alternative text.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticSpan(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let spanWith (alternativeText: string) : Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticSpan alternativeText))

    /// <summary>Tags the content as a link, described to assistive technology by the alternative text.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticLink(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let link (alternativeText: string) : Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticLink alternativeText))

    /// <summary>Tags the content as a figure, such as a chart or a diagram, described to assistive technology by the alternative text.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticFigure(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let figure (alternativeText: string) : Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticFigure alternativeText))

    /// <summary>Tags the content as an image, described to assistive technology by the alternative text.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticImage(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let image (alternativeText: string) : Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticImage alternativeText))

    /// <summary>Tags the content as a mathematical formula, described to assistive technology by the alternative text.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticFormula(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let formula (alternativeText: string) : Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticFormula alternativeText))

    /// <summary>Sets the natural language of the content, as a BCP 47 tag such as <c>"en-AU"</c>.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticLanguage(QuestPDF.Infrastructure.IContainer,System.String)"/>.</remarks>
    let language (language: string) : Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticLanguage language))

    /// <summary>Marks the content as an artifact: decoration, such as a rule or a background, that assistive technology skips.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.SemanticExtensions.SemanticIgnore(QuestPDF.Infrastructure.IContainer)"/>.</remarks>
    let ignore: Modifier =
        closure (fun (Slot container) -> Slot (container.SemanticIgnore ()))
