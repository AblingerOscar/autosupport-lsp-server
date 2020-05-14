using autosupport_lsp_server.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace autosupport_lsp_server.Parsing.Impl
{
    internal class Parser
    {
        private readonly ParseState parseState;
        private readonly IAutosupportLanguageDefinition languageDefinition;

        private Parser(IAutosupportLanguageDefinition autosupportLanguageDefinition, Document document)
        {
            languageDefinition = autosupportLanguageDefinition;

            parseState = GetInitializedParseState(document);
        }

        public static IParseResult Parse(IAutosupportLanguageDefinition autosupportLanguageDefinition, Document document)
        {
            return new Parser(autosupportLanguageDefinition, document).Parse();
        }

        private IParseResult Parse()
        {
            ParseUntilEndOrFailed();
            return MakeParseResult();
        }

        private ParseState GetInitializedParseState(Document document)
        {
            var ruleStates =
                (from startRuleName in languageDefinition.StartRules
                 select new RuleState(languageDefinition.Rules[startRuleName]))
                 .ToList();

            return new ParseState(document, new Position(0, 0), ruleStates);
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
            if (terminal.TryParse(parseState!.GetNextTextFromPosition(terminal.MinimumNumberOfCharactersToParse)))
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
            throw new NotImplementedException();
        }

        private IDictionary<int, IEnumerable<RuleState>>? ParseOneOf(RuleState ruleState, IOneOf oneOf)
        {
            return oneOf.Options
                .Select(ruleName => ruleState.Clone()
                    .WithNewRule(languageDefinition.Rules[ruleName])
                    .Build())
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
            return new ParseResult()
            {
                FinishedSuccessfully = (!parseState?.Failed) ?? false
            };
        }
    }
}
