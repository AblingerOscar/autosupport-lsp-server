using autosupport_lsp_server.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static autosupport_lsp_server.Parsing.RuleState;

namespace autosupport_lsp_server.Parsing.Impl
{
    internal class Parser
    {
        private readonly ParseState parseState;
        private readonly IAutosupportLanguageDefinition languageDefinition;
        private readonly IList<IError> errors;
        private readonly IList<(string Continuation, RuleState? RuleState)> possibleContinuations;
        private readonly ISet<Identifier> identifiers = Identifier.CreateIdentifierSet();

        private Parser(IAutosupportLanguageDefinition autosupportLanguageDefinition, string[] text)
        {
            languageDefinition = autosupportLanguageDefinition;

            errors = new List<IError>();
            possibleContinuations = new List<(string Continuation, RuleState? RuleState)>();
            parseState = GetInitializedParseState(text);
        }

        public static IParseResult Parse(IAutosupportLanguageDefinition autosupportLanguageDefinition, string[] text)
        {
            return new Parser(autosupportLanguageDefinition, text).Parse();
        }

        private IParseResult Parse()
        {
            ParseUntilEndOrFailed();
            return MakeParseResult();
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

            while (!parseState.Failed && !parseState.IsAtEndOfDocument)
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
                throw new ArgumentException(nameof(parseState) + " may not be null when running " + nameof(ParseUntilEndOrFailed));

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

            return currentSymbol.Match(
                        terminal => ParseTerminal(ruleState, terminal),
                        nonTerminal => ParseNonTerminal(ruleState, nonTerminal),
                        action => ParseAction(ruleState, action),
                        oneOf => ParseOneOf(ruleState, oneOf)
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
            var text = parseState!.GetNextTextFromPosition(terminal.MinimumNumberOfCharactersToParse);

            if (text.Length < terminal.MinimumNumberOfCharactersToParse)
            {
                foreach (var content in terminal.PossibleContent)
                {
                    if (content.StartsWith(text))
                    {
                        possibleContinuations.Add((
                            content.Substring(text.Length),
                            ruleState.Clone().WithNextSymbol().TryBuild()
                            ));
                    }
                }
                return null;
            }

            if (terminal.TryParse(text))
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

        private IDictionary<int, IEnumerable<RuleState>>? ParseNonTerminal(RuleState ruleState, INonTerminal nonTerminal)
        {
            return GetPossibleNextStatesOfSymbol(
                ruleState.Clone()
                .WithNewRule(languageDefinition.Rules[nonTerminal.ReferencedRule])
                .Build());
        }

        private IDictionary<int, IEnumerable<RuleState>>? ParseAction(RuleState ruleState, IAction action)
        {
            var ruleStateBuilder = InterpretAction(ruleState, action);

            return GetPossibleNextStatesOfSymbol(
                    ruleStateBuilder.WithNextSymbol().TryBuild() ?? RuleState.FinishedRuleState
                );
        }

        private IConcreteRuleStateBuilder InterpretAction(RuleState ruleState, IAction action)
        {
            (var cmd, var args) = ExtractCommandAndArgsFromAction(action);

            switch (cmd)
            {
                case "identifier":
                    if (ruleState.Markers.TryGetValue("identifier", out var pos))
                    {
                        identifiers.Add(new Identifier()
                        {
                            Name = parseState.GetTextBetweenPositions(pos)
                        });
                        return ruleState.Clone().WithoutMarker("identifier");
                    }
                    else
                        return ruleState.Clone().WithMarker("identifier", parseState.Position);
                default:
                    return ruleState.Clone();
            }

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

        private IDictionary<int, IEnumerable<RuleState>>? ParseOneOf(RuleState ruleState, IOneOf oneOf)
        {
            var rules = oneOf.Options
                .Select(ruleName => ruleState.Clone()
                    .WithNewRule(languageDefinition.Rules[ruleName])
                    .Build());

            if (oneOf.AllowNone)
            {
                rules = rules.Append(ruleState.Clone()
                        .WithNextSymbol()
                        .TryBuild()
                        ?? RuleState.FinishedRuleState);
            }

            return rules
                .Select(GetPossibleNextStatesOfSymbol)
                .Aggregate<IDictionary<int, IEnumerable<RuleState>>?, IDictionary<int, IEnumerable<RuleState>>>(new Dictionary<int, IEnumerable<RuleState>>(), MergeDictionaries);
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

            return dict2;
        }

        private IParseResult MakeParseResult()
        {
            return new ParseResult(
                    finished: (parseState?.IsAtEndOfDocument) ?? false,
                    possibleContinuations: GetPossibleContinuations(),
                    errors: new IError[0]
                );
        }

        private string[] GetPossibleContinuations()
        {
            return parseState.RuleStates
                .Select<RuleState, (string Continuation, RuleState? RuleState)>(rs => ("", rs))
                .Union(possibleContinuations)
                .SelectMany(GetPossibleContinuationsOfRuleState)
                .Where(continuation => continuation.Length > 0)
                .ToArray();
        }

        private IEnumerable<string> GetPossibleContinuationsOfRuleState((string Continuation, RuleState? RuleState) state)
        {
            (var continuation, var ruleState) = state;

            if (ruleState == null || ruleState.IsFinished || ruleState.CurrentSymbol == null)
            {
                yield return continuation;
            }
            else
            {
                foreach(var possibleNext in ruleState.CurrentSymbol.Match(
                    terminal =>
                    {
                        return terminal.PossibleContent;
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
                        return new string[0];
                    },
                    oneOf =>
                    {
                        return new string[0];
                        /*
                        IEnumerable<(string Continuation, RuleState? RuleState)> rules = oneOf.Options
                            .Select<string, (string Continuation, RuleState? RuleState)>(ruleName => (continuation, ruleState.Clone()
                                .WithNewRule(languageDefinition.Rules[ruleName])
                                .Build()));

                        if (oneOf.AllowNone)
                        {
                            rules = rules.Append((continuation, ruleState.Clone()
                                    .WithNextSymbol()
                                    .TryBuild()));
                        }

                        return rules
                            .SelectMany(GetPossibleContinuationsOfRuleState)
                            .ToArray();
                            */
                    }))
                {
                    yield return possibleNext;
                }
            }
        }
    }
}
