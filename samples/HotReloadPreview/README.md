# HotReloadPreview

The two recipes of [a live preview on save](https://shayanhabibi.github.io/QuestPDF.FSharp/recipes/hot-reload-preview.html),
runnable in this repository. Build the repository first (`dotnet fsi build.fsx -- build`), and start the SageFs
daemon (`sagefs`).

## Script: `invoice.fsx`

1. Create a SageFs session on this folder in the REPL workflow with no projects, for example with the MCP call
   `create_session {"working_directory": "<this folder>", "projects": "", "workflow": "interactive"}`.
2. Evaluate the file once: `#load "invoice.fsx";;`.
3. Open http://localhost:5800/ and edit `invoice.fsx` or `parts/header.fsx`. Every save reloads the script; a
   compile error shows over the last good pages.

Outside this repository, replace the `#r` lines of `invoice.fsx` with `#r "nuget: QuestPDF.FSharp.Preview, 1.0.0"`.
Keep the version: a reference without one is resolved again on every reload.

## Project: `HotReloadPreview.fsproj`

1. `dotnet build`.
2. Create a SageFs session for `HotReloadPreview.fsproj` in the Hot Reload workflow (`workflow: "live"`).
3. Send `preview.fsx` to the session. It prints the URL and turns on file watching for the session.
4. Open http://localhost:5800/ and edit a body or a helper in `Invoice.fs`. Keep the signature of `Invoice.build`.

`dotnet run` writes `invoice.pdf` without a preview.
