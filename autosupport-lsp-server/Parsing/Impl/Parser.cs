using autosupport_lsp_server.LSP;
using autosupport_lsp_server.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static autosupport_lsp_server.Parsing.RuleState;

namespace autosupport_lsp_server.Parsing.Impl
{
    internal class Parser : IParser
    {
        private readonly IAutosupportLanguageDefinition languageDefinition;
        private readonly IList<IError> errors;
        private readonly IList<(CompletionItem Continuation, RuleState? RuleState)> possibleContinuations;
        private ParseState parseState;

        public Parser(IAutosupportLanguageDefinition autosupportLanguageDefinition)
        {
            languageDefinition = autosupportLanguageDefinition;

            errors = new List<IError>();
            possibleContinuations = new List<(CompletionItem Continuation, RuleState? RuleState)>();
            parseState = new ParseState(new string[0], new Position(), new List<RuleState>(0));
        }

        public IParseResult Parse(string[] text)
        {
            SetupDefaultValues(text);
            ParseUntilEndOrFailed();
            return MakeParseResult();
        }

        private void SetupDefaultValues(string[] text)
        {
            errors.Clear();
            possibleContinuations.Clear();
            parseState = GetInitializedParseState(text);
        }

        private ParseState GetInitializedParseState(string[] text)
        {
            var ruleStates =
                (from startRuleName in languageDefinition.StartRules
                 select new RuleState(languageDefinition.Rules[startRuleName]))
                 .ToList();

            return new ParseState(text, new Position(0, 0), ruleStates);
        }

        private void ParseUntilEndOrFailed()
        {
            if (parseState == null)
                throw new ArgumentException(nameof(parseState) + " may not be null when running " + nameof(ParseUntilEndOrFailed));

            while (!parseState.HasFinishedParsing && !parseState.IsAtEndOfDocument)
            {
                foreach (var ruleState in parseState.RuleStates)
                {
                    ParseRuleState(ruleState);
                }

                parseState.NextStep();
            }
        }

        private void ParseRuleState(RuleState ruleState)
        {
            if (parseState == null)
                throw new ArgumentException(nameof(parseState) + " may not be null when running " + nameof(ParseRuleState));

            var newParseStates = GetPossibleNextStatesOfSymbol(ruleState);
            ScheduleNextParseStates(newParseStates);
        }

        private IDictionary<int, IEnumerable<RuleState>>? GetPossibleNextStatesOfSymbol(RuleState ruleState)
        {
            if (ruleState.IsFinished)
                return null;

            var currentSymbol = ruleState.CurrentSymbol;
            if (currentSymbol == null)
                throw new Exception("Current Symbol is null");

            var nextStates = LSPUtils.FollowUntilNextTerminalOrAction(
                new LSPUtils.FollowUntilNextTerminalOrActionArgs<IDictionary<int, IEnumerable<RuleState>>?>(
                        ruleState,
                        rules: languageDefinition.Rules,
                        onTerminal: ParseTerminal,
                        onAction: InterpretAction
                    ))
                .WhereNotNull();

            return nextStates.Aggregate<IDictionary<int, IEnumerable<RuleState>>?, IDictionary<int, IEnumerable<RuleState>>>(
                new Dictionary<int, IEnumerable<RuleState>>(), MergeDictionaries
                );
        }


        private void ScheduleNextParseStates(IDictionary<int, IEnumerable<RuleState>>? nextParseStates)
        {
            nextParseStates?.ForEach(parseStateKvp =>
                parseState.ScheduleNewRuleStatesIn(parseStateKvp.Key, parseStateKvp.Value)
            );
        }

        private IDictionary<int, IEnumerable<RuleState>>? ParseTerminal(RuleState ruleState, ITerminal terminal)
        {
            var actualText = parseState.GetNextTextFromPosition(terminal.MinimumNumberOfCharactersToParse);

            if (actualText.Length < terminal.MinimumNumberOfCharactersToParse)
            {
                foreach (var expectedText in terminal.PossibleContent)
                {
                    if (expectedText.StartsWith(actualText))
                    {
                        TextEdit? textEdit = null;

                        if (expectedText != actualText)
                        {
                            textEdit = new TextEdit()
                            {
                                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(parseState.Position, parseState.Position),
                                NewText = expectedText.Substring(actualText.Length)
                            };
                        }


                        possibleContinuations.Add((
                            new CompletionItem()
                            {
                                Label = expectedText,
                                Kind = CompletionItemKind.Keyword,
                                TextEdit = textEdit
                            },
                            ruleState.Clone().WithNextSymbol().TryBuild()
                            ));
                    }
                }
                return null;
            }

            if (terminal.TryParse(actualText))
            {
                return new Dictionary<int, IEnumerable<RuleState>>(1)
                {
                    {
                        terminal.MinimumNumberOfCharactersToParse,
                        new RuleState[1]
                        {
                            ruleState.Clone().WithNextSymbol().TryBuild() ?? RuleState.FinishedRuleState
                        }
                    }
                };
            }
            else
            {
                return null;
            }
        }

