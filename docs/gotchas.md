# Gotchas

- **Open `QuestPDF.FSharp` last.** `QuestPDF.Fluent` and `QuestPDF.Infrastructure` define names such as `Image`
  and `Document`; the last open wins.
- **`Image` is the wrapper module.** The QuestPDF image class is `QuestPDF.Infrastructure.Image`.
- **Some mistakes fail at generation, not at compile time.** Two `Page.content` parts raise
  `DocumentComposeException`, `layers` without `Layers.primary` raises, a table cell placed where it cannot fit
  raises, and content that cannot fit raises `DocumentLayoutException`.
- **Signatures show `^a`.** A length argument accepts `int`, `float`, `float32` or `Length`; a size, weight or
  angle accepts any numeric type.
- **A negative number needs parentheses.** `rotate -20` is a subtraction; write `rotate (-20)`.
- **`Text.lineBreak` is not `Text.emptyLine`.** `lineBreak` ends the current line; `emptyLine` adds a blank line.
- **Documents are lazy.** Side effects inside `Content` run again on every `Pdf.*` call.
- **The dates are fixed when `document` runs.** Without `Meta.dated`, generating the same document value twice gives
  the same bytes, and building it again a second later does not. `Meta.dated` makes every build reproducible.
- **A partial application of `Pdf.images`, `Pdf.save` or `Pdf.write` at the top level needs a type annotation.**
  `let render = Pdf.images ImageFormat.Png 72` is a value restriction error, because F# makes the interface
  parameter flexible; write `let render: IDocument -> byte[] list = Pdf.images ImageFormat.Png 72`, or take the
  document as a parameter.
- **`Output.rightToLeft` applies to every page.** `Page.leftToRight` restores left to right for one page.
