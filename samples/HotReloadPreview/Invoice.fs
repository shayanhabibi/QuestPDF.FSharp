/// An invoice for the project recipe: edit a body or a helper, save, and the preview re-renders.
module Invoice

open QuestPDF.Fluent
open QuestPDF.FSharp

type Line =
    { Item: string
      Quantity: int
      Price: decimal }

let lines =
    [ { Item = "Design"
        Quantity = 12
        Price = 80m }
      { Item = "Development"
        Quantity = 40
        Price = 95m }
      { Item = "Hosting"
        Quantity = 1
        Price = 120m } ]

let money (amount: decimal) : string =
    amount.ToString ("N2", System.Globalization.CultureInfo.InvariantCulture)

let cell (value: string) =
    Table.cell (
        borderBottom 0.5
        >> borderColor Colors.Grey.Lighten2
        >> padding 4
        >> text value
    )

let header (value: string) =
    Table.cell (
        background Colors.Grey.Lighten3
        >> padding 4
        >> styledText Style.semiBold value
    )

/// The invoice. Keep the signature: the preview holds on to this function.
let build () : Document =
    let total =
        lines
        |> List.sumBy (fun line -> decimal line.Quantity * line.Price)

    document
        [ page
              [ Page.size PageSizes.A5
                Page.margin (1 * cm)
                Page.textStyle (Style.size 10)
                Page.header (
                    styledText
                        (Style.size 20
                         >> Style.bold
                         >> Style.color Colors.Blue.Darken2)
                        "Invoice #1"
                )
                Page.content (
                    paddingV (5 * mm)
                    >> table
                        [ Table.columns [ Table.relative 4; Table.relative 1; Table.relative 1.5 ]
                          Table.header [ header "Item"; header "Qty"; header "Amount" ]
                          for line in lines do
                              Table.cells
                                  [ cell line.Item
                                    cell (string line.Quantity)
                                    cell (money (decimal line.Quantity * line.Price)) ]
                          Table.footer
                              [ Table.cellWith
                                    [ Cell.columnSpan 3 ]
                                    (alignRight
                                     >> padding 4
                                     >> styledText Style.bold $"Total {money total}") ] ]
                )
                Page.footer (alignCenter >> text "Thank you for your business.") ] ]
