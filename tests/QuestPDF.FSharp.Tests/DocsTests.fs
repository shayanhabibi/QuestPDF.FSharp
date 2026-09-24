module QuestPDF.FSharp.Tests.DocsTests

open System
open System.IO
open System.Reflection
open System.Text.RegularExpressions
open Expecto
open QuestPDF.Fluent
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

/// The docs directory of the repository, found from this source file at compile time.
let private docsDirectory =
    Path.GetFullPath (Path.Combine (__SOURCE_DIRECTORY__, "..", "..", "docs"))

/// The literate pages of the documentation, relative to docs/, without the .fsx extension.
let private literatePages =
    [ "index"
      "concepts"
      "lengths-and-colors"
      "text"
      "layout"
      "tables"
      "paging"
      "images"
      "output"
      "interop"
      "testing-your-documents"
      "recipes/invoice"
      "recipes/resume" ]

let private pageSource (page: string) =
    File.ReadAllText (Path.Combine (docsDirectory, page + ".fsx"))

let private readme () =
    File.ReadAllText (Path.Combine (docsDirectory, "..", "README.md"))

/// The navigation of the site: each page with its category, in reading order.
let private navigation =
    [ "concepts", "Guide"
      "lengths-and-colors", "Guide"
      "text", "Guide"
      "layout", "Guide"
      "tables", "Guide"
      "paging", "Guide"
      "images", "Guide"
      "output", "Guide"
      "interop", "Guide"
      "testing-your-documents", "Guide"
      "recipes/invoice", "Recipes"
      "recipes/resume", "Recipes"
      "gotchas", "Reference" ]

/// The fsdocs front matter of a page, as key-value pairs.
let private frontMatter (page: string) =
    let path =
        [ page + ".fsx"; page + ".md" ]
        |> List.map (fun name -> Path.Combine (docsDirectory, name))
        |> List.find File.Exists

    let m =
        Regex.Match (File.ReadAllText path, @"\A(?:\(\*\*\s*)?---\r?\n(.*?)\r?\n---", RegexOptions.Singleline)

    if m.Success then
        [ for line in m.Groups[1].Value.Split '\n' do
              match line.Split (':', 2) with
              | [| key; value |] -> key.Trim (), value.Trim ()
              | _ -> () ]
        |> Map.ofList
    else
        Map.empty

/// The visible text of a literate page: the page without its hidden blocks.
let private visibleSource (page: string) =
    Regex.Replace (pageSource page, @"\(\*\*\* hide \*\*\*\).*?(?=\(\*\*)", "", RegexOptions.Singleline)

