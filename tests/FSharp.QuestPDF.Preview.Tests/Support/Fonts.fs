/// The repository fonts and the QuestPDF settings of the render tests.
[<AutoOpen>]
module FSharp.QuestPDF.Preview.Tests.Support.Fonts

open System
open System.IO
open QuestPDF.Infrastructure
open FSharp.QuestPDF

/// The fonts directory of the repository.
let fontsDirectory =
    Path.GetFullPath (Path.Combine (__SOURCE_DIRECTORY__, "..", "..", "..", "fonts"))

/// A font file of the repository fonts, by name.
let repoFont (name: string) =
    Path.Combine (fontsDirectory, name)

/// Sets the Community license, disables system fonts, makes missing fonts and glyphs throw, and registers the
/// repository fonts.
let configure () =
    QuestPDF.Settings.License <- Nullable LicenseType.Community
    QuestPDF.Settings.UseSystemFonts <- false
    QuestPDF.Settings.ThrowOnMissingFontFamilies <- true
    QuestPDF.Settings.ThrowOnMissingTextGlyphs <- true
    Font.registerDirectory fontsDirectory

/// A new empty directory under the temporary directory.
let tempDirectory () =
    Directory.CreateDirectory(Path.Combine (Path.GetTempPath (), $"qpdf-preview-{Guid.NewGuid ():N}")).FullName
