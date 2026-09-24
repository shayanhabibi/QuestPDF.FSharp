# Gotchas

- **Open `QuestPDF.FSharp` last.** `QuestPDF.Fluent` and `QuestPDF.Infrastructure` define names such as `Image`
  and `Document`; the last open wins.
- **`Image` is the wrapper module.** The QuestPDF image class is `QuestPDF.Infrastructure.Image`.
- **Some mistakes fail at generation, not at compile time.** Two `Page.content` parts raise
  `DocumentComposeException`, `layers` without `Layers.primary` raises, and content that cannot fit raises
  `DocumentLayoutException`.
- **Signatures show `^a`.** A length argument accepts `int`, `float`, `float32` or `Length`; a size, weight or
  angle accepts any numeric type.
- **`Text.lineBreak` is not `Text.emptyLine`.** `lineBreak` ends the current line; `emptyLine` adds a blank line.
- **Documents are lazy.** Side effects inside `Content` run again on every `Pdf.*` call.
- **Without `Meta.dated` the bytes change every second**, because QuestPDF stamps the current time.
- **`Output.rightToLeft` leaves pages unchanged** in QuestPDF 2026.9.0; use `Page.rightToLeft` for a right-to-left
  page.
