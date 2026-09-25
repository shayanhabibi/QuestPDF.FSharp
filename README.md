# QuestPDF.FSharp

An idiomatic F# layer over [QuestPDF](https://www.questpdf.com/) 2026.9.0 (net10.0). Layouts are plain
functions composed with `>>`, containers take lists, lengths are bare numbers or `5 * mm`, and the output is
byte-identical to the equivalent fluent QuestPDF code.

## Quick start

```fsharp
open QuestPDF.FSharp

License.community ()

document [
    page [
        Page.size PageSizes.A4
        Page.margin (2 * cm)
        Page.content (padding 10 >> background Colors.Grey.Lighten3 >> styledText (Style.size 20) "Hello, world!")
    ]
]
|> Pdf.save "hello.pdf"
```

In F# Interactive or a `dotnet fsi` script, register a font before generating: QuestPDF's default font, Lato, is
copied next to a compiled application only, and without it generation raises `DocumentDrawingException`.

```fsharp
Font.registerDirectory "path/to/fonts"   // a folder of .ttf or .otf files; Font.registerFile takes one file
```

- A `Content` fills a slot and a `Modifier` wraps one: `padding 10 >> background c >> text "x"` reads outer to
  inner, like the fluent chain.
- `column`, `row`, `table`, `layers`, `decoration`, `page` and `document` take lists, so `for ... do`, `if` and
  `match` work inside them. Loop with `for ... do`: a `for ... ->` drops the other items of the list.
- Lengths accept `int`, `int64`, `float`, `float32` or `decimal` (points) or a `Length` such as `5 * mm` or
  `2.5 * cm`.
- `open QuestPDF.FSharp` is the only open needed. Raw interop code also opens `QuestPDF.Fluent` and
  `QuestPDF.Infrastructure`, in any order.
- The license is never set implicitly: call `License.community ()` (or `professional`/`enterprise`) first.

## Features

| Area | API |
|------|-----|
| Text | `text`, `styledText`, `richText` with `Text.span`/`styled`/`link`/`sectionLink`/`pageNumber`/`totalPages`/`formatPage`, alignment, clamping, paragraph spacing |
| Style | `Style.size`/`color`/`family`/`bold`/`italic`/`underline`/... composed with `>>`; `withStyle` on any span |
| Box modifiers | padding, alignment, width/height constraints, extend/shrink, aspect ratio, border, corner radius, background |
| Layout | `column`, `columnSpaced`, `row` (`Row.fill`/`relative`/`constant`/`auto`), `table` (`Table.*`, `Cell.*`), `layers`, `decoration` |
| Elements | `lineH`/`lineV`, `Image.file`/`bytes`/`shared` and their `With` forms, `Svg.text`, `pageBreak`, `placeholder`, `empty` |
| Paging | `showOnce`, `skipOnce`, `repeat`, `ensureSpace`, `preventPageBreak`, `showEntire`, `showWhen`, sections and links |
| Transforms | `rotate`, `scale`, `flipH`/`flipV`, `offsetX`/`offsetY`, `zIndex` |
| Pages | `Page.size`/`sizeOf`/`minSize`/`maxSize`/`continuous`, margins, colour, header/content/footer, background/foreground, `rightToLeft`/`leftToRight` |
| Document | `Meta.*` metadata (`Meta.dated` pins both dates for reproducible bytes), `Output.*` settings: `pdfA`, `pdfUA`, `compress`, `imageQuality`, `imageDpi`, `rightToLeft` |
| Generation | `Pdf.bytes`/`save`/`write`/`show`, `Pdf.images` (PNG/JPEG/WebP per page), `Pdf.svgs`, `Pdf.companion` |
| Live preview | Live preview on save (`QuestPDF.FSharp.Preview`, SageFs): `Preview.Live`, `Preview.show`, `Preview.serve`; see the [recipe](https://shayanhabibi.github.io/QuestPDF.FSharp/recipes/hot-reload-preview.html) |
| Setup | `License.*`, `Font.registerFile`/`registerDirectory`/`registerBytes`/`useSystemFonts`/`strict`/`registered` |
| Interop | `raw`, `fluent` and `modify` lift fluent code; `Content.run` mounts wrapper content in raw code; `Text.raw`; a descriptor lambda is a valid part of `page`, `row`, `table`, `Table.columns`, `layers` and `decoration` |

Every public method of the `QuestPDF.Fluent` and `QuestPDF.Companion` types (the descriptors, `Document`,
`DocumentOperation` and the extension classes) and of the QuestPDF extension classes in other namespaces, such as
`PageSizeExtensions`, is mapped to a wrapper function or to the raw escape hatch in
`tests/QuestPDF.FSharp.Tests/Coverage.fs`. A reflection test fails when a QuestPDF upgrade adds an unmapped method.
The [interop page](https://shayanhabibi.github.io/QuestPDF.FSharp/interop.html) renders the table.

## Build CLI

Every repository task runs through `build.fsx`, a [Partas.Build](https://github.com/shayanhabibi/partas.build)
script, so the tasks are typed, composable and discoverable:

```shell
dotnet fsi build.fsx -- --help
```

| Command | What it does |
|---------|--------------|
| `build` | Restores and builds the source projects and the samples |
| `test` | Cleans, then runs the Expecto suite (`--skip-tests` to skip it) |
| `format` | Formats every source file with Fantomas (`--dry-format` checks instead) |
| `publish` | Builds, tests, packs and pushes to NuGet (`--api-key`, or the `NUGET_API_KEY` env var) |
| `bump` | Bumps the version of a project |
| `preview-e2e` | Runs the SageFs end-to-end checks of `QuestPDF.FSharp.Preview` against a SageFs daemon on port 37749 |
| `docs` | Builds the fsdocs site from `docs/`, evaluating every page, and fails on a snippet that does not compile or run (`--watch` to serve it) |

Global flags: `--quick` skips restores and cleaning,
`--format` formats before building, `--dry-format` checks formatting before building,
`-c` picks the configuration (default `Release`).

## Layout

```
build.fsx                      the build CLI
build/                         build helpers loaded by build.fsx, such as the API reference link rewrite
src/QuestPDF.FSharp/           the library
src/QuestPDF.FSharp.Preview/   live browser previews reloaded on save under SageFs
tests/QuestPDF.FSharp.Tests/   the Expecto suite
tests/QuestPDF.FSharp.Preview.Tests/  the preview suite; its SageFs end-to-end checks run with preview-e2e
samples/                       runnable samples, built by the build command: HotReloadPreview
docs/                          fsdocs pages: literate .fsx scripts evaluated by the docs build
fonts/                         Lato (SIL OFL 1.1), registered by the docs pages for their page images
```

### Adding a project

The build CLI addresses the repository through `Partas.TypeProvider.BuildHelper`,
so projects are discovered at compile time: anything under `src/` is a source
project, anything under `tests/` is a test project, and anything under `samples/` is a sample that `build` compiles
and `pack` skips. Add the project to the
solution and it is picked up by `build`, `test`
and `pack`
on the next run.

### Adding a step

A step is a stage of a command. A stage that needs a flag binds it in an
`input { }` block, which is also what puts the flag into `--help`:

```fsharp
let myStep = input {
    let! quick = Options.quick
    return stage "my step" {
        when' (not quick)
        run "dotnet ..."
    }
}
```

Add it to any `command "..." { }` block. Because the condition lives in the
stage, the command carries no flags of its own, and adding the stage to a
second command registers `--quick` there too.
