using autosupport_lsp_server.Parsing;
using autosupport_lsp_server.Symbols;
using autosupport_lsp_server.Symbols.Impl;
using autosupport_lsp_server.Symbols.Impl.Terminals;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using static autosupport_lsp_server.Parsing.RuleState;

namespace autosupport_lsp_server.LSP
{
    internal class LSPUtils
    {
        private static DocumentSelector? documentSelector;

        public static DocumentSelector GetDocumentSelector(IAutosupportLanguageDefinition languageDefinition)
        {
            if (documentSelector == null)
            {
                documentSelector = new DocumentSelector(
                            DocumentFilter.ForPattern(languageDefinition.LanguageFilePattern),
                            DocumentFilter.ForLanguage(languageDefinition.LanguageId)
                            );
            }

            return documentSelector;
        }

        public static IEnumerable<CompletionItem> GetAllKeywordsAsCompletionItems(IAutosupportLanguageDefinition languageDefinition)
        {
            return GetAllKeywords(languageDefinition)
                .Select(str =>
                {
                    return new CompletionItem()
                    {
                        Label = str,
                        Kind = CompletionItemKind.Keyword
                    };
                });
        }

        public static IEnumerable<string> GetAllKeywords(IAutosupportLanguageDefinition languageDefinition)
        {
            return languageDefinition.Rules
                .SelectMany(rule => rule.Value.Symbols)
                .Select(symbol =>
                    symbol.Match(
                        terminal: terminal =>
                            terminal is StringTerminal stringTerminal
                                ? stringTerminal.String
                                : null,
                            nonTerminal: (_) => null,
                            oneOf: (_) => null,
                            action: (_) => null))
                .WhereNotNull()
                .Distinct();
        }

        internal class FollowUntilNextTerminalOrActionArgs<T>
        {
            public RuleState RuleState;
            public IDictionary<string, IRule> Rules;

            public Func<RuleState, ITerminal, T> OnTerminal;
            public Func<RuleState, IAction, IConcreteRuleStateBuilder> OnAction;

            public FollowUntilNextTerminalOrActionArgs(
                RuleState ruleState,
                IDictionary<string, IRule> rules,
                Func<RuleState, ITerminal, T> onTerminal,
                Func<RuleState, IAction, IConcreteRuleStateBuilder> onAction)
            {
                RuleState = ruleState;
                Rules = rules;
                OnTerminal = onTerminal;
                OnAction = onAction;
            }
        }


        public static IEnumerable<T> FollowUntilNextTerminalOrAction<T>(FollowUntilNextTerminalOrActionArgs<T> args)
        {
            if (args.RuleState.IsFinished || args.RuleState.CurrentSymbol == null)
                return Enumerable.Empty<T>();

            return args.RuleState.CurrentSymbol.Match(
                    terminal: nt => new T[] { args.OnTerminal(args.RuleState, nt) },
                    nonTerminal: nt =>
                        FollowUntilNextTerminalOrAction(
                            new FollowUntilNextTerminalOrActionArgs<T>(
                                args.RuleState.Clone().WithNewRule(args.Rules[nt.ReferencedRule]).Build(),
                                args.Rules,
                                args.OnTerminal,
                                args.OnAction)),
                    action: action =>
                    {
                        var ruleStateBuilder = args.OnAction.Invoke(args.RuleState, action);
                        return FollowUntilNextTerminalOrAction(
                            new FollowUntilNextTerminalOrActionArgs<T>(
                                ruleStateBuilder.WithNextSymbol().TryBuild() ?? RuleState.FinishedRuleState,
                                args.Rules,
                                args.OnTerminal,
                                args.OnAction));
                    },
                    oneOf: oneOf =>
                    {
                        var newRuleStates = oneOf.Options
                            .Select(ruleName => args.RuleState.Clone()
                                .WithNewRule(args.Rules[ruleName])
                                .Build());

                        if (oneOf.AllowNone)
                        {
                            newRuleStates = newRuleStates.Append(args.RuleState.Clone()
                                    .WithNextSymbol()
                                    .TryBuild()
                                    ?? RuleState.FinishedRuleState);
                        }

                        return newRuleStates
                            .SelectMany(nrs => FollowUntilNextTerminalOrAction(
                                new FollowUntilNextTerminalOrActionArgs<T>(nrs, args.Rules, args.OnTerminal, args.OnAction)));
                    }
                );
        }
    }
}