/// The types a compiled module function takes as a parameter only when F# folds the lambda of its body into its
/// parameters.
let private phantomParameterTypes =
    [ typeof<Slot>
      typeof<Styled>
      typeof<PageDescriptor>
      typeof<RowDescriptor>
      typeof<TableDescriptor>
      typeof<TableColumnsDefinitionDescriptor>
      typeof<LayersDescriptor>
      typeof<DecorationDescriptor>
      typeof<TextDescriptor>
      typeof<ImageDescriptor>
      typeof<SvgImageDescriptor>
      typeof<LineDescriptor> ]

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
          }
          test "every documentation page is a literate script" {
              let missing =
                  literatePages
                  |> List.filter (fun page -> not (File.Exists (Path.Combine (docsDirectory, page + ".fsx"))))

              Expect.equal missing [] "pages without a .fsx source"
          }
          test "gotchas is the only Markdown page" {
              let markdown =
                  Directory.GetFiles (docsDirectory, "*.md", SearchOption.AllDirectories)
                  |> Array.map (fun path -> Path.GetRelativePath(docsDirectory, path).Replace ('\\', '/'))
                  |> Array.sort
                  |> List.ofArray

              Expect.equal markdown [ "gotchas.md" ] "Markdown pages"
          }
          test "every literate page embeds a rendered page" {
              let withoutRender =
                  literatePages
                  |> List.filter (fun page -> File.Exists (Path.Combine (docsDirectory, page + ".fsx")))
                  |> List.filter (fun page ->
                      let source = pageSource page

                      not (source.Contains "(*** include-it-raw ***)")
                      || not (source.Contains "Pdf.images ImageFormat.Png 96"))

              Expect.equal withoutRender [] "pages without an embedded render"
          }
          test "module functions compile with their declared parameters" {
              let offenders =
                  [ for t in typeof<Length>.Assembly.GetExportedTypes () do
                        if t.IsAbstract && t.IsSealed then
                            for m in
                                t.GetMethods (
                                    BindingFlags.Public
                                    ||| BindingFlags.Static
                                    ||| BindingFlags.DeclaredOnly
                                ) do
                                if
                                    m.GetParameters ()
                                    |> Array.exists (fun p -> List.contains p.ParameterType phantomParameterTypes)
                                then
                                    yield $"{t.Name}.{m.Name}" ]

              Expect.equal offenders [] "functions whose reference page shows a slot or descriptor parameter"
          }
          test "no page loops with -> in a list" {
              let arrow = Regex @"\bfor\s+[\w(), ]+\s+in\s+[^\r\n`]*->"

              let pages =
                  [ for page in literatePages do
                        if arrow.IsMatch (pageSource page) then
                            yield page
                    if arrow.IsMatch (readme ()) then
                        yield "README" ]

              Expect.equal pages [] "pages that teach for ... -> in place of for ... do"
          }
          test "the gotchas quote the compiler messages of common mistakes" {
              let gotchas = File.ReadAllText (Path.Combine (docsDirectory, "gotchas.md"))

              for text in
                  [ "FS0193"
                    "FS0071"
                    "LengthWitness"
                    "NumberWitness"
                    "PageDescriptor"
                    "'unit'" ] do
                  Expect.stringContains gotchas text $"gotchas quote {text}"
          }
          test "the quick start shows the font registration a script needs" {
              Expect.stringContains (readme ()) "Font.registerDirectory" "README"
              Expect.stringContains (visibleSource "index") "Font.registerDirectory" "index, outside the hidden blocks"
              Expect.stringContains (visibleSource "text") "Style.family" "text, outside the hidden blocks"
          }
          test "the navigation follows the reading order" {
              let categoryIndex =
                  navigation
                  |> List.map snd
                  |> List.distinct
                  |> List.mapi (fun i category -> category, string (i + 1))
                  |> Map.ofList

              for category, pages in List.groupBy snd navigation do
                  pages
                  |> List.iteri (fun i (page, _) ->
                      let matter = frontMatter page
                      Expect.equal (Map.tryFind "category" matter) (Some category) $"{page} category"
                      Expect.equal (Map.tryFind "categoryindex" matter) (Some categoryIndex[category]) $"{page} categoryindex"
                      Expect.equal (Map.tryFind "index" matter) (Some (string (i + 1))) $"{page} index")
          }
          test "QuestPDF links are replaced by their text" {
              let html =
                  "Maps to <a href=\"https://learn.microsoft.com/dotnet/api/questpdf.fluent.pagedescriptor.size\">PageDescriptor.Size</a> and "
                  + "<a href=\"https://learn.microsoft.com/dotnet/api/system.string\">string</a>."

              Expect.equal
                  (QuestPDF.FSharp.Build.QuestPdfLinks.unlink html)
                  ("Maps to <code>PageDescriptor.Size</code> and "
                   + "<a href=\"https://learn.microsoft.com/dotnet/api/system.string\">string</a>.")
                  "only the QuestPDF link is replaced"
          }
          test "the interop mapping table is generated from Coverage.fs" {
              Expect.stringContains (pageSource "interop") "#load \"../tests/QuestPDF.FSharp.Tests/Coverage.fs\"" "loads Coverage.fs"
          } ]
