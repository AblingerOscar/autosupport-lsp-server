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

            var currentSymbol = ruleState.CurrentSymbol;
            if (currentSymbol == null)
            {
                throw new Exception("Current Symbol is null");
            }
            else
            {
                IDictionary<int, IEnumerable<RuleState>>? newParseStates = currentSymbol.Match(
                        terminal => ParseTerminal(ruleState, terminal),
                        nonTerminal => ParseNonTerminal(ruleState, nonTerminal),
                        action => ParseAction(ruleState, action),
                        operation => ParseOperation(ruleState, operation)
                    );

                newParseStates?.ForEach(parseStateKvp =>
                    parseState.ScheduleNewRuleStatesIn(parseStateKvp.Key, parseStateKvp.Value)
                );
            }
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
            ParseRuleState(
                ruleState.Clone()
                .WithNewRule(languageDefinition.Rules[nonTerminal.ReferencedRule])
                .Build());
            return null;
        }

        private IDictionary<int, IEnumerable<RuleState>>? ParseAction(RuleState ruleState, IAction action)
        {
            throw new NotImplementedException();
        }

        private IDictionary<int, IEnumerable<RuleState>>? ParseOperation(RuleState ruleState, IOperation operation)
        {
            throw new NotImplementedException();
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
