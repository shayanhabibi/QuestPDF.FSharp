module QuestPDF.FSharp.Tests.CoverageTests

open System
open System.Reflection
open Expecto
open QuestPDF.FSharp
open QuestPDF.FSharp.Tests.Coverage

let private isObsolete (m: MemberInfo) =
    m.IsDefined (typeof<ObsoleteAttribute>, false)

/// The public, non-obsolete members of the QuestPDF fluent extension classes and descriptors, as
/// <c>Type.Member</c>.
let private questPdfMembers =
    lazy
        (typeof<QuestPDF.Fluent.Document>.Assembly.GetExportedTypes ()
         |> Seq.filter (fun t ->
             (t.Namespace = "QuestPDF.Fluent"
              || t.Namespace = "QuestPDF.Companion")
             && (t.Name.EndsWith "Extensions"
                 || t.Name.EndsWith "Descriptor")
             && not (isObsolete t))
         |> Seq.collect (fun t ->
             t.GetMethods (
                 BindingFlags.Public
                 ||| BindingFlags.Static
                 ||| BindingFlags.Instance
                 ||| BindingFlags.DeclaredOnly
             )
             |> Seq.filter (fun m -> not m.IsSpecialName && not (isObsolete m))
             |> Seq.map (fun m -> $"{t.Name}.{m.Name}"))
         |> Set.ofSeq)

/// The public members of each F# module of QuestPDF.FSharp, keyed by source module name.
let private wrapperModules =
    lazy
        (typeof<Length>.Assembly.GetExportedTypes ()
         |> Array.filter (fun t -> t.IsAbstract && t.IsSealed)
         |> Array.map (fun t ->
             let name =
                 if t.Name.EndsWith "Module" then
                     t.Name.Substring (0, t.Name.Length - "Module".Length)
                 else
                     t.Name

             let members =
                 t.GetMembers (
                     BindingFlags.Public
                     ||| BindingFlags.Static
                     ||| BindingFlags.DeclaredOnly
                 )
                 |> Array.map _.Name
                 |> Set.ofArray

             name, t.IsDefined (typeof<AutoOpenAttribute>, false), members))

/// True when the name is Module.member of a wrapper module, or a member of an AutoOpen module.
let private resolves (name: string) =
    match name.LastIndexOf '.' with
    | -1 ->
        wrapperModules.Value
        |> Array.exists (fun (_, autoOpen, members) -> autoOpen && members.Contains name)
    | dot ->
        let moduleName, memberName = name.Substring (0, dot), name.Substring (dot + 1)

        wrapperModules.Value
        |> Array.exists (fun (m, _, members) -> m = moduleName && members.Contains memberName)

[<Tests>]
let tests =
    testList
        "Coverage"
        [ test "every QuestPDF fluent member has a mapping" {
              let mapped = mappings |> List.map fst |> Set.ofList

              let missing =
                  Set.difference questPdfMembers.Value mapped
                  |> Set.toList

              Expect.equal missing [] "unmapped QuestPDF members"
          }
          test "every mapping names a current QuestPDF member" {
              let stale =
                  mappings
                  |> List.map fst
                  |> List.filter (fun key -> not (questPdfMembers.Value.Contains key))

              Expect.equal stale [] "mappings for members QuestPDF no longer has"
          }
          test "each QuestPDF member is mapped once" {
              let duplicates =
                  mappings
                  |> List.countBy fst
                  |> List.filter (fun (_, count) -> count > 1)
                  |> List.map fst

              Expect.equal duplicates [] "members mapped more than once"
          }
          test "every wrapper name resolves to a public wrapper member" {
              let unresolved =
                  [ for key, mapping in mappings do
                        match mapping with
                        | Wrapped names ->
                            if List.isEmpty names then
                                yield $"{key}: no names"

                            for name in names do
                                if not (resolves name) then
                                    yield $"{key}: {name}"
                        | Raw _ -> () ]

              Expect.equal unresolved [] "wrapper names without a public member"
          }
          test "every raw mapping gives a reason" {
              let blank =
                  [ for key, mapping in mappings do
                        match mapping with
                        | Raw reason when String.IsNullOrWhiteSpace reason -> yield key
                        | _ -> () ]

              Expect.equal blank [] "raw mappings without a reason"
          }
          test "the wrapper covers most of the fluent API" {
              let wrapped =
                  mappings
                  |> List.filter (fun (_, m) ->
                      match m with
                      | Wrapped _ -> true
                      | Raw _ -> false)

              Expect.isGreaterThan wrapped.Length (mappings.Length / 2) "more wrapped than raw"
          } ]
