namespace QuestPDF.FSharp.PreviewServer

open System
open System.IO
open System.Text
open QuestPDF.FSharp

/// The CSS face of a font file: the typographic family, the weight class and the italic flag.
type internal FontFace =
    { Family: string
      Weight: int
      Italic: bool }

/// The contents of a served font.
type internal FontContent =
    | FromFile of path: string
    | FromData of data: byte[]

/// A font served to the browser at /font/<index>.
type internal ServedFont =
    { Face: FontFace; Content: FontContent }

[<RequireQualifiedAccess>]
module internal FontFace =
    let private u16 (data: byte[]) (offset: int) =
        (int data[offset] <<< 8) ||| int data[offset + 1]

    let private u32 (data: byte[]) (offset: int) =
        (u16 data offset <<< 16) ||| u16 data (offset + 2)

    /// The offset of a table of an sfnt file.
    let private table (data: byte[]) (tag: string) =
        [ 0 .. u16 data 4 - 1 ]
        |> List.tryPick (fun i ->
            let record = 12 + 16 * i

            if Encoding.ASCII.GetString (data, record, 4) = tag then
                Some (u32 data (record + 8))
            else
                None)

    /// The name records of a name table, as (name ID, text).
    let private names (data: byte[]) (offset: int) =
        let strings = offset + u16 data (offset + 4)

        [ for i in 0 .. u16 data (offset + 2) - 1 do
              let record = offset + 6 + 12 * i
              let platform = u16 data record
              let id = u16 data (record + 6)
              let length = u16 data (record + 8)
              let start = strings + u16 data (record + 10)

              let text =
                  if platform = 0 || platform = 3 then
                      Encoding.BigEndianUnicode.GetString (data, start, length)
                  else
                      Encoding.Latin1.GetString (data, start, length)

              id, text ]

    let private isSfnt (data: byte[]) =
        data.Length >= 12
        && (u32 data 0 = 0x00010000
            || Encoding.ASCII.GetString (data, 0, 4) = "OTTO"
            || Encoding.ASCII.GetString (data, 0, 4) = "true")

    /// The face of the contents of a TrueType or OpenType font file; None for other data.
    let read (data: byte[]) : FontFace option =
        try
            if not (isSfnt data) then
                None
            else
                match table data "OS/2", table data "name" with
                | Some os2, Some name ->
                    let records = names data name

                    [ 16; 1 ]
                    |> List.tryPick (fun id ->
                        records
                        |> List.tryFind (fst >> (=) id)
                        |> Option.map snd)
                    |> Option.map (fun family ->
                        { Family = family
                          Weight = u16 data (os2 + 4)
                          Italic = (u16 data (os2 + 62) &&& 0x201) <> 0 })
                | _ -> None
        with
        | :? ArgumentException
        | :? IndexOutOfRangeException -> None

    /// The face of a font file; None for a file that is not a TrueType or OpenType font.
    let readFile (path: string) : FontFace option =
        try
            read (File.ReadAllBytes path)
        with
        | :? IOException
        | :? UnauthorizedAccessException -> None

    let private isFontFile (path: string) =
        let extension = Path.GetExtension(path).ToLowerInvariant ()
        extension = ".ttf" || extension = ".otf"

    let private key (content: FontContent) =
        match content with
        | FromFile path -> "file:" + path.ToUpperInvariant ()
        | FromData data ->
            "data:"
            + Convert.ToHexString (Security.Cryptography.SHA256.HashData data)

    let private contents (source: FontSource) =
        match source with
        | FontFile path -> [ FromFile (Path.GetFullPath path) ]
        | FontDirectory path when Directory.Exists path ->
            Directory.GetFiles path
            |> Array.filter isFontFile
            |> Array.sortWith (fun a b -> String.CompareOrdinal (a, b))
            |> Array.map (Path.GetFullPath >> FromFile)
            |> List.ofArray
        | FontDirectory _ -> []
        | FontData data -> [ FromData data ]

    /// The served fonts of registrations, one per distinct file or content, in registration order. A directory
    /// contributes its .ttf and .otf files in name order; files that are not fonts are left out.
    let expand (sources: FontSource list) : ServedFont list =
        sources
        |> List.collect contents
        |> List.distinctBy key
        |> List.choose (fun content ->
            let face =
                match content with
                | FromFile path -> readFile path
                | FromData data -> read data

            face
            |> Option.map (fun face -> { Face = face; Content = content }))

    /// One @font-face rule per served font, whose source is /font/<index>.
    let css (fonts: ServedFont list) : string =
        fonts
        |> List.mapi (fun i font ->
            let family = font.Face.Family.Replace("\\", "\\\\").Replace ("\"", "\\\"")
            let style = if font.Face.Italic then "italic" else "normal"

            $"@font-face{{font-family:\"{family}\";font-weight:{font.Face.Weight};font-style:{style};src:url(/font/{i});font-display:block}}")
        |> String.concat ""
