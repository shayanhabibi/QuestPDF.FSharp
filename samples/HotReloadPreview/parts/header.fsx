// Loaded by invoice.fsx: saving this file reloads invoice.fsx.
module Header

open QuestPDF.FSharp

/// The page header of an invoice number.
let title (number: int) =
    styledText (Style.size 20 >> Style.bold >> Style.color Colors.Blue.Darken2) $"Invoice #{number}"
