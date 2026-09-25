#r "nuget: Partas.Build, 0.4.0-alpha.3"
#r "nuget: Partas.TypeProvider.BuildHelper, 0.2.5"
#r "nuget: Fake.IO.FileSystem"
#r "nuget: Str"
#load "build/QuestPdfLinks.fs"

open Partas.Build
open Partas.TypeProvider.BuildHelper
open Fake.IO.Globbing.Operators
open Fake.IO
open Str

[<Literal>]
let __REPOSITORY_DIRECTORY__ =
    __SOURCE_DIRECTORY__

type Repo = BuildHelperProvider<
    __REPOSITORY_DIRECTORY__,
    "bin/",
    capabilityFullOverride = true
>

let inline funApply value fn = fn value

module Spec =
    let formattingSourceFiles =
        !! "**/*.fs"
        -- "**/obj/**/*.*"
        -- "**/AssemblyInfo.fs"
        -- "**/fable-modules/**/*.*"

    let projects = Repo.Project.AllProjects()
    let sourceProjects = projects |> List.filter _.RelativePath.StartsWith("src")
    let testProjects = projects |> List.filter _.RelativePath.StartsWith("tests")
    let sampleProjects = projects |> List.filter _.RelativePath.StartsWith("samples")
    let sourceProjectsMap =
        sourceProjects
        |> List.map (
            _.Name
            >> Str.replaceChar '.' '-'
            >> Str.toLower
            )
        |> List.zip
        |> funApply sourceProjects
        |> Map.ofList

module Options =
    let config =
        Baked.Input.DotNet.configString
        |> InputSpec.ofInput
        |> InputSpec.map (Option.defaultValue "Release")
    let quick =
        Input.option<bool> "--quick"
        |> Input.alias "-q"
        |> Input.desc "Skips installs and restores"

    let format =
        Input.option<bool> "--format"
        |> Input.alias "-f"
        |> Input.desc "Formats the source files prior to building/testing/publishing"

    let dryFormat =
        Input.option<bool> "--dry-format"
        |> Input.desc "Checks if the source files require formatting"

    let skipTests =
        Input.option<bool> "--skip-tests"
        |> Input.desc "Skips running the test suite"

    let watch =
        Input.option<bool> "--watch"
        |> Input.desc "Runs the operation in watch mode"


    let apiKey =
        Baked.Input.NuGet.apiKeyOrEnv
        |> Input.alias "--api-key"
        |> Input.alias "-k"

    let projects =
        Spec.sourceProjectsMap
        |> Map.keys
        |> Seq.toList
        |> Baked.Input.Project.target
        |> Input.customParser (fun res ->
            res.Tokens
            |> Seq.toList
            |> List.map (
                _.Value
                >> Map.find
                >> funApply Spec.sourceProjectsMap
                >> _.Path
                )
            )

