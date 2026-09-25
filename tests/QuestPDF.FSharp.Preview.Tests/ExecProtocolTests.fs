module QuestPDF.FSharp.Preview.Tests.ExecProtocolTests

open System.Text.Json
open Expecto
open QuestPDF.FSharp.PreviewServer
open QuestPDF.FSharp.Preview.Tests.Support

/// The script of the compile-error fixture.
let private script = @"C:\work\invoice\invoice.fsx"

/// The code and the working directory of a request body, and its property names.
let private fields (body: string) =
    let document = JsonDocument.Parse body

    try
        let root = document.RootElement

        let names =
            root.EnumerateObject ()
            |> Seq.map _.Name
            |> List.ofSeq

        root.GetProperty("code").GetString (), root.GetProperty("working_directory").GetString (), names
    finally
        document.Dispose ()

let private request =
    testList
        "request"
        [ test "the body has exactly code and working_directory" {
              let _, workingDirectory, names =
                  fields (ExecProtocol.request script "C:/work/invoice")

              Expect.equal names [ "code"; "working_directory" ] "the properties"
              Expect.equal workingDirectory "C:/work/invoice" "the working directory"
          }
          test "the code loads the script through a verbatim string" {
              let code, _, _ = fields (ExecProtocol.request @"C:\My Scripts\invoice.fsx" "C:/w")
              Expect.stringStarts code "#load @\"C:\\My Scripts\\invoice.fsx\" // qpdf-preview " "backslashes and spaces kept"
          }
          test "a quote in the path is doubled" {
              let code, _, _ = fields (ExecProtocol.request "/home/a\"b/invoice.fsx" "/w")
              Expect.stringStarts code "#load @\"/home/a\"\"b/invoice.fsx\" // qpdf-preview " "doubled quote"
          }
          test "the nonce differs on each call" {
              let first, _, _ = fields (ExecProtocol.request script "C:/w")
              let second, _, _ = fields (ExecProtocol.request script "C:/w")
              Expect.notEqual first second "two codes"
          } ]

let private parse =
    testList
        "parse"
        [ test "success is Loaded" { Expect.equal (ExecProtocol.parse script 200 (fixture "exec-success.json")) Loaded "loaded" }
          test "a compile error gives the diagnostic at line 17, column 54 of the script" {
              match ExecProtocol.parse script 200 (fixture "exec-compile-error.json") with
              | Compile [ d ] ->
                  Expect.equal (d.File, d.Line, d.Column) (script, 17, 54) "the position, with a 1-based column"
                  Expect.equal d.Message "The type 'int' does not match the type 'string'" "the message"
              | other -> failtest $"expected one compile error, got %A{other}"
          }
          test "a runtime exception gives Runtime with the exception message" {
              match ExecProtocol.parse script 200 (fixture "exec-runtime-error.json") with
              | Runtime message -> Expect.equal message "Exception: no invoice today" "the first line"
              | other -> failtest $"expected Runtime, got %A{other}"
          }
          test "no matching session gives Routing with the daemon's reason" {
              match ExecProtocol.parse script 404 (fixture "exec-no-session.json") with
              | Routing reason -> Expect.stringStarts reason "No sessions match workingDirectory" "the reason, without the prefix"
              | other -> failtest $"expected Routing, got %A{other}"
          }
          test "several matching sessions give Routing with the daemon's reason" {
              match ExecProtocol.parse script 404 (fixture "exec-ambiguous.json") with
              | Routing reason -> Expect.stringStarts reason "Multiple sessions match workingDirectory" "the reason"
              | other -> failtest $"expected Routing, got %A{other}"
          }
          test "an HTML 404 gives Routing with the status" {
              match ExecProtocol.parse script 404 (fixture "html-404.html") with
              | Routing reason -> Expect.stringContains reason "404" "the status"
              | other -> failtest $"expected Routing, got %A{other}"
          }
          test "an empty body gives Routing with the status" {
              for status in [ 200; 404 ] do
                  match ExecProtocol.parse script status "" with
                  | Routing reason -> Expect.stringContains reason (string status) "the status"
                  | other -> failtest $"expected Routing for {status}, got %A{other}"
          }
          test "a 2xx JSON body other than an object gives Routing with the status" {
              for body in [ "[]"; "null"; "42" ] do
                  match ExecProtocol.parse script 200 body with
                  | Routing reason -> Expect.stringContains reason "HTTP 200 without a readable body" body
                  | other -> failtest $"expected Routing for {body}, got %A{other}"
          } ]

let private diagnostics =
    testList
        "diagnostics"
        [ test "a message over several lines is kept whole, and warnings are left out" {
              let text =
                  "Error: Evaluation failed\nDiagnostics:\n  [warning] (2,0) Unused\n  [error] (3,4) This expression was expected to have type\n    'string'    \nbut here has type\n    'int'    \n  [error] (9,1) Second"

              let found = ExecProtocol.diagnostics script text

              Expect.equal
                  (found
                   |> List.map (fun d -> d.Line, d.Column, d.Message))
                  [ 3, 5, "This expression was expected to have type\n    'string'    \nbut here has type\n    'int'"
                    9, 2, "Second" ]
                  "two errors"
          }
          test "a text without a Diagnostics block has none" {
              Expect.isEmpty (ExecProtocol.diagnostics script "Error: Evaluation failed: Exception: boom") "none"
          } ]

