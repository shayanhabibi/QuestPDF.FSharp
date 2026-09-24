/// The QuestPDF fluent members and their QuestPDF.FSharp counterparts.
module QuestPDF.FSharp.Tests.Coverage

/// How QuestPDF.FSharp reaches a QuestPDF member.
type Mapping =
    /// Wrapped by public members, each named <c>Module.member</c> or, for an AutoOpen module, <c>member</c>.
    | Wrapped of string list
    /// Reached through <c>raw</c>, <c>fluent</c>, <c>modify</c> or a lambda part, for the reason given.
    | Raw of string

/// Every public, non-obsolete method of the QuestPDF.Fluent and QuestPDF.Companion types and of the QuestPDF extension
/// classes, keyed as <c>Type.Member</c>.
let mappings: (string * Mapping) list =
    [ "AlignmentExtensions.AlignBottom", Wrapped [ "alignBottom" ]
      "AlignmentExtensions.AlignCenter", Wrapped [ "alignCenter" ]
      "AlignmentExtensions.AlignLeft", Wrapped [ "alignLeft" ]
      "AlignmentExtensions.AlignMiddle", Wrapped [ "alignMiddle" ]
      "AlignmentExtensions.AlignRight", Wrapped [ "alignRight" ]
      "AlignmentExtensions.AlignTop", Wrapped [ "alignTop" ]
      "ColumnDescriptor.Item", Wrapped [ "column" ]
      "ColumnDescriptor.Spacing", Wrapped [ "columnSpaced" ]
      "ColumnExtensions.Column", Wrapped [ "columnSpaced"; "column" ]
      "CompanionExtensions.ShowInCompanion", Wrapped [ "Pdf.companion" ]
      "CompanionExtensions.ShowInCompanionAsync", Raw "the asynchronous Companion call is planned for a later version of QuestPDF.FSharp"
      "ComponentExtensions.Component", Wrapped [ "Content.ofComponent" ]
      "ConstrainedExtensions.Height", Wrapped [ "height" ]
      "ConstrainedExtensions.MaxHeight", Wrapped [ "maxHeight" ]
      "ConstrainedExtensions.MaxWidth", Wrapped [ "maxWidth" ]
      "ConstrainedExtensions.MinHeight", Wrapped [ "minHeight" ]
      "ConstrainedExtensions.MinWidth", Wrapped [ "minWidth" ]
      "ConstrainedExtensions.Width", Wrapped [ "width" ]
      "ContentDirectionExtensions.ContentFromLeftToRight", Wrapped [ "contentLtr" ]
      "ContentDirectionExtensions.ContentFromRightToLeft", Wrapped [ "contentRtl" ]
      "DebugExtensions.DebugArea", Wrapped [ "debugArea" ]
      "DebugExtensions.DebugPointer", Raw "debugging aid; use raw"
      "DecorationDescriptor.After", Wrapped [ "Decoration.after" ]
      "DecorationDescriptor.Before", Wrapped [ "Decoration.before" ]
      "DecorationDescriptor.Content", Wrapped [ "Decoration.content" ]
      "DecorationExtensions.Decoration", Wrapped [ "decoration" ]
      "Document.Compose", Raw "the IDocument implementation QuestPDF calls during generation"
      "Document.Create", Wrapped [ "document"; "page" ]
      "Document.GetMetadata", Raw "reads back the metadata of a built document; call it on the Document that document returns"
      "Document.GetSettings", Raw "reads back the settings of a built document; call it on the Document that document returns"
      "Document.Merge",
      Raw "merging documents is planned for a later version of QuestPDF.FSharp; Document.Merge accepts the documents that document returns"
      "Document.WithMetadata",
      Wrapped
          [ "Meta.title"
            "Meta.author"
            "Meta.subject"
            "Meta.keywords"
            "Meta.creator"
            "Meta.producer"
            "Meta.language"
            "Meta.created"
            "Meta.modified"
            "Meta.dated" ]
      "Document.WithSettings",
      Wrapped
          [ "Output.pdfA"
            "Output.pdfUA"
            "Output.compress"
            "Output.imageQuality"
            "Output.imageDpi"
            "Output.rightToLeft" ]
      "DocumentOperation.AddAttachment",
      Raw "the PDF file operations of qpdf are planned for a later version of QuestPDF.FSharp; call DocumentOperation directly"
      "DocumentOperation.Decrypt",
      Raw "the PDF file operations of qpdf are planned for a later version of QuestPDF.FSharp; call DocumentOperation directly"
      "DocumentOperation.Encrypt",
      Raw "the PDF file operations of qpdf are planned for a later version of QuestPDF.FSharp; call DocumentOperation directly"
      "DocumentOperation.ExtendMetadata",
      Raw "the PDF file operations of qpdf are planned for a later version of QuestPDF.FSharp; call DocumentOperation directly"
      "DocumentOperation.Linearize",
      Raw "the PDF file operations of qpdf are planned for a later version of QuestPDF.FSharp; call DocumentOperation directly"
      "DocumentOperation.LoadFile",
      Raw "the PDF file operations of qpdf are planned for a later version of QuestPDF.FSharp; call DocumentOperation directly"
      "DocumentOperation.MergeFile",
      Raw "the PDF file operations of qpdf are planned for a later version of QuestPDF.FSharp; call DocumentOperation directly"
      "DocumentOperation.OverlayFile",
      Raw "the PDF file operations of qpdf are planned for a later version of QuestPDF.FSharp; call DocumentOperation directly"
      "DocumentOperation.RemoveRestrictions",
      Raw "the PDF file operations of qpdf are planned for a later version of QuestPDF.FSharp; call DocumentOperation directly"
      "DocumentOperation.Save",
      Raw "the PDF file operations of qpdf are planned for a later version of QuestPDF.FSharp; call DocumentOperation directly"
      "DocumentOperation.TakePages",
      Raw "the PDF file operations of qpdf are planned for a later version of QuestPDF.FSharp; call DocumentOperation directly"
      "DocumentOperation.UnderlayFile",
      Raw "the PDF file operations of qpdf are planned for a later version of QuestPDF.FSharp; call DocumentOperation directly"
      "DynamicComponentExtensions.Dynamic", Raw "dynamic components are planned for a later version of QuestPDF.FSharp"
      "DynamicComponentExtensions.Element", Raw "dynamic components are planned for a later version of QuestPDF.FSharp"
      "DynamicImageDescriptor.UseOriginalImage", Raw "dynamic images are planned for a later version of QuestPDF.FSharp"
      "DynamicImageDescriptor.WithCompressionQuality", Raw "dynamic images are planned for a later version of QuestPDF.FSharp"
      "DynamicImageDescriptor.WithRasterDpi", Raw "dynamic images are planned for a later version of QuestPDF.FSharp"
      "ElementExtensions.AspectRatio", Wrapped [ "aspectRatioWith"; "aspectRatio" ]
      "ElementExtensions.CaptureContentPosition", Raw "content position capture is planned for a later version of QuestPDF.FSharp"
      "ElementExtensions.Container", Raw "a Content already receives its container; use raw"
      "ElementExtensions.DefaultTextStyle", Wrapped [ "textStyle" ]
      "ElementExtensions.Element", Wrapped [ "modify"; "raw" ]
      "ElementExtensions.EnsureSpace", Wrapped [ "ensureSpace" ]
      "ElementExtensions.Hyperlink", Wrapped [ "hyperlink" ]
      "ElementExtensions.Lazy", Raw "lazy content is planned for a later version of QuestPDF.FSharp"
      "ElementExtensions.LazyWithCache", Raw "lazy content is planned for a later version of QuestPDF.FSharp"
      "ElementExtensions.PageBreak", Wrapped [ "pageBreak" ]
      "ElementExtensions.Placeholder", Wrapped [ "placeholder" ]
      "ElementExtensions.PreventPageBreak", Wrapped [ "preventPageBreak" ]
      "ElementExtensions.Repeat", Wrapped [ "repeat" ]
      "ElementExtensions.ScaleToFit", Wrapped [ "scaleToFit" ]
      "ElementExtensions.Section", Wrapped [ "section" ]
      "ElementExtensions.SectionLink", Wrapped [ "sectionLink" ]
      "ElementExtensions.ShowEntire", Wrapped [ "showEntire" ]
      "ElementExtensions.ShowIf", Wrapped [ "showWhen"; "showIf" ]
      "ElementExtensions.ShowOnce", Wrapped [ "showOnce" ]
      "ElementExtensions.SkipOnce", Wrapped [ "skipOnce" ]
      "ElementExtensions.StopPaging", Wrapped [ "stopPaging" ]
      "ElementExtensions.Unconstrained", Wrapped [ "unconstrained" ]
      "ElementExtensions.ZIndex", Wrapped [ "zIndex" ]
      "ExtendExtensions.Extend", Wrapped [ "extend" ]
      "ExtendExtensions.ExtendHorizontal", Wrapped [ "extendH" ]
      "ExtendExtensions.ExtendVertical", Wrapped [ "extendV" ]
      "GenerateExtensions.GenerateImages", Wrapped [ "Pdf.images" ]
      "GenerateExtensions.GeneratePdf", Wrapped [ "Pdf.write"; "Pdf.save"; "Pdf.bytes" ]
      "GenerateExtensions.GeneratePdfAndShow", Wrapped [ "Pdf.show" ]
      "GenerateExtensions.GenerateSvg", Wrapped [ "Pdf.svgs" ]
      "GridDescriptor.AlignCenter", Raw "Grid is obsolete in QuestPDF; use table or row"
      "GridDescriptor.AlignLeft", Raw "Grid is obsolete in QuestPDF; use table or row"
      "GridDescriptor.AlignRight", Raw "Grid is obsolete in QuestPDF; use table or row"
      "GridDescriptor.Alignment", Raw "Grid is obsolete in QuestPDF; use table or row"
      "GridDescriptor.Columns", Raw "Grid is obsolete in QuestPDF; use table or row"
      "GridDescriptor.HorizontalSpacing", Raw "Grid is obsolete in QuestPDF; use table or row"
      "GridDescriptor.Item", Raw "Grid is obsolete in QuestPDF; use table or row"
      "GridDescriptor.Spacing", Raw "Grid is obsolete in QuestPDF; use table or row"
      "GridDescriptor.VerticalSpacing", Raw "Grid is obsolete in QuestPDF; use table or row"
      "ImageDescriptor.FitArea", Wrapped [ "Image.fitArea" ]
      "ImageDescriptor.FitHeight", Wrapped [ "Image.fitHeight" ]
      "ImageDescriptor.FitUnproportionally", Wrapped [ "Image.fitUnproportionally" ]
      "ImageDescriptor.FitWidth", Wrapped [ "Image.fitWidth" ]
      "ImageDescriptor.UseOriginalImage", Wrapped [ "Image.original" ]
      "ImageDescriptor.WithCompressionQuality", Wrapped [ "Image.quality" ]
      "ImageDescriptor.WithRasterDpi", Wrapped [ "Image.dpi" ]
      "ImageExtensions.Image",
      Wrapped
          [ "Image.shared"
            "Image.bytesWith"
            "Image.bytes"
            "Image.fileWith"
            "Image.file" ]
      "InlinedDescriptor.AlignCenter", Raw "inlined layout is planned for a later version of QuestPDF.FSharp"
      "InlinedDescriptor.AlignJustify", Raw "inlined layout is planned for a later version of QuestPDF.FSharp"
      "InlinedDescriptor.AlignLeft", Raw "inlined layout is planned for a later version of QuestPDF.FSharp"
      "InlinedDescriptor.AlignRight", Raw "inlined layout is planned for a later version of QuestPDF.FSharp"
      "InlinedDescriptor.AlignSpaceAround", Raw "inlined layout is planned for a later version of QuestPDF.FSharp"
      "InlinedDescriptor.BaselineBottom", Raw "inlined layout is planned for a later version of QuestPDF.FSharp"
      "InlinedDescriptor.BaselineMiddle", Raw "inlined layout is planned for a later version of QuestPDF.FSharp"
      "InlinedDescriptor.BaselineTop", Raw "inlined layout is planned for a later version of QuestPDF.FSharp"
      "InlinedDescriptor.HorizontalSpacing", Raw "inlined layout is planned for a later version of QuestPDF.FSharp"
      "InlinedDescriptor.Item", Raw "inlined layout is planned for a later version of QuestPDF.FSharp"
      "InlinedDescriptor.Spacing", Raw "inlined layout is planned for a later version of QuestPDF.FSharp"
      "InlinedDescriptor.VerticalSpacing", Raw "inlined layout is planned for a later version of QuestPDF.FSharp"
      "InlinedExtensions.Inlined", Raw "inlined layout is planned for a later version of QuestPDF.FSharp"
      "LayerExtensions.Layers", Wrapped [ "layers" ]
      "LayersDescriptor.Layer", Wrapped [ "Layers.layer" ]
      "LayersDescriptor.PrimaryLayer", Wrapped [ "Layers.primary" ]
      "LineDescriptor.LineColor", Wrapped [ "lineV"; "lineH" ]
      "LineDescriptor.LineDashPattern", Wrapped [ "lineHWith"; "lineVWith" ]
      "LineDescriptor.LineGradient", Wrapped [ "lineHWith"; "lineVWith" ]
      "LineExtensions.LineHorizontal", Wrapped [ "lineHWith"; "lineH" ]
      "LineExtensions.LineVertical", Wrapped [ "lineVWith"; "lineV" ]
      "MultiColumnDescriptor.BalanceHeight", Raw "multi-column layout is planned for a later version of QuestPDF.FSharp"
      "MultiColumnDescriptor.Columns", Raw "multi-column layout is planned for a later version of QuestPDF.FSharp"
      "MultiColumnDescriptor.Content", Raw "multi-column layout is planned for a later version of QuestPDF.FSharp"
      "MultiColumnDescriptor.Spacer", Raw "multi-column layout is planned for a later version of QuestPDF.FSharp"
      "MultiColumnDescriptor.Spacing", Raw "multi-column layout is planned for a later version of QuestPDF.FSharp"
      "MultiColumnExtensions.MultiColumn", Raw "multi-column layout is planned for a later version of QuestPDF.FSharp"
      "OffsetExtensions.OffsetX", Wrapped [ "offsetX" ]
      "OffsetExtensions.OffsetY", Wrapped [ "offsetY" ]
      "PaddingExtensions.Padding", Wrapped [ "padding" ]
      "PaddingExtensions.PaddingBottom", Wrapped [ "paddingBottom" ]
      "PaddingExtensions.PaddingHorizontal", Wrapped [ "paddingH" ]
      "PaddingExtensions.PaddingLeft", Wrapped [ "paddingLeft" ]
      "PaddingExtensions.PaddingRight", Wrapped [ "paddingRight" ]
      "PaddingExtensions.PaddingTop", Wrapped [ "paddingTop" ]
      "PaddingExtensions.PaddingVertical", Wrapped [ "paddingV" ]
      "PageDescriptor.Background", Wrapped [ "Page.background" ]
      "PageDescriptor.Content", Wrapped [ "Page.content" ]
      "PageDescriptor.ContentFromLeftToRight", Wrapped [ "Page.leftToRight" ]
      "PageDescriptor.ContentFromRightToLeft", Wrapped [ "Page.rightToLeft" ]
      "PageDescriptor.ContinuousSize", Wrapped [ "Page.continuous" ]
      "PageDescriptor.DefaultTextStyle", Wrapped [ "Page.textStyle" ]
      "PageDescriptor.Footer", Wrapped [ "Page.footer" ]
      "PageDescriptor.Foreground", Wrapped [ "Page.foreground" ]
      "PageDescriptor.Header", Wrapped [ "Page.header" ]
      "PageDescriptor.Margin", Wrapped [ "Page.margin" ]
      "PageDescriptor.MarginBottom", Wrapped [ "Page.marginBottom" ]
      "PageDescriptor.MarginHorizontal", Wrapped [ "Page.marginH" ]
      "PageDescriptor.MarginLeft", Wrapped [ "Page.marginLeft" ]
      "PageDescriptor.MarginRight", Wrapped [ "Page.marginRight" ]
      "PageDescriptor.MarginTop", Wrapped [ "Page.marginTop" ]
      "PageDescriptor.MarginVertical", Wrapped [ "Page.marginV" ]
      "PageDescriptor.MaxSize", Wrapped [ "Page.maxSize" ]
      "PageDescriptor.MinSize", Wrapped [ "Page.minSize" ]
      "PageDescriptor.PageColor", Wrapped [ "Page.color" ]
      "PageDescriptor.Size", Wrapped [ "Page.sizeOf"; "Page.size" ]
      "PageSizeExtensions.Landscape", Wrapped [ "PageSize.landscape" ]
      "PageSizeExtensions.Portrait", Wrapped [ "PageSize.portrait" ]
      "PageExtensions.Page", Wrapped [ "page" ]
      "RotateExtensions.Rotate", Wrapped [ "rotate" ]
      "RotateExtensions.RotateLayoutClockwise", Wrapped [ "rotateLayoutCw" ]
      "RotateExtensions.RotateLayoutCounterclockwise", Wrapped [ "rotateLayoutCcw" ]
      "RowDescriptor.AutoItem", Wrapped [ "Row.auto" ]
      "RowDescriptor.ConstantItem", Wrapped [ "Row.constant" ]
      "RowDescriptor.RelativeItem", Wrapped [ "Row.relative"; "Row.fill" ]
      "RowDescriptor.Spacing", Wrapped [ "Row.spacing" ]
      "RowExtensions.Row", Wrapped [ "row" ]
      "ScaleExtensions.FlipHorizontal", Wrapped [ "flipH" ]
      "ScaleExtensions.FlipOver", Wrapped [ "flipOver" ]
      "ScaleExtensions.FlipVertical", Wrapped [ "flipV" ]
      "ScaleExtensions.Scale", Wrapped [ "scale" ]
      "ScaleExtensions.ScaleHorizontal", Wrapped [ "scaleH" ]
      "ScaleExtensions.ScaleVertical", Wrapped [ "scaleV" ]
      "SemanticExtensions.SemanticArticle", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticBlockQuotation", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticCaption", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticCode", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticDivision", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticFigure", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticFormula", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticHeading1", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticHeading2", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticHeading3", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticHeading4", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticHeading5", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticHeading6", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticIgnore", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticImage", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticIndex", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticLanguage", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticLink", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticList", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticListItem", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticListItemBody", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticListLabel", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticParagraph", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticQuote", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticSection", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticSpan", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticTable", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticTableOfContents", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "SemanticExtensions.SemanticTableOfContentsItem", Raw "semantic tagging for PDF/UA is planned for a later version of QuestPDF.FSharp"
      "ShrinkExtensions.Shrink", Wrapped [ "shrink" ]
      "ShrinkExtensions.ShrinkHorizontal", Wrapped [ "shrinkH" ]
      "ShrinkExtensions.ShrinkVertical", Wrapped [ "shrinkV" ]
      "StyledBoxExtensions.Background", Wrapped [ "background" ]
      "StyledBoxExtensions.BackgroundLinearGradient", Raw "gradients are planned for a later version of QuestPDF.FSharp"
      "StyledBoxExtensions.Border", Wrapped [ "border" ]
      "StyledBoxExtensions.BorderAlignmentInside", Wrapped [ "borderInside" ]
      "StyledBoxExtensions.BorderAlignmentMiddle", Wrapped [ "borderMiddle" ]
      "StyledBoxExtensions.BorderAlignmentOutside", Wrapped [ "borderOutside" ]
      "StyledBoxExtensions.BorderBottom", Wrapped [ "borderBottom" ]
      "StyledBoxExtensions.BorderColor", Wrapped [ "borderColor" ]
      "StyledBoxExtensions.BorderHorizontal", Wrapped [ "borderH" ]
      "StyledBoxExtensions.BorderLeft", Wrapped [ "borderLeft" ]
      "StyledBoxExtensions.BorderLinearGradient", Raw "gradients are planned for a later version of QuestPDF.FSharp"
      "StyledBoxExtensions.BorderRight", Wrapped [ "borderRight" ]
      "StyledBoxExtensions.BorderTop", Wrapped [ "borderTop" ]
      "StyledBoxExtensions.BorderVertical", Wrapped [ "borderV" ]
      "StyledBoxExtensions.CornerRadius", Wrapped [ "cornerRadius" ]
      "StyledBoxExtensions.CornerRadiusBottomLeft", Wrapped [ "cornerRadiusBottomLeft" ]
      "StyledBoxExtensions.CornerRadiusBottomRight", Wrapped [ "cornerRadiusBottomRight" ]
      "StyledBoxExtensions.CornerRadiusTopLeft", Wrapped [ "cornerRadiusTopLeft" ]
      "StyledBoxExtensions.CornerRadiusTopRight", Wrapped [ "cornerRadiusTopRight" ]
      "StyledBoxExtensions.Shadow", Raw "shadows are planned for a later version of QuestPDF.FSharp"
      "SvgExtensions.Svg", Wrapped [ "Svg.textWith"; "Svg.text" ]
      "SvgImageDescriptor.FitArea", Wrapped [ "Svg.fitArea" ]
      "SvgImageDescriptor.FitHeight", Wrapped [ "Svg.fitHeight" ]
      "SvgImageDescriptor.FitWidth", Wrapped [ "Svg.fitWidth" ]
      "TableCellDescriptor.Cell", Wrapped [ "Table.header"; "Table.footer" ]
      "TableCellExtensions.Column", Wrapped [ "Cell.at" ]
      "TableCellExtensions.ColumnSpan", Wrapped [ "Cell.columnSpan" ]
      "TableCellExtensions.Row", Wrapped [ "Cell.at" ]
      "TableCellExtensions.RowSpan", Wrapped [ "Cell.rowSpan" ]
      "TableCellExtensions.SemanticHorizontalHeader", Raw "semantic tagging is planned for a later version of QuestPDF.FSharp"
      "TableColumnsDefinitionDescriptor.ConstantColumn", Wrapped [ "Table.constant" ]
      "TableColumnsDefinitionDescriptor.RelativeColumn", Wrapped [ "Table.relative" ]
      "TableDescriptor.Cell", Wrapped [ "Table.cells" ]
      "TableDescriptor.ColumnsDefinition", Wrapped [ "Table.columns" ]
      "TableDescriptor.ExtendLastCellsToTableBottom", Wrapped [ "Table.extendLastCellsToBottom" ]
      "TableDescriptor.Footer", Wrapped [ "Table.footer" ]
      "TableDescriptor.Header", Wrapped [ "Table.header" ]
      "TableExtensions.Table", Wrapped [ "table" ]
      "TextBlockDescriptor.AlignCenter", Wrapped [ "Text.alignCenter" ]
      "TextBlockDescriptor.AlignEnd", Wrapped [ "Text.alignEnd" ]
      "TextBlockDescriptor.AlignLeft", Wrapped [ "Text.alignLeft" ]
      "TextBlockDescriptor.AlignRight", Wrapped [ "Text.alignRight" ]
      "TextBlockDescriptor.AlignStart", Wrapped [ "Text.alignStart" ]
      "TextBlockDescriptor.ClampLines", Wrapped [ "Text.clampLines"; "Text.clampLinesWith" ]
      "TextBlockDescriptor.Justify", Wrapped [ "Text.justify" ]
      "TextBlockDescriptor.ParagraphFirstLineIndentation", Wrapped [ "Text.firstLineIndent" ]
      "TextBlockDescriptor.ParagraphSpacing", Wrapped [ "Text.paragraphSpacing" ]
      "TextDescriptor.AlignCenter", Wrapped [ "Text.alignCenter" ]
      "TextDescriptor.AlignEnd", Wrapped [ "Text.alignEnd" ]
      "TextDescriptor.AlignLeft", Wrapped [ "Text.alignLeft" ]
      "TextDescriptor.AlignRight", Wrapped [ "Text.alignRight" ]
      "TextDescriptor.AlignStart", Wrapped [ "Text.alignStart" ]
      "TextDescriptor.BeginPageNumberOfSection", Wrapped [ "Text.sectionBeginPage" ]
      "TextDescriptor.ClampLines", Wrapped [ "Text.clampLinesWith"; "Text.clampLines" ]
      "TextDescriptor.CurrentPageNumber", Wrapped [ "Text.pageNumber" ]
      "TextDescriptor.DefaultTextStyle", Wrapped [ "Text.style" ]
      "TextDescriptor.Element", Raw "inline elements in text are planned for a later version of QuestPDF.FSharp"
      "TextDescriptor.EmptyLine", Wrapped [ "Text.emptyLine"; "Text.lineBreak" ]
      "TextDescriptor.EndPageNumberOfSection", Wrapped [ "Text.sectionEndPage" ]
      "TextDescriptor.Hyperlink", Wrapped [ "Text.link" ]
      "TextDescriptor.Justify", Wrapped [ "Text.justify" ]
      "TextDescriptor.Line", Wrapped [ "Text.line" ]
      "TextDescriptor.PageNumberWithinSection", Wrapped [ "Text.sectionPageNumber" ]
      "TextDescriptor.ParagraphFirstLineIndentation", Wrapped [ "Text.firstLineIndent" ]
      "TextDescriptor.ParagraphSpacing", Wrapped [ "Text.paragraphSpacing" ]
      "TextDescriptor.SectionLink", Wrapped [ "Text.sectionLink" ]
      "TextDescriptor.Span", Wrapped [ "Text.styled"; "Text.span" ]
      "TextDescriptor.TotalPages", Wrapped [ "Text.totalPages" ]
      "TextDescriptor.TotalPagesWithinSection", Wrapped [ "Text.sectionTotalPages" ]
      "TextExtensions.Text", Wrapped [ "richText"; "styledText"; "text" ]
      "TextPageNumberDescriptor.Format", Wrapped [ "Text.formatPage" ]
      "TextSpanDescriptorExtensions.BackgroundColor", Wrapped [ "Style.background"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.Black", Wrapped [ "Style.black"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.Bold", Wrapped [ "Style.bold"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.BreakAnywhere", Wrapped [ "Style.breakAnywhere"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.DecorationColor", Wrapped [ "Style.decorationColor"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.DecorationDashed", Wrapped [ "Style.decorationDashed"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.DecorationDotted", Wrapped [ "Style.decorationDotted"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.DecorationDouble", Wrapped [ "Style.decorationDouble"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.DecorationSolid", Wrapped [ "Style.decorationSolid"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.DecorationThickness", Wrapped [ "Style.decorationThickness"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.DecorationWavy", Wrapped [ "Style.decorationWavy"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.DirectionAuto", Wrapped [ "Style.directionAuto"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.DirectionFromLeftToRight", Wrapped [ "Style.leftToRight"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.DirectionFromRightToLeft", Wrapped [ "Style.rightToLeft"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.DisableFontFeature", Wrapped [ "Style.disableFeature"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.EnableFontFeature", Wrapped [ "Style.enableFeature"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.ExtraBlack", Wrapped [ "Style.extraBlack"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.ExtraBold", Wrapped [ "Style.extraBold"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.ExtraLight", Wrapped [ "Style.extraLight"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.FontColor", Wrapped [ "Style.color"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.FontFamily", Wrapped [ "Style.families"; "Style.family"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.FontSize", Wrapped [ "Style.size"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.Italic", Wrapped [ "Style.italic"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.LetterSpacing", Wrapped [ "Style.letterSpacing"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.Light", Wrapped [ "Style.light"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.LineHeight", Wrapped [ "Style.lineHeight"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.Medium", Wrapped [ "Style.medium"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.NormalPosition", Wrapped [ "Style.normalPosition"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.NormalWeight", Wrapped [ "Style.normalWeight"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.Overline", Wrapped [ "Style.overline"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.SemiBold", Wrapped [ "Style.semiBold"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.Strikethrough", Wrapped [ "Style.strikethrough"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.Style", Wrapped [ "Text.withStyle" ]
      "TextSpanDescriptorExtensions.Subscript", Wrapped [ "Style.subscript"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.Superscript", Wrapped [ "Style.superscript"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.Thin", Wrapped [ "Style.thin"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.Underline", Wrapped [ "Style.underline"; "Text.withStyle" ]
      "TextSpanDescriptorExtensions.WordSpacing", Wrapped [ "Style.wordSpacing"; "Text.withStyle" ]
      "TextStyleExtensions.BackgroundColor", Wrapped [ "Style.background" ]
      "TextStyleExtensions.Black", Wrapped [ "Style.black" ]
      "TextStyleExtensions.Bold", Wrapped [ "Style.bold" ]
      "TextStyleExtensions.BreakAnywhere", Wrapped [ "Style.breakAnywhere" ]
      "TextStyleExtensions.DecorationColor", Wrapped [ "Style.decorationColor" ]
      "TextStyleExtensions.DecorationDashed", Wrapped [ "Style.decorationDashed" ]
      "TextStyleExtensions.DecorationDotted", Wrapped [ "Style.decorationDotted" ]
      "TextStyleExtensions.DecorationDouble", Wrapped [ "Style.decorationDouble" ]
      "TextStyleExtensions.DecorationSolid", Wrapped [ "Style.decorationSolid" ]
      "TextStyleExtensions.DecorationThickness", Wrapped [ "Style.decorationThickness" ]
      "TextStyleExtensions.DecorationWavy", Wrapped [ "Style.decorationWavy" ]
      "TextStyleExtensions.DirectionAuto", Wrapped [ "Style.directionAuto" ]
      "TextStyleExtensions.DirectionFromLeftToRight", Wrapped [ "Style.leftToRight" ]
      "TextStyleExtensions.DirectionFromRightToLeft", Wrapped [ "Style.rightToLeft" ]
      "TextStyleExtensions.DisableFontFeature", Wrapped [ "Style.disableFeature" ]
      "TextStyleExtensions.EnableFontFeature", Wrapped [ "Style.enableFeature" ]
      "TextStyleExtensions.ExtraBlack", Wrapped [ "Style.extraBlack" ]
      "TextStyleExtensions.ExtraBold", Wrapped [ "Style.extraBold" ]
      "TextStyleExtensions.ExtraLight", Wrapped [ "Style.extraLight" ]
      "TextStyleExtensions.FontColor", Wrapped [ "Style.color" ]
      "TextStyleExtensions.FontFamily", Wrapped [ "Style.families"; "Style.family" ]
      "TextStyleExtensions.FontSize", Wrapped [ "Style.size" ]
      "TextStyleExtensions.Italic", Wrapped [ "Style.italic" ]
      "TextStyleExtensions.LetterSpacing", Wrapped [ "Style.letterSpacing" ]
      "TextStyleExtensions.Light", Wrapped [ "Style.light" ]
      "TextStyleExtensions.LineHeight", Wrapped [ "Style.lineHeight" ]
      "TextStyleExtensions.Medium", Wrapped [ "Style.medium" ]
      "TextStyleExtensions.NormalPosition", Wrapped [ "Style.normalPosition" ]
      "TextStyleExtensions.NormalWeight", Wrapped [ "Style.normalWeight" ]
      "TextStyleExtensions.Overline", Wrapped [ "Style.overline" ]
      "TextStyleExtensions.SemiBold", Wrapped [ "Style.semiBold" ]
      "TextStyleExtensions.Strikethrough", Wrapped [ "Style.strikethrough" ]
      "TextStyleExtensions.Subscript", Wrapped [ "Style.subscript" ]
      "TextStyleExtensions.Superscript", Wrapped [ "Style.superscript" ]
      "TextStyleExtensions.Thin", Wrapped [ "Style.thin" ]
      "TextStyleExtensions.Underline", Wrapped [ "Style.underline" ]
      "TextStyleExtensions.Weight", Wrapped [ "Style.weight" ]
      "TextStyleExtensions.WordSpacing", Wrapped [ "Style.wordSpacing" ] ]
