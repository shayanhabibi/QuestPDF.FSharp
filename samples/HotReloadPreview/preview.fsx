// The project recipe: send this file to a SageFs Hot Reload session of HotReloadPreview.fsproj, open
// http://localhost:5800/ and edit Invoice.fs. Every save patches Invoice.build and the page re-renders.
open QuestPDF.FSharp

License.community ()
Font.useSystemFonts false
Font.registerDirectory (System.IO.Path.Combine (__SOURCE_DIRECTORY__, "../../fonts"))

Preview.show Invoice.build