        private IConcreteRuleStateBuilder InterpretAction(RuleState ruleState, IAction action)
        {
            (var cmd, var args) = ExtractCommandAndArgsFromAction(action);

            switch (cmd)
            {
                case IAction.IDENTIFIER:
                    if (ruleState.Markers.TryGetValue(IAction.IDENTIFIER, out var pos))
                    {
                        string name = parseState.GetTextBetweenPositions(pos);
                        var identifier = ruleState.Identifiers.FirstOrDefault(i => i.Name == name);

                        if (identifier == null)
                        {
                            ruleState.Identifiers.Add(new Identifier()
                            {
                                Name = name,
                                References = new List<Position>() { pos }
                            });
                        }
                        else
                        {
                            identifier.References.Add(pos);
                        }
                        return ruleState.Clone().WithoutMarker(IAction.IDENTIFIER);
                    }
                    else
                        return ruleState.Clone().WithMarker(IAction.IDENTIFIER, parseState.Position);
            }

            return ruleState.Clone();
        }

        private IDictionary<int, IEnumerable<RuleState>> MergeDictionaries(IDictionary<int, IEnumerable<RuleState>> dict1, IDictionary<int, IEnumerable<RuleState>>? dict2)
        {
            if (dict2 == null)
            {
                return dict1;
            }

            dict2.Keys.ForEach(key =>
            {
                if (!dict1.ContainsKey(key))
                {
                    dict1.Add(key, new RuleState[0]);
                }

                dict1[key] = dict2[key].Aggregate(dict1[key], (acc, ruleState) => acc.Append(ruleState));
            });

            return dict1;
        }


        private (string Cmd, string[]? Args) ExtractCommandAndArgsFromAction(IAction action)
        {
            int splitIdx = action.Command.IndexOf(' ');

            if (splitIdx >= 0)
                return (
                    Cmd: action.Command.Substring(0, splitIdx),
                    Args: action.Command.Substring(splitIdx + 1).Split(' ')
                );
            else
                return (
                    Cmd: action.Command,
                    Args: null
                );
        }

        private IParseResult MakeParseResult()
        {
            return new ParseResult(
                    finished: (parseState?.IsAtEndOfDocument) ?? false,
                    possibleContinuations: GetPossibleContinuations(),
                    errors: new IError[0]
                );
        }

        private CompletionItem[] GetPossibleContinuations()
        {
            return parseState.RuleStates
                .Select<RuleState, (CompletionItem? Continuation, RuleState? RuleState)>(rs => (null, rs))
                .Union(Enumerable.Cast<(CompletionItem? Continuation, RuleState? RuleState)>(possibleContinuations))
                .SelectMany(GetPossibleContinuationsOfRuleState)
                .WhereNotNull()
                .ToArray();
        }

        private IEnumerable<CompletionItem?> GetPossibleContinuationsOfRuleState((CompletionItem? Continuation, RuleState? RuleState) state)
        {
            (var continuation, var ruleState) = state;

            if (ruleState == null || ruleState.IsFinished || ruleState.CurrentSymbol == null)
            {
                yield return continuation;
            }
            else
            {
                foreach (var possibleNext in ruleState.CurrentSymbol.Match(
                    terminal =>
                    {
                        return terminal.PossibleContent.Select(pc =>
                            new CompletionItem()
                            {
                                Label = pc,
                                Kind = CompletionItemKind.Keyword
                            });
                    },
                    nonTerminal =>
                    {
                        return GetPossibleContinuationsOfRuleState((continuation, ruleState.Clone()
                                .WithNewRule(languageDefinition.Rules[nonTerminal.ReferencedRule])
                                .Build()))
                            .ToArray();
                    },
                    action =>
                    {
                        return Enumerable.Empty<CompletionItem?>();
                    },
                    oneOf =>
                    {
                        return Enumerable.Empty<CompletionItem?>();
                    }))
                {
                    yield return possibleNext;
                }
            }
        }
    }
}