let private loads =
    testList
        "loads"
        [ test "a directive names one or more files in regular, verbatim and triple-quoted strings" {
              let text =
                  String.concat
                      "\n"
                      [ """#r "nuget: X" """
                        """#load "parts/a.fsx" """
                        """  #load @"C:\b.fsx" "c.fsx" """
                        "#load \"\"\"d.fsx\"\"\""
                        """#load "e\\f.fsx" """
                        "let x = 1" ]

              Expect.equal (ExecProtocol.loads text) [ "parts/a.fsx"; @"C:\b.fsx"; "c.fsx"; "d.fsx"; @"e\f.fsx" ] "the paths in order"
          }
          test "a commented directive and a #load inside a string are left out" {
              let text =
                  String.concat
                      "\n"
                      [ """// #load "a.fsx" """
                        """let s = "#load \"b.fsx\"" """
                        "(*"
                        """#load "c.fsx" """
                        "*)" ]

              Expect.equal (ExecProtocol.loads text) [] "no paths"
          } ]

let private loadOrder =
    testList
        "loadOrder"
        [ test "loaded files come before the files that load them, each once, without the script" {
              let root =
                  if System.OperatingSystem.IsWindows () then
                      @"C:\w"
                  else
                      "/w"

              let at (relative: string) =
                  System.IO.Path.GetFullPath (System.IO.Path.Combine (root, relative))

              let files =
                  Map
                      [ at "main.fsx", "#load \"parts/b.fsx\" \"parts/a.fsx\""
                        at "parts/b.fsx", "#load \"a.fsx\""
                        at "parts/a.fsx", "module A"
                        at "unused.fsx", "" ]

              Expect.equal (ExecProtocol.loadOrder files.TryFind (at "main.fsx")) [ at "parts/a.fsx"; at "parts/b.fsx" ] "a, then b"
          }
          test "a file that cannot be read, and a cycle, end the walk without failing" {
              let root =
                  if System.OperatingSystem.IsWindows () then
                      @"C:\w"
                  else
                      "/w"

              let at (relative: string) =
                  System.IO.Path.GetFullPath (System.IO.Path.Combine (root, relative))

              let files =
                  Map
                      [ at "main.fsx", "#load \"a.fsx\" \"missing.fsx\""
                        at "a.fsx", "#load \"main.fsx\"" ]

              Expect.equal (ExecProtocol.loadOrder files.TryFind (at "main.fsx")) [ at "a.fsx" ] "a only"
          } ]

let private attribute =
    let error (file: string) (line: int) =
        { File = file
          Line = line
          Column = 1
          Message = "m" }

    let failed = Compile [ error script 5 ]

    testList
        "attribute"
        [ test "the errors of the first loaded file that fails to compile replace the errors of the script" {
              let probed = ResizeArray<string> ()

              let probe (file: string) =
                  probed.Add file

                  match file with
                  | "b.fsx" -> Compile [ error "b.fsx" 5 ]
                  | _ -> Loaded

              Expect.equal (ExecProtocol.attribute probe [ "a.fsx"; "b.fsx"; "c.fsx" ] failed) (Compile [ error "b.fsx" 5 ]) "b's errors"
              Expect.equal (List.ofSeq probed) [ "a.fsx"; "b.fsx" ] "probes stop at b"
          }
          test "when every loaded file compiles, the errors stay with the script" {
              let probe _ =
                  Runtime "boom"

              Expect.equal (ExecProtocol.attribute probe [ "a.fsx" ] failed) failed "unchanged"
          }
          test "a probe that cannot reach the session ends the search" {
              let probed = ResizeArray<string> ()

              let probe (file: string) =
                  probed.Add file
                  Routing "down"

              Expect.equal (ExecProtocol.attribute probe [ "a.fsx"; "b.fsx" ] failed) failed "unchanged"
              Expect.equal (List.ofSeq probed) [ "a.fsx" ] "one probe"
          }
          test "an outcome other than a compile error makes no probe" {
              let probe (file: string) =
                  failwith $"probed {file}"

              for outcome in [ Loaded; Runtime "boom"; Routing "down" ] do
                  Expect.equal (ExecProtocol.attribute probe [ "a.fsx" ] outcome) outcome (string outcome)
          } ]

let private issues =
    testList
        "issue"
        [ test "each outcome maps to the status shown until the next reload" {
              let diagnostic =
                  { File = script
                    Line = 1
                    Column = 1
                    Message = "m" }

              Expect.equal (ExecProtocol.issue Loaded) None "loaded clears"

              Expect.equal (ExecProtocol.issue (Compile [ diagnostic ])) (Some (CompileFailed [ diagnostic ])) "compile"

              Expect.equal (ExecProtocol.issue (Runtime "boom")) (Some (ReloadFailed "boom")) "runtime"
              Expect.equal (ExecProtocol.issue (Routing "down")) (Some (ReloadFailed "down")) "routing"
          } ]

[<Tests>]
let tests =
    testList "ExecProtocol" [ request; parse; diagnostics; loads; loadOrder; attribute; issues ]
