module FSharp.QuestPDF.Preview.Tests.DocsTests

open System
open System.IO
open System.Reflection
open System.Xml.Linq
open Expecto
open Microsoft.FSharp.Reflection
open FSharp.QuestPDF

/// The documentation file of FSharp.QuestPDF.Preview: $FSHARP_QUESTPDF_PREVIEW_DOCS_XML when set, otherwise the file
/// beside the assembly.
let private docsXml () =
    match
        Environment.GetEnvironmentVariable "FSHARP_QUESTPDF_PREVIEW_DOCS_XML"
        |> Option.ofObj
    with
    | Some path -> path
    | None -> Path.ChangeExtension (typeof<Preview>.Assembly.Location, ".xml")

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

let private methodId (typeName: string) (m: MethodInfo) =
    let generic =
        if m.IsGenericMethod then
            "``" + string (m.GetGenericArguments().Length)
        else
            ""

    let parameters =
        match m.GetParameters () with
        | [||] -> ""
        | ps ->
            "("
            + (ps
               |> Array.map (fun p -> typeId p.ParameterType)
               |> String.concat ",")
            + ")"

    $"M:{typeName}.{m.Name}{generic}{parameters}"

/// The documentation IDs of the public F# surface of an assembly under the FSharp.QuestPDF namespace: types, union
/// cases, record fields, module values and functions, and the members declared by classes. A SageFs session adds
/// types of its own to the assembly outside the namespace.
let private publicIds (assembly: Assembly) =
    let flags =
        BindingFlags.Public
        ||| BindingFlags.Static
        ||| BindingFlags.Instance
        ||| BindingFlags.DeclaredOnly

    let isCaseType (t: Type) =
        not (isNull t.DeclaringType)
        && (FSharpType.IsUnion t.DeclaringType
            || FSharpType.IsRecord t.DeclaringType)

    [ for t in assembly.GetExportedTypes () do
          if
              t.Namespace = "FSharp.QuestPDF"
              && not (isCaseType t)
          then
              let name = t.FullName.Replace ('+', '.')
              yield "T:" + name

              if FSharpType.IsUnion t then
                  for case in FSharpType.GetUnionCases t do
                      yield $"T:{name}.{case.Name}"
              elif FSharpType.IsRecord t then
                  for field in FSharpType.GetRecordFields t do
                      yield $"P:{name}.{field.Name}"
              else
                  for p in t.GetProperties flags do
                      yield $"P:{name}.{p.Name}"

                  for m in t.GetMethods flags do
                      if not m.IsSpecialName then
                          yield methodId name m ]

/// The IDs of a documentation file whose member has a non-empty summary.
let private summarized (xml: XDocument) =
    xml.Descendants (XName.Get "member")
    |> Seq.filter (fun m ->
        match m.Element (XName.Get "summary") with
        | null -> false
        | summary -> not (String.IsNullOrWhiteSpace summary.Value))
    |> Seq.map (fun m -> m.Attribute(XName.Get "name").Value)
    |> Set.ofSeq

let private undocumented (xml: XDocument) =
    let documented = summarized xml

    publicIds typeof<Preview>.Assembly
    |> List.filter (fun id -> not (documented.Contains id))

[<Tests>]
let tests =
    testList
        "Docs"
        [ test "every public member of FSharp.QuestPDF.Preview has an XML summary" {
              let xml = XDocument.Load (docsXml ())
              Expect.isNonEmpty (publicIds typeof<Preview>.Assembly) "the public surface"
              Expect.equal (undocumented xml) [] "members without a summary"
          }
          test "the check reports a member without a summary" {
              let xml = XDocument.Load (docsXml ())

              let serve =
                  xml.Descendants (XName.Get "member")
                  |> Seq.find (fun m -> m.Attribute(XName.Get "name").Value.StartsWith "M:FSharp.QuestPDF.PreviewModule.serve``1")

              serve.Remove ()

              Expect.equal
                  (undocumented xml)
                  [ "M:FSharp.QuestPDF.PreviewModule.serve``1(FSharp.QuestPDF.PreviewModule.Options,Microsoft.FSharp.Core.FSharpFunc{Microsoft.FSharp.Core.Unit,``0})" ]
                  "the removed member"
          } ]
