using autosupport_lsp_server.LSP;
using autosupport_lsp_server.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
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
            parseState = new ParseState(new Uri("nothing://"), new string[0], new Position(), new List<RuleState>(0));
        }

        public IParseResult Parse(Uri uri, string[] text)
        {
            SetupDefaultValues(uri, text);
            ParseUntilEndOrFailed();
            return MakeParseResult();
        }

        private void SetupDefaultValues(Uri uri, string[] text)
        {
            errors.Clear();
            possibleContinuations.Clear();
            parseState = GetInitializedParseState(uri, text);
        }

        private ParseState GetInitializedParseState(Uri uri, string[] text)
        {
            var ruleStates =
                (from startRuleName in languageDefinition.StartRules
                 select new RuleState(languageDefinition.Rules[startRuleName]))
                 .ToList();

            return new ParseState(uri, text, new Position(0, 0), ruleStates);
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

                logger.AppendLine("===== Next step");
                parseState.NextStep();
            }
            logger.AppendLine("Done.");
        }

        private void ParseRuleState(RuleState ruleState)
        {
            if (parseState == null)
                throw new ArgumentException(nameof(parseState) + " may not be null when running " + nameof(ParseRuleState));

            var newParseStates = GetPossibleNextStatesOfSymbol(ruleState);
            ScheduleNextParseStates(newParseStates);
        }
           
        private StringBuilder logger = new StringBuilder();

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
                .WhereNotNull()
                .ToList();

            foreach (var dict in nextStates)
            {
                foreach (var kvp in dict)
                {
                    logger.AppendLine($"{kvp.Key}: {kvp.Value.JoinToString(",\n\t\t")}");
                }
            }

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
                logger.AppendLine($"<{actualText}> to short for {terminal} (needs at least {terminal.MinimumNumberOfCharactersToParse}, has {actualText.Length})");

                SaveAsPossibleContinuation(ruleState, terminal, actualText);
                return null;
            }

            if (terminal.TryParse(actualText))
            {
                logger.AppendLine($"{terminal} successfully parsed (at least the start of) <{actualText}>");

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
                logger.AppendLine($"{terminal} failed to parse <{actualText}>");
                return null;
            }
        }

        private void SaveAsPossibleContinuation(RuleState ruleState, ITerminal terminal, string actualText)
        {
            foreach (var expectedText in terminal.PossibleContent)
            {
                if (!expectedText.StartsWith(actualText))
                    continue;

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

        private IConcreteRuleStateBuilder InterpretAction(RuleState ruleState, IAction action)
        {
            return ActionParser.ParseAction(parseState, ruleState, action);
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

        private IParseResult MakeParseResult()
        {
            return new ParseResult(
                    finished: (parseState?.IsAtEndOfDocument) ?? false,
                    possibleContinuations: GetPossibleContinuations(),
                    errors: new IError[0],
                    identifiers: GetAllIdentifiers()
                );
        }

        private Identifier[] GetAllIdentifiers()
        {
            return (parseState?.RuleStates ?? Enumerable.Empty<RuleState>())
                .Union(possibleContinuations.Select(pc => pc.RuleState))
                .WhereNotNull()
                .SelectMany(rs => rs?.Identifiers)
                .ToArray();
        }

        private CompletionItem[] GetPossibleContinuations()
        {
            List<CompletionItemPrioritizationItem> continuations = new List<CompletionItemPrioritizationItem>();

            // continuations of keywords
            continuations.AddRange(GetContinuationsOfUnfinishedTerminals(possibleContinuations));

            // all keywords
            continuations.AddRange(
                LSPUtils.GetAllKeywordsAsCompletionItems(languageDefinition)
                    .Select(cont =>
                        new CompletionItemPrioritizationItem(
                            ContinuationType.Keyword, cont
                        )));

            foreach (var ruleState in parseState.RuleStates)
            {
                continuations.AddRange(GetUnfinishedIdentifiers(ruleState));
                continuations.AddRange(GetNextIdentifiers(ruleState));
                continuations.AddRange(GetAllIdentifiersAsCompletionItem(ruleState));
                continuations.AddRange(GetContinuationsOfNextTerminal(ruleState));
            }

            continuations.RemoveAll(cont =>
                            cont.CompletionItem.Label.Trim() == ""
                            || cont.CompletionItem.TextEdit?.NewText == "");

            continuations.Sort();

            return continuations
                .Select(cont => cont.CompletionItem)
                .Distinct(new CompletionItemContentEqualityComparer())
                .ToArray();
        }

        private IEnumerable<CompletionItemPrioritizationItem> GetContinuationsOfUnfinishedTerminals(IList<(CompletionItem Continuation, RuleState? RuleState)> possibleContinuations)
        {
            return possibleContinuations.SelectMany(possibleCont =>
            {
                if (possibleCont.Continuation.Label.Trim() != "")
                    // if it's not just whitespace, just return it
                    return new[] { 
                        new CompletionItemPrioritizationItem(ContinuationType.CompletionOfKeyword, possibleCont.Continuation)
                    };
                else if (possibleCont.RuleState == null)
                {   // if it's only whitespace, but there is no further valid rule, do not suggest it
                    return Enumerable.Empty<CompletionItemPrioritizationItem>();
                }
                else
                {   // if it's only whitespace and there are more valid rules, then find all possible next terminals
                    //   and if their continuation is not only whitespace, too, then it will be appended and returned
                    return LSPUtils.FollowUntilNextTerminalOrAction(new LSPUtils.FollowUntilNextTerminalOrActionArgs<string[]>(
                            ruleState: possibleCont.RuleState,
                            rules: languageDefinition.Rules,
                            onTerminal: (ruleState, terminal) => terminal.PossibleContent,
                            onAction: (ruleState, action) => ruleState.Clone()
                        ))
                        .SelectMany(continuations => continuations)
                        .Where(cont => cont.Trim() != "")
                        .Select(cont =>
                        new CompletionItemPrioritizationItem(ContinuationType.CompletionOfKeyword, new CompletionItem()
                        {
                            Label = cont,
                            Kind = CompletionItemKind.Keyword,
                            TextEdit = possibleCont.Continuation.TextEdit == null
                                ? null
                                : new TextEdit()
                                {
                                    NewText = possibleCont.Continuation.TextEdit.NewText + cont,
                                    Range = possibleCont.Continuation.TextEdit.Range
                                }
                        }));
                }
            });
        }

        private IEnumerable<CompletionItemPrioritizationItem> GetUnfinishedIdentifiers(RuleState ruleState)
        {
            if (ruleState.Markers.TryGetValue(IAction.IDENTIFIER, out var pos))
            {
                var textSinceMarker = parseState.GetTextBetweenPositions(pos);

                var continuableIdentifiers = ruleState.Identifiers
                    .Where(identifier => identifier.Name.StartsWith(textSinceMarker));

                foreach (var identifier in continuableIdentifiers)
                {
                    yield return new CompletionItemPrioritizationItem(
                        ContinuationType.CompletionOfIdentifier,
                        new CompletionItem()
                        {
                            Label = identifier.Name,
                            Kind = identifier.Kind,
                            TextEdit = new TextEdit()
                            {
                                NewText = identifier.Name.Substring(textSinceMarker.Length),
                                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                                    parseState.Position, parseState.Position
                                    )
                            }
                        },
                        ruleState.ValueStore.TryGetValue(RuleStateValueStoreKey.NextType, out string? nextType) && identifier.Type == nextType);
                }
            }
        }

        private IEnumerable<CompletionItemPrioritizationItem> GetNextIdentifiers(RuleState ruleState)
        {
            if (!ruleState.Markers.ContainsKey(IAction.IDENTIFIER))
            {
                var newRuleStates = LSPUtils.FollowUntilNextTerminalOrAction(new LSPUtils.FollowUntilNextTerminalOrActionArgs<RuleState>(
                    ruleState,
                    languageDefinition.Rules,
                    onTerminal: (ruleState, terminal) => ruleState,
                    onAction: InterpretAction
                    ));


                foreach (var newRuleState in newRuleStates)
                {
                    if (newRuleState != null && newRuleState.Markers.ContainsKey(IAction.IDENTIFIER))
                    {
                        foreach (var item in GetUnfinishedIdentifiers(newRuleState))
                        {
                            if (item.IsExpectedType.HasValue && item.IsExpectedType.Value)
                            {
                                item.ContinuationSource = ContinuationType.NextIdentifier;
                                yield return item;
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<CompletionItemPrioritizationItem> GetAllIdentifiersAsCompletionItem(RuleState ruleState)
        {
            foreach (var identifier in ruleState.Identifiers)
            {
                yield return new CompletionItemPrioritizationItem(
                    ContinuationType.Identifier,
                    new CompletionItem()
                    {
                        Label = identifier.Name,
                        Kind = identifier.Kind
                    });
            }
        }

        private IEnumerable<CompletionItemPrioritizationItem> GetContinuationsOfNextTerminal(RuleState ruleState)
        {
            return LSPUtils.FollowUntilNextTerminalOrAction(new LSPUtils.FollowUntilNextTerminalOrActionArgs<IEnumerable<CompletionItemPrioritizationItem>>(
                  ruleState,
                  languageDefinition.Rules,
                  onTerminal: (rs, terminal) =>
                    terminal.PossibleContent.Select(possibleContent =>
                        new CompletionItemPrioritizationItem(
                            ContinuationType.NextKeyword,
                            new CompletionItem()
                            {
                                Label = possibleContent,
                                Kind = CompletionItemKind.Keyword
                            })),
                  onAction: (rs, action) => rs.Clone()
                ))
                .SelectMany(continuations => continuations);
        }
    }
}