module Stage =
    let restore = input {
        let! quick = Options.quick
        return stage "restore" {
            quiet
            workingDir Repo.FileSystem.``.``
            when' (not quick)
            parallel'
            stage "restore solution" {
                run $"dotnet restore {Repo.Project.SolutionFile} -v q"
            }
            stage "restore tools" {
                run "dotnet tool restore -v q"
            }
        }
    }

    let clean = input {
        let! quick = Options.quick
        return stage "clean" {
            when' (not quick)
            run (async {
                !! "**/**/bin"
                ++ "temp"
                -- "bin"
                |> Shell.cleanDirs
            })
        }
    }


    let format (formatInput: InputSpec<bool>) (dryRun: InputSpec<bool>) = input {
        let! format = formatInput
        and! dryRun = dryRun
        let commandString =
            Spec.formattingSourceFiles
            |> Seq.map (sprintf "\"%s\"")
            |> String.concat " "
            |> if dryRun
                then sprintf "dotnet fantomas %s --check"
                else sprintf "dotnet fantomas %s"
        return stage "format" {
            when' (format || dryRun)
            quiet
            run commandString
        }
    }

    let build = input {
        let! config = Options.config
        return stage "build" {
            quiet
            stage "build sources" {
                parallel'
                for { Name = name; Path = project } in Spec.sourceProjects do
                stage $"build {name}" { run (cmd $"dotnet build {project} -c {config} -v q") }
            }
            stage "build samples" {
                parallel'
                for { Name = name; Path = project } in Spec.sampleProjects do
                stage $"build {name}" { run (cmd $"dotnet build {project} -c {config} -v q") }
            }
        }
    }


    let pack = input {
        let! projects = Options.projects
        and! config = Options.config
        return stage "pack" {
            quiet
            projects
            |> function
                | [] ->
                    Spec.sourceProjects
                    |> List.map _.Path
                | projects ->
                    projects
            |> List.map (fun project ->
                stage $"pack {project}" {
                    run (cmd $"dotnet pack {project} -c {config} -v q --no-build --no-restore -o {Repo.VirtualFileSystem.bin.ToString()}")
                })
        }
    }

    let publish = input {
        let! apiKey = Options.apiKey
        // One separator style: on Windows a mixed "C:\...\bin/*.nupkg" matches no files.
        let packages = System.IO.Path.Combine(Repo.VirtualFileSystem.bin.ToString(), "*.nupkg")
        // No --skip-duplicate: it treats every 409 as an existing version, and nuget.org also answers 409 for an
        // ID in a reserved prefix, so a rejected push passed as success. The workflow skips versions already out.
        return stage "publish" {
            quiet
            failIfIgnored
            when' apiKey.IsSome
            run (cmd $"dotnet nuget push {packages} -k {apiKey.Value} -s https://api.nuget.org/v3/index.json")
        }
    }

    let runTests = input {
        let! config = Options.config
        and! skipTests = Options.skipTests
        return stage "run tests" {
            quiet
            when' (not skipTests)
            for { Name = name; Path = path } in Spec.testProjects do
            stage $"run {name}" {
                run (cmd $"dotnet test {path} -c {config} -v q")
            }
        }
    }

    /// Runs the SageFs end-to-end checks of FSharp.QuestPDF.Preview, which need a SageFs daemon on port 37749.
    let previewE2e = input {
        let! config = Options.config
        return stage "preview e2e" {
            envVars [ ("QPDF_SAGEFS_E2E", "1") ]
            run (cmd $"dotnet run --project tests/FSharp.QuestPDF.Preview.Tests -c {config} -- --filter-test-list SageFs")
        }
    }

    let generateDocs = input {
        let! watch = Options.watch
        and! config = Options.config
        return stage "docs" {
            quiet
            run (
                Cmd.ofString "dotnet"
                |> Cmd.arg "fsdocs"
                |> Cmd.arg (if watch then "watch" else "build")
                |> Cmd.arg "--eval"
                |> Cmd.arg "--properties"
                |> Cmd.arg $"Configuration={config}"
                |> Cmd.argIf (not watch) [ "--clean"; "--strict" ]
                )
        }
    }
    /// Replaces the links fsdocs makes from QuestPDF crefs, which point at missing Microsoft Learn pages, with their
    /// text.
    let unlinkQuestPdf = input {
        let! watch = Options.watch
        return stage "unlink QuestPDF members" {
            when' (not watch)
            run (async {
                for page in !! "output/reference/*.html" do
                    let html = System.IO.File.ReadAllText page
                    let unlinked = FSharp.QuestPDF.Build.QuestPdfLinks.unlink html
                    if unlinked <> html then
                        System.IO.File.WriteAllText (page, unlinked)
            })
        }
    }
    /// Fails when a page evaluated with an error: fsdocs --strict stops on compile errors only, and a snippet that
    /// throws leaves "No value returned by any evaluator" in the page.
    let checkDocs = input {
        let! watch = Options.watch
        return stage "check docs" {
            when' (not watch)
            run (async {
                let failed =
                    !! "output/**/*.html"
                    |> Seq.filter (fun page -> System.IO.File.ReadAllText(page).Contains "No value returned by any evaluator")
                    |> Seq.toList
                if not failed.IsEmpty then
                    failwithf "snippets failed to evaluate in: %s" (String.concat ", " failed)
            })
        }
    }
exit <| rootCommandOfScript {
    name "build.fsx"
    description "Build CLI"
    workingDir __REPOSITORY_DIRECTORY__
    command "build" {
        description "Builds the solution"
        Stage.restore
        Stage.build
    }
    command "publish" {
        description "Publishes the solution to NuGet"
        Stage.restore
        Stage.clean
        Stage.format (InputSpec.ofInput Options.format) (InputSpec.ofInput Options.dryFormat)
        Stage.build
        Stage.runTests
        Stage.pack
        Stage.publish
    }
    command "bump" {
        description "Bumps the version of the project"
        Baked.Pipelines.bumpArgument (Spec.sourceProjects |> List.map _.Path) (InputSpec.ofInput Options.projects)
    }
    command "docs" {
        description "Generates the documentation (--watch to serve it)"
        Stage.restore
        Stage.build
        Stage.generateDocs
        Stage.unlinkQuestPdf
        Stage.checkDocs
    }
    command "test" {
        alias "tests"
        description "Runs the test suite"
        Stage.restore
        Stage.clean
        Stage.format (InputSpec.ofInput Options.format) (InputSpec.ofInput Options.dryFormat)
        Stage.runTests
    }
    command "preview-e2e" {
        description "Runs the SageFs end-to-end checks of FSharp.QuestPDF.Preview against a running daemon"
        Stage.restore
        Stage.previewE2e
    }
    command "format" {
        alias "apply-style"
        description "Formats the source files"
        Stage.restore
        Stage.clean
        Stage.format (InputSpec.ret true) (InputSpec.ofInput Options.dryFormat)
    }
}
