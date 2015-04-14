﻿(*
    FSharpLint, a linter for F#.
    Copyright (C) 2014 Matthew Mcveigh

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*)

module TestRuleBase

open NUnit.Framework
open Microsoft.FSharp.Compiler.Range
open FSharpLint.Framework.Ast
open FSharpLint.Framework.Configuration
open FSharpLint.Framework.LoadVisitors

let emptyConfig =
    {
        IgnoreFiles = []
        Analysers =
            Map.ofList 
                [ 
                    ("", { 
                        Rules = Map.ofList [ ("", { Settings = Map.ofList [ ("", Enabled(true)) ] }) ]
                        Settings = Map.ofList [] 
                    }) 
                ]
    }

[<AbstractClass>]
type TestRuleBase(analyser:VisitorType, ?analysers) =
    let errorRanges = System.Collections.Generic.List<range * string>()

    let postError (range:range) error =
        errorRanges.Add(range, error)

    let config = 
        match analysers with
            | Some(analysers) -> { IgnoreFiles = []; Analysers = analysers }
            | None -> emptyConfig

    member this.Parse(input, ?overrideAnalysers) = 
        let config =
            match overrideAnalysers with
                | Some(overrideAnalysers) -> { IgnoreFiles = []; Analysers = overrideAnalysers }
                | None -> config

        let visitorInfo = { PostError = postError; Config = config }

        match analyser with
            | Ast(visitor) ->
                let parseInfo = parseInput input
                parse (fun _ -> false) parseInfo [visitor visitorInfo] |> ignore
            | PlainText(visitor) -> 
                let parseInfo = parseInput input
                let suppressedMessages = getSuppressMessageAttributesFromAst parseInfo.Ast
                visitor visitorInfo { File = ""; Input = input; SuppressedMessages = suppressedMessages }

    member this.ErrorExistsAt(startLine, startColumn) =
        errorRanges
            |> Seq.exists (fun (r, _) -> r.StartLine = startLine && r.StartColumn = startColumn)

    member this.ErrorsAt(startLine, startColumn) =
        errorRanges
            |> Seq.filter (fun (r, _) -> r.StartLine = startLine && r.StartColumn = startColumn)

    member this.ErrorExistsOnLine(startLine) =
        errorRanges
            |> Seq.exists (fun (r, _) -> r.StartLine = startLine)

    member this.ErrorWithMessageExistsAt(message, startLine, startColumn) =
        this.ErrorsAt(startLine, startColumn)
            |> Seq.exists (fun (_, e) -> e = message)

    [<SetUp>]
    member this.SetUp() = 
        errorRanges.Clear()