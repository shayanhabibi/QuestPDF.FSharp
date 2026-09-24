module QuestPDF.FSharp.Tests.DocsTests

open System
open System.IO
open System.Reflection
open System.Text.RegularExpressions
open Expecto
open QuestPDF.FSharp

/// The documentation file of QuestPDF.FSharp: $QUESTPDF_FSHARP_DOCS_XML when set, otherwise the file beside the
/// assembly.
let private docsXml () =
    match
        Environment.GetEnvironmentVariable "QUESTPDF_FSHARP_DOCS_XML"
        |> Option.ofObj
    with
    | Some path -> path
    | None -> Path.ChangeExtension (typeof<Length>.Assembly.Location, ".xml")

/// The documentation ID of a type in a member signature.
let rec private typeId (t: Type) : string =
    if t.IsByRef then
        typeId (t.GetElementType ()) + "@"
    elif t.IsArray then
        typeId (t.GetElementType ()) + "[]"
    elif t.IsGenericParameter then
        (if isNull t.DeclaringMethod then "`" else "``")
        + string t.GenericParameterPosition
    elif t.IsGenericType then
        let definition = t.GetGenericTypeDefinition ()
        let name = definition.FullName.Substring (0, definition.FullName.IndexOf '`')

        name
        + "{"
        + (t.GetGenericArguments ()
           |> Array.map typeId
           |> String.concat ",")
        + "}"
    else
        t.FullName.Replace ('+', '.')

/// The documentation IDs of the public types, methods, constructors and properties of QuestPDF.
let private questPdfIds =
    lazy
        (let flags =
            BindingFlags.Public
            ||| BindingFlags.Static
            ||| BindingFlags.Instance
            ||| BindingFlags.DeclaredOnly

         let parameters (ps: ParameterInfo[]) =
             if ps.Length = 0 then
                 ""
             else
                 "("
                 + (ps
                    |> Array.map (fun p -> typeId p.ParameterType)
                    |> String.concat ",")
                 + ")"

         set
             [ for t in typeof<QuestPDF.Infrastructure.Unit>.Assembly.GetExportedTypes () do
                   let name = t.FullName.Replace ('+', '.')
                   yield "T:" + name

                   for m in t.GetMethods flags do
                       let generic =
                           if m.IsGenericMethod then
                               "``" + string (m.GetGenericArguments().Length)
                           else
                               ""

                       yield $"M:{name}.{m.Name}{generic}{parameters (m.GetParameters ())}"

                   for c in t.GetConstructors () do
                       yield $"M:{name}.#ctor{parameters (c.GetParameters ())}"

                   for p in t.GetProperties flags do
                       yield $"P:{name}.{p.Name}"

                   for f in t.GetFields (BindingFlags.Public ||| BindingFlags.Static) do
                       yield $"F:{name}.{f.Name}" ])

[<Tests>]
let tests =
    testList
        "Docs"
        [ test "every QuestPDF cref names a QuestPDF member" {
              let crefs =
                  [ for m in Regex.Matches (File.ReadAllText (docsXml ()), "cref=\"([A-Z]:QuestPDF\\.[^\"]*)\"") do
                        let cref = m.Groups[1].Value

                        if not (cref.Substring(2).StartsWith "QuestPDF.FSharp.") then
                            yield cref ]

              Expect.isNonEmpty crefs "the documentation has QuestPDF crefs"

              let unresolved =
                  crefs
                  |> List.filter (fun cref -> not (questPdfIds.Value.Contains cref))
                  |> List.distinct

              Expect.equal unresolved [] "every QuestPDF cref resolves"
          } ]
