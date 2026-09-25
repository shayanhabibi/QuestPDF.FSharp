/// Font files for tests that register fonts.
[<AutoOpen>]
module FSharp.QuestPDF.Tests.Support.Fonts

open System
open System.IO

/// Lato Regular from the repository fonts, renamed to a four-letter family QuestPDF does not ship. A test that
/// registers its own family leaves the registrations of other tests unaffected.
let renamedLato (family: string) : byte[] =
    let path =
        Path.Combine (__SOURCE_DIRECTORY__, "..", "..", "..", "fonts", "Lato-Regular.ttf")

    let replace (find: byte[]) (by: byte[]) (data: byte[]) =
        let data = Array.copy data

        for i in 0 .. data.Length - find.Length do
            if data.AsSpan(i, find.Length).SequenceEqual (ReadOnlySpan find) then
                Array.blit by 0 data i by.Length

        data

    File.ReadAllBytes path
    |> replace (Text.Encoding.BigEndianUnicode.GetBytes "Lato") (Text.Encoding.BigEndianUnicode.GetBytes family)
    |> replace (Text.Encoding.ASCII.GetBytes "Lato") (Text.Encoding.ASCII.GetBytes family)
