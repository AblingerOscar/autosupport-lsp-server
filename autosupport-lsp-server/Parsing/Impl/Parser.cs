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
                SaveAsPossibleContinuation(ruleState, terminal, actualText);
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
            (var cmd, var args) = ExtractCommandAndArgsFromAction(action);

            switch (cmd)
            {
                case IAction.IDENTIFIER:
                    if (ruleState.Markers.TryGetValue(IAction.IDENTIFIER, out var pos))
                    {
                        string name = parseState.GetTextBetweenPositions(pos);

                        if (name.Trim() == "")
                            break;

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
            List<(ContinuationSource Source, CompletionItem Item)> continuations = new List<(ContinuationSource Source, CompletionItem Item)>();

            // continuations of keywords
            continuations.AddRange(GetContinuationsOfUnfinishedTerminals(possibleContinuations));

            // all keywords
            continuations.AddRange(
                LSPUtils.GetAllKeywordsAsCompletionItems(languageDefinition).Select(cont =>
                    (ContinuationSource.Keyword, cont)));

            foreach (var ruleState in parseState.RuleStates)
            {
                continuations.AddRange(GetUnfinishedIdentifiers(ruleState));
                continuations.AddRange(GetAllIdentifiersAsCompletionItem(ruleState));
                continuations.AddRange(GetContinuationsOfNextTerminal(ruleState));
            }

            return SortContinuations(continuations.Where(cont =>
                            cont.Item.Label.Trim() != ""
                            && cont.Item.TextEdit?.NewText != ""))
                    .Distinct(new CompletionItemContentEqualityComparer())
                    .ToArray();
        }

        private IEnumerable<(ContinuationSource Source, CompletionItem Item)> GetContinuationsOfUnfinishedTerminals(IList<(CompletionItem Continuation, RuleState? RuleState)> possibleContinuations)
        {
            return possibleContinuations.SelectMany(possibleCont =>
            {
                if (possibleCont.Continuation.Label.Trim() != "")
                    // if it's not just whitespace, just return it
                    return new[] { (ContinuationSource.CompletionOfKeyword, possibleCont.Continuation) };
                else if (possibleCont.RuleState == null)
                {   // if it's only whitespace, but there is no further valid rule, do not suggest it
                    return Enumerable.Empty<(ContinuationSource Source, CompletionItem Item)>();
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
                        .Select(cont => (ContinuationSource.CompletionOfKeyword, new CompletionItem()
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

        private IEnumerable<(ContinuationSource Source, CompletionItem Item)> GetUnfinishedIdentifiers(RuleState ruleState)
        {
            if (ruleState.Markers.TryGetValue(IAction.IDENTIFIER, out var pos))
            {
                var textSinceMarker = parseState.GetTextBetweenPositions(pos);

                var continuableIdentifiers = ruleState.Identifiers
                    .Where(identifier => identifier.Name.StartsWith(textSinceMarker));

                foreach (var identifier in continuableIdentifiers)
                {
                    yield return (
                        ContinuationSource.CompletionOfIdentifier,
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
                        });
                }
            }
        }

        private IEnumerable<(ContinuationSource Source, CompletionItem Item)> GetAllIdentifiersAsCompletionItem(RuleState ruleState)
        {
            foreach (var identifier in ruleState.Identifiers)
            {
                yield return (
                    ContinuationSource.Identifier,
                    new CompletionItem()
                    {
                        Label = identifier.Name,
                        Kind = identifier.Kind
                    });
            }
        }

        private IEnumerable<(ContinuationSource Source, CompletionItem Item)> GetContinuationsOfNextTerminal(RuleState ruleState)
        {
            return LSPUtils.FollowUntilNextTerminalOrAction(new LSPUtils.FollowUntilNextTerminalOrActionArgs<IEnumerable<CompletionItem>>(
                  ruleState,
                  languageDefinition.Rules,
                  onTerminal: (rs, terminal) =>
                    terminal.PossibleContent.Select(possibleContent =>
                        new CompletionItem()
                        {
                            Label = possibleContent,
                            Kind = CompletionItemKind.Keyword
                        }),
                  onAction: (rs, action) => rs.Clone()
                ))
                .SelectMany(continuations => continuations)
                .Select(continuation => (ContinuationSource.NextKeyword, continuation));
        }

        private enum ContinuationSource
        {
            CompletionOfIdentifier = 4,
            CompletionOfKeyword = 3,
            NextKeyword = 2,
            Identifier = 1,
            Keyword = 0
        }

        private IList<CompletionItem> SortContinuations(IEnumerable<(ContinuationSource Source, CompletionItem Item)> continuations)
        {
            var priorityList = continuations
                .ToList();

            priorityList.Sort((item1, item2) => (int)item2.Source - (int)item1.Source);

            return priorityList.Select(item => item.Item).ToArray();
        }
    }
}
