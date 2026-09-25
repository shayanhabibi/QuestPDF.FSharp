(**
---
category: Recipes
categoryindex: 2
index: 3
---
*)
(*** hide ***)
#r "nuget: QuestPDF, 2026.9.0"
#r "../../src/FSharp.QuestPDF/bin/Release/net10.0/FSharp.QuestPDF.dll"
#r "../../src/FSharp.QuestPDF.Preview/bin/Release/net10.0/FSharp.QuestPDF.Preview.dll"

open System
open FSharp.QuestPDF

License.community ()
Font.useSystemFonts false
Font.strict true
Font.registerDirectory (IO.Path.Combine (__SOURCE_DIRECTORY__, "../..", "fonts"))

/// A loopback port that was free when the call returned.
let freePort () =
    let listener = new Net.Sockets.TcpListener (Net.IPAddress.Loopback, 0)
    listener.Start ()

    try
        (listener.LocalEndpoint :?> Net.IPEndPoint).Port
    finally
        listener.Stop ()

(**
# Recipe: a live preview on save

`FSharp.QuestPDF.Preview` serves the pages of a document function to a browser and renders them again when the code
changes. Under [SageFs](https://github.com/WillEhrendreich/SageFs), a save in any editor reloads the code: the page
updates in about half a second (0.25 to 0.6 s measured for both recipes), keeps its scroll position, and shows compile
errors and exceptions over the last good pages.

![The preview page with a compile error over the last good pages](../img/preview.png)

```shell
dotnet add package FSharp.QuestPDF.Preview
```

The runnable version of both recipes is `samples/HotReloadPreview` in the repository.

## The script recipe

A script is the quickest start: one `.fsx`, a `global.json` and a folder of fonts. Every save reloads the whole
script, so new types, changed signatures and new helpers all work.

Prerequisites: the .NET 10 SDK and `dotnet tool install --global SageFs`.

```shell
mkdir invoice && cd invoice
dotnet new globaljson --sdk-version 10.0.100 --roll-forward latestFeature
mkdir fonts          # copy the .ttf files you use, e.g. Lato-*.ttf (SIL OFL)
```

- **`global.json`** pins the SDK. Without it, a SageFs session can pick a preview SDK and fail at warm-up.
- **`fonts/`**: inside F# Interactive, QuestPDF's bundled Lato is not found, and the first render fails until a font
  is registered.

`invoice.fsx`:

```fsharp
#r "nuget: FSharp.QuestPDF.Preview, 0.1.0"

open FSharp.QuestPDF

License.community ()
Font.useSystemFonts false
Font.registerDirectory (System.IO.Path.Combine (__SOURCE_DIRECTORY__, "fonts"))

let invoice () =
    document [
        page [
            Page.size PageSizes.A5
            Page.margin (1 * cm)
            Page.content (column [
                styledText (Style.size 20 >> Style.bold) "Invoice #1"
                text "Thank you for your business."
            ])
        ]
    ]

Preview.Live invoice
```

Keep the version in the `#r` line. Every save loads the script again, and F# Interactive resolves a reference without
a version on every load: that took 1 to 5 s a reload in our measurements, and about a second more on the first
reload after an error. With the version, a reload took 0.25 to 0.4 s, after an error too.

1. **Start the daemon** with `sagefs`, or let the VS Code extension start it.
2. **Create a session on the folder** in the **REPL** workflow with **no projects**:
   - VS Code: open the folder; the extension creates the session.
   - Dashboard: http://localhost:37750/dashboard, create a session with the folder as the working directory, no
     projects and the REPL workflow.
   - MCP: `create_session {"working_directory": "<folder>", "projects": "", "workflow": "interactive"}`.
3. **Evaluate the file once**:
   - VS Code: *SageFs: Evaluate File*.
   - Dashboard REPL: `#load "invoice.fsx";;`.
   - MCP: `send_fsharp_code {"agentName": "<your name>", "working_directory": "<folder>", "code": "#load \"invoice.fsx\";;"}`.
     SageFs 0.6.828 refuses the call without `agentName`.

   Evaluating a selection is not enough, because `Preview.Live` needs the path of the file; the page says so. The
   evaluation prints:

   ```
   Preview: http://localhost:5800/
   Preview: reloading invoice.fsx through SageFs session 7ccbc644 on save
   ```

4. **Open the URL** and edit the script in any editor. On save the page updates.
   - A typo shows the compiler error over the dimmed last good pages. The banner names the full path of the file, the
     line and the column, such as `C:\work\invoice\invoice.fsx(16,40): The type 'int' does not match the type
     'Content'`.
   - An exception in `invoice ()` shows its message and stack trace.
   - Fixing either clears the banner.
   - Saving any `.fs` or `.fsx` file under the script's folder, such as a file the script `#load`s, reloads the
     script.
   - An error in a file the script `#load`s names that file, such as `C:\work\invoice\parts\header.fsx(5,23)`.
     SageFs reports no file name, so after a compile error the preview loads each file the script `#load`s on its own,
     in load order, until one fails; those loads run the top level of each file, as every reload does.
5. **Stop** by stopping the session, or with `Preview.stop 5800`.

## The project recipe

In an application, the preview re-renders a function of a compiled module while SageFs Hot Reload patches the
module on save.

1. **Create the app.** Add `global.json` first: `dotnet new console` targets the newest SDK installed, and with a
   preview SDK such as 11.0 installed, a project created before `global.json` targets `net11.0`. `dotnet add package`
   then fails with `NETSDK1045`.

   ```shell
   mkdir invoice-app && cd invoice-app
   dotnet new globaljson --sdk-version 10.0.100 --roll-forward latestFeature
   dotnet new console -lang F#
   dotnet add package FSharp.QuestPDF.Preview
   mkdir fonts          # copy the .ttf files you use, e.g. Lato-*.ttf (SIL OFL)
   ```

   A class library also needs `<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>` in its project file.
2. **Add `Invoice.fs`** with a module, so that `Invoice.build` names the document function:

   ```fsharp
   module Invoice

   open FSharp.QuestPDF

   let build () =
       document [
           page [
               Page.size PageSizes.A5
               Page.margin (1 * cm)
               Page.content (text "Thank you for your business.")
           ]
       ]
   ```

   F# compiles files in project order, so list it before `Program.fs` in `invoice-app.fsproj`:

   ```xml
   <ItemGroup>
     <Compile Include="Invoice.fs" />
     <Compile Include="Program.fs" />
   </ItemGroup>
   ```

3. **Build once** with `dotnet build`.
4. **Create a session** for the project in the **Hot Reload** workflow: *SageFs: Switch Workflow → Hot Reload* in
   VS Code, the workflow dropdown of the dashboard, or
   `create_session {"working_directory": "<folder>", "projects": "<folder>/invoice-app.fsproj", "workflow": "live"}`
   over MCP.
5. **Write `preview.fsx`** next to the project file:

   ```fsharp
   open FSharp.QuestPDF

   License.community ()
   Font.useSystemFonts false
   Font.registerDirectory (System.IO.Path.Combine (__SOURCE_DIRECTORY__, "fonts"))
   Preview.show Invoice.build
   ```

   `__SOURCE_DIRECTORY__` is the folder of `preview.fsx`. A relative path such as `"fonts"` resolves against the
   working directory of the session instead.
6. **Send `preview.fsx`** to the session by loading it, as in step 3 of the script recipe: *SageFs: Evaluate File*,
   `#load "preview.fsx";;` in the dashboard REPL, or `send_fsharp_code` with that code. It prints the URL and
   `Preview: SageFs session <sid>, watching <n> files`: the preview turns on SageFs file watching for the session,
   which is off in a new session.
7. **Edit and save `Invoice.fs`.** The page updates.
   - Keep the signature of `build`. A changed signature, a new type, or a helper that becomes generic needs a hard
     reset of the session and `preview.fsx` again.
   - A compile error leaves the last good pages up without a banner; the editor shows the error.
   - A hard reset starts a new process for the session. The old process serves the page until the rebuild finishes,
     then the page stops answering and watching is off. Send `preview.fsx` again: it starts a new server, whose page
     reloads itself, and turns watching back on. The number of watched files it prints can differ from the first
     send.

## What reloads

| Change | Script (`Preview.Live`) | Project (`Preview.show`) |
|--------|-------------------------|--------------------------|
| Function bodies and helpers | yes | yes |
| New or changed types | yes | needs a hard reset |
| The signature of the document function | yes | needs a hard reset |
| Compile errors | shown on the page | shown in the editor only |

## Without SageFs

The same script works in any F# Interactive: Ionide or Rider *Send File to F# Interactive*, or `dotnet fsi`. The
reload mode is then manual, and the page says "Automatic reload needs SageFs; send the file to FSI again". Sending
the file again updates the page at once, because `Preview.Live` replaces the document function of a running preview.

`Preview.serve` returns the running server, for code that wants its status. Here it serves a document on a free port
and reads the status of the first render:
*)

let build () =
    document [
        page [
            Page.size PageSizes.A6
            Page.margin (1 * cm)
            Page.content (text "Hello, preview")
        ]
    ]

let server =
    Preview.serve
        { Preview.defaults with
            Port = freePort ()
            Poll = None
            Reload = Preview.Manual }
        build

let status =
    match server.Status with
    | Preview.Rendered (pages, _) -> $"Rendered ({pages}, ...)"
    | other -> string other

(*** include-value: status ***)

(*** hide ***)
(server :> IDisposable).Dispose ()

(**
Pass the function, not a document: `let build () = document [...]`. A value `let build = document [...]` is built
once, and the page shows a hint when the function returns the same document on every call.

## Options

`Preview.LiveWith` and `Preview.serve` take `Preview.Options`; `Preview.defaults` holds the defaults.

| Field | Default | Meaning |
|-------|---------|---------|
| `Port` | `5800` | the port of `http://localhost:<port>/`; one preview per port |
| `Poll` | `Some 500 ms` | the shortest interval between re-renders while a page is open; `None` renders on reloads and `Refresh` only |
| `Reload` | `Auto` | `Auto` uses SageFs inside a SageFs session; `SageFs uri` names a daemon; `Manual` never reloads |
| `WatchProjectFiles` | `true` | turns on SageFs file watching for the session of a project preview |
| `Fonts` | `[]` | font files or folders served to the browser in addition to the `Font.register*` fonts |
| `OpenBrowser` | `false` | opens the page on the first start of a port |

The page draws the SVG of each page with the fonts of `Font.register*` and `Options.Fonts`, at the weights the PDF
uses. Its **PDF** link serves the PDF itself, for checks where the exact output matters.

## Endpoints

Scripts, CI checks and agents can read the preview over HTTP at `http://localhost:<port>`:

| Path | Content |
|------|---------|
| `/` | the page |
| `/snapshot` | the state as JSON, described below |
| `/events` | server-sent events: an event named `version` whose data is the `/snapshot` JSON, sent on connect and on each new version |
| `/page/<n>.svg` | page `n`, from 1, as SVG with its `@font-face` rules; `?h=<hash>` from `pages` makes it cacheable |
| `/font/<i>` | the font of the `i`th `@font-face` rule, from 0 |
| `/document.pdf` | the PDF of the current document function, rendered on request |

The `/snapshot` JSON:

| Field | Meaning |
|-------|---------|
| `instance` | an id of the running server; it changes when the server starts again, such as after a hard reset |
| `version` | increases when the pages, the status or the hint change |
| `pages` | the content hash of each page, in order |
| `status` | an object whose `kind` is `starting`, `rendered` (with `pages` and `renderMs`), `renderFailed` (with `error`), `compileFailed` (with `diagnostics`: `file`, `line`, `column`, `message`) or `reloadFailed` (with `reason`) |
| `hint` | the text of the hint banner, or `null` |
| `reload` | `sagefs-script`, `sagefs-project` or `manual` |

For example, `curl -s http://localhost:5800/snapshot` right after a typo in a loaded file returns a status such as
`{"kind":"compileFailed","diagnostics":[{"file":"C:\\work\\invoice\\parts\\header.fsx","line":5,"column":23,"message":"..."}]}`.

## Troubleshooting

See the [gotchas](../gotchas.html): pass a function rather than a document, keep the top level of a live script
cheap, pin the package version in a live script, turn on file watching for a project session, send `preview.fsx`
again after a hard reset, annotate a helper that is still `failwith "todo"`, pin the SDK with `global.json` before
`dotnet new`, and give a class library `CopyLocalLockFileAssemblies`.
*)
