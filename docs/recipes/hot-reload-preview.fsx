(**
---
category: Recipes
categoryindex: 2
index: 3
---
*)
(*** hide ***)
#r "nuget: QuestPDF, 2026.9.0"
#r "../../src/QuestPDF.FSharp/bin/Release/net10.0/QuestPDF.FSharp.dll"
#r "../../src/QuestPDF.FSharp.Preview/bin/Release/net10.0/QuestPDF.FSharp.Preview.dll"

open System
open QuestPDF.FSharp

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

`QuestPDF.FSharp.Preview` serves the pages of a document function to a browser and renders them again when the code
changes. Under [SageFs](https://github.com/WillEhrendreich/SageFs), a save in any editor reloads the code: the page
updates in about half a second, keeps its scroll position, and shows compile errors and exceptions over the last good
pages.

![The preview page with a compile error over the last good pages](../img/preview.png)

```shell
dotnet add package QuestPDF.FSharp.Preview
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
#r "nuget: QuestPDF.FSharp.Preview"

open QuestPDF.FSharp

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

1. **Start the daemon** with `sagefs`, or let the VS Code extension start it.
2. **Create a session on the folder** in the **REPL** workflow with **no projects**:
   - VS Code: open the folder; the extension creates the session.
   - Dashboard: http://localhost:37750/dashboard, create a session with the folder as the working directory, no
     projects and the REPL workflow.
   - MCP: `create_session {"working_directory": "<folder>", "projects": "", "workflow": "interactive"}`.
3. **Evaluate the file once**: *SageFs: Evaluate File* in VS Code, or `#load "invoice.fsx";;` in the dashboard REPL.
   Evaluating a selection is not enough, because `Preview.Live` needs the path of the file; the page says so. The
   evaluation prints:

   ```
   Preview: http://localhost:5800/
   Preview: reloading invoice.fsx through SageFs session 7ccbc644 on save
   ```

4. **Open the URL** and edit the script in any editor. On save the page updates.
   - A typo shows the compiler error, such as `invoice.fsx(14,40): The type 'int' does not match...`, over the
     dimmed last good pages.
   - An exception in `invoice ()` shows its message and stack trace.
   - Fixing either clears the banner.
   - Saving any `.fs` or `.fsx` file under the script's folder, such as a file the script `#load`s, reloads the
     script.
5. **Stop** by stopping the session, or with `Preview.stop 5800`.

## The project recipe

In an application, the preview re-renders a function of a compiled module while SageFs Hot Reload patches the
module on save.

1. **Create the app** with `dotnet new console -lang F#`. A class library also needs
   `<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>` in its project file. Then:
   - add a `global.json`;
   - `dotnet add package QuestPDF.FSharp.Preview`;
   - put `let build () = document [...]` in `Invoice.fs`.
2. **Build once** with `dotnet build`.
3. **Create a session** for the project in the **Hot Reload** workflow: *SageFs: Switch Workflow → Hot Reload* in
   VS Code, the workflow dropdown of the dashboard, or `workflow: "live"` over MCP.
4. **Send `preview.fsx`** to the session:

   ```fsharp
   open QuestPDF.FSharp

   License.community ()
   Font.registerDirectory "path/to/fonts"
   Preview.show Invoice.build
   ```

   It prints the URL and `Preview: SageFs session <sid>, watching <n> files`: the preview turns on SageFs file
   watching for the session, which is off in a new session.
5. **Edit and save `Invoice.fs`.** The page updates.
   - Keep the signature of `build`. A changed signature, a new type, or a helper that becomes generic needs a hard
     reset of the session and `preview.fsx` again.
   - A compile error leaves the last good pages up without a banner; the editor shows the error.
   - A hard reset clears the watched files of the session. Sending `preview.fsx` again turns watching back on.

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

The page draws the SVG of each page with the fonts of `Font.register*` and `Options.Fonts`. Its **PDF** link serves
the PDF itself, for checks where the exact output matters.

## Troubleshooting

See the [gotchas](../gotchas.html): pass a function rather than a document, keep the top level of a live script
cheap, turn on file watching for a project session, annotate a helper that is still `failwith "todo"`, pin the SDK
with `global.json`, and give a class library `CopyLocalLockFileAssemblies`.
*)
