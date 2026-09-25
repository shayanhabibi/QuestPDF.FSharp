namespace FSharp.QuestPDF.PreviewServer

open System.IO

[<RequireQualifiedAccess>]
module internal Shell =
    /// The page served at /: a status bar, a banner and one SVG object per page, updated from /events.
    let html: Lazy<byte[]> =
        lazy
            (let stream = typeof<Snapshot>.Assembly.GetManifestResourceStream "Shell.html"

             let bytes = new MemoryStream ()

             try
                 stream.CopyTo bytes
                 bytes.ToArray ()
             finally
                 stream.Dispose ()
                 bytes.Dispose ())
