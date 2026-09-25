---
category: Reference
categoryindex: 3
index: 1
---
# Gotchas

- **In a parts list, loop with `for ... do`, not `for ... ->`.** `->` is an explicit yield, and one explicit yield
  turns off the implicit yield of the whole list: in `column [ text "head"; for p in body -> text p ]` the heading
  is discarded. `Content` is a function, so the discarded item raises only warning FS0193, `This expression is a
  function value, i.e. is missing arguments`. `for p in body do text p` keeps every item. With
  `<WarningsAsErrors>FS0193</WarningsAsErrors>` (or `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`) in the
  project file, the warning is an error.
- **Scripts register a font before generating.** QuestPDF's default family, Lato, is copied next to a compiled
  application only. In F# Interactive and `dotnet fsi` scripts, generation raises `DocumentDrawingException` with
  `font families that are not available: 'Lato'`. Call `Font.registerDirectory "path/to/fonts"` (or
  `Font.registerFile`) first, and set a family other than Lato with `Style.family`.
- **A function of your own that passes a parameter to a length or number argument is `inline`.** Without `inline`,
  a definition with no use fails with FS0071, `Type constraint mismatch when applying the default type 'obj' for a
  type inference variable. No overloads match for method 'ToLength'. Known type parameters: < LengthWitness , obj >`
  (`ToFloat` and `NumberWitness` for a size, weight or angle), and a definition with a use is fixed to the type of
  its first argument. Write `let inline boxed pad label = padding pad >> text label`, or annotate the parameter as
  `Length` and pass `len 4` or `4 * mm`.
- **Signatures show `^a`.** A length argument accepts `int`, `int64`, `float`, `float32` or `decimal` (points) or
  a `Length`; a size, weight, angle, scale or ratio accepts `int`, `int64`, `float`, `float32` or `decimal`.
- **A part of the wrong kind in a list gives an error in terms of QuestPDF types.** Content placed straight in a
  `page`, `row` or `table` list gives FS0001, `All elements of a list must be implicitly convertible to the type of
  the first element, which here is 'Slot'. This element has type 'QuestPDF.Fluent.PageDescriptor'` (or
  `RowDescriptor`, `TableDescriptor`): wrap the content in `Page.content`, `Row.fill`, `Table.cell` and the like.
  `... which here is 'unit'. This element has type 'Slot'` is a modifier chain with no content at its end: end it in
  content, or in `empty` for a box without content.
- **A negative number needs parentheses.** `rotate -20` is a subtraction; write `rotate (-20)`.
- **`Text.lineBreak` and `Text.emptyLine` are the same line break.** A blank line takes two breaks in a row, such as
  `Text.line "a"; Text.emptyLine` or two `Text.lineBreak` parts.
- **`Image` is the wrapper module.** The QuestPDF image class is `QuestPDF.Infrastructure.Image`, the type
  `Image.shared` takes.
- **Some mistakes fail at generation, not at compile time.** Two `Page.content` parts raise
  `DocumentComposeException`, `layers` without `Layers.primary` raises, a table cell placed where it cannot fit
  raises, and content that cannot fit raises `DocumentLayoutException`.
- **Documents are lazy.** Side effects inside `Content` run again on every `Pdf.*` call.
- **The dates are fixed when `document` runs.** Without `Meta.dated`, generating the same document value twice gives
  the same bytes, and building it again a second later does not. `Meta.dated` makes every build reproducible.
- **A partial application of `Pdf.images`, `Pdf.save` or `Pdf.write` at the top level needs a type annotation.**
  `let render = Pdf.images ImageFormat.Png 72` is a value restriction error, because F# makes the interface
  parameter flexible; write `let render: IDocument -> byte[] list = Pdf.images ImageFormat.Png 72`, or take the
  document as a parameter.
- **`Output.rightToLeft` applies to every page.** `Page.leftToRight` restores left to right for one page.

## Live previews

- **Pass a function to the preview, not a document.** `let invoice = document [...]` is built once and never
  changes; write `let invoice () = document [...]`. The preview shows a hint when the function returns the same
  document on every call.
- **A SageFs session reloads only the files it watches.** Project sessions start with watching off. `Preview.show`
  turns it on for the session; after a hard reset, send `preview.fsx` again.
- **`let title () = failwith "todo"` has type `unit -> 'a`.** Under SageFs Hot Reload, fixing it later is a signature
  change that its callers do not see. Annotate the result: `let title () : string = failwith "todo"`.
- **Scripts under `Preview.Live` run their top level on every save.** Keep it cheap and repeatable: the license, the
  fonts and `Preview.Live`. `Font.register*` ignores a path or font data it has already registered.
- **`Pdf.companion` blocks until the Companion app closes.** Do not call it from a REPL you want to keep using; the
  [live preview](recipes/hot-reload-preview.html) does not block.
- **A class library needs `<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>`** to load in a SageFs
  session. Without it, QuestPDF's native library is missing, `QuestPDF.Settings` fails to initialise, and the session
  needs a hard reset.
- **Pin the SDK with `global.json`** in folders you open in SageFs. Without it, a session can pick a preview SDK and
  fail at warm-up.
