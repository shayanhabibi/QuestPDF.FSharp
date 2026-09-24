/// Pinned settings, metadata and document builders for byte-comparable PDFs.
[<AutoOpen>]
module QuestPDF.FSharp.Tests.Support.Deterministic

open System
open System.Text
open System.Text.RegularExpressions
open QuestPDF.Fluent
open QuestPDF.Helpers
open QuestPDF.Infrastructure
open QuestPDF.FSharp

/// The creation and modification date of every pinned document: 2026-01-02T03:04:05Z.
let fixedDate = DateTimeOffset (2026, 1, 2, 3, 4, 5, TimeSpan.Zero)

/// Sets the Community license, disables system fonts and makes missing fonts and glyphs throw.
let configure () =
    QuestPDF.Settings.License <- Nullable LicenseType.Community
    QuestPDF.Settings.UseSystemFonts <- false
    QuestPDF.Settings.ThrowOnMissingFontFamilies <- true
    QuestPDF.Settings.ThrowOnMissingTextGlyphs <- true

/// Metadata with both dates set to fixedDate.
let pinnedMetadata () =
    DocumentMetadata (CreationDate = fixedDate, ModifiedDate = fixedDate)

/// A raw QuestPDF document with pinned metadata.
let rawDocument (compose: IDocumentContainer -> unit) : IDocument =
    Document.Create(fun container -> compose container).WithMetadata (pinnedMetadata ())

/// A raw QuestPDF document with a single page.
let rawPage (build: PageDescriptor -> unit) : IDocument =
    rawDocument (fun container -> container.Page (fun page -> build page) |> ignore)

/// A raw A5 page with a 20 pt margin; the body fills the content slot.
let rawContent (body: IContainer -> unit) : IDocument =
    rawPage (fun page ->
        page.Size PageSizes.A5
        page.Margin 20f
        body (page.Content ()))

/// A wrapper document with pinned dates and a single page.
let wrapPage (parts: PagePart list) : IDocument =
    document [ Meta.dated fixedDate; page parts ]

/// The wrapper counterpart of rawContent.
let wrapContent (content: Content) : IDocument =
    wrapPage [ Page.size PageSizes.A5; Page.margin 20; Page.content content ]

let private zeroHex (value: string) =
    Regex.Replace (value, "[0-9A-Fa-f]", "0")

let private zeroAngleHex (m: Match) =
    Regex.Replace (m.Value, "<[0-9A-Fa-f]*>", fun hex -> "<" + zeroHex (hex.Value.Trim ('<', '>')) + ">")

/// Zeroes the XMP uuid and the trailer /ID of a PDF/A or PDF/UA file; the length is preserved.
let normalizeIds (pdf: byte[]) : byte[] =
    let text = Encoding.Latin1.GetString pdf

    let text =
        Regex.Replace (text, @"uuid:[0-9A-Fa-f\-]{36}", fun m -> "uuid:" + zeroHex (m.Value.Substring 5))

    let text =
        Regex.Replace (text, @"/ID\s*\[\s*<[0-9A-Fa-f]*>\s*<[0-9A-Fa-f]*>\s*\]", zeroAngleHex)

    Encoding.Latin1.GetBytes text
