using autosupport_lsp_server.LSP;
using autosupport_lsp_server.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using static autosupport_lsp_server.Parsing.RuleState;

using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace autosupport_lsp_server.Parsing.Impl
{
    internal class Parser : IParser
    {
        private readonly IAutosupportLanguageDefinition languageDefinition;
        private readonly IList<IError> errors;
        /// <summary>
        /// List of rule states that didn't continue, because the Input stopped, not because it was invalid
        /// </summary>
        private readonly IList<(Position Position, RuleState RuleState)> unfinishedRuleStates;
        private ParseState parseState;

        public Parser(IAutosupportLanguageDefinition autosupportLanguageDefinition)
        {
            languageDefinition = autosupportLanguageDefinition;

            errors = new List<IError>();
            unfinishedRuleStates = new List<(Position Position, RuleState RuleState)>();
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
            unfinishedRuleStates.Clear();
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

        private readonly StringBuilder logger = new StringBuilder();

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
                SaveUnfinishedRuleStateIfContinuationIsPossible(ruleState, terminal, actualText);
                return null;
            }
            else if (!terminal.TryParse(actualText))
            {
                logger.AppendLine($"{terminal} failed to parse <{actualText}>");

                return null;
            }
            else
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
        }

        private void SaveUnfinishedRuleStateIfContinuationIsPossible(RuleState ruleState, ITerminal terminal, string textUntilNow)
        {
            logger.AppendLine($"<{textUntilNow}> too short for {terminal} (needs at least {terminal.MinimumNumberOfCharactersToParse}, has {textUntilNow.Length})");

            if (terminal.PossibleContent.Any(possibleContent => possibleContent.StartsWith(textUntilNow)))
            {
                logger.AppendLine($"Added {terminal} to {nameof(unfinishedRuleStates)}");

                unfinishedRuleStates.Add((parseState.Position.Clone(), ruleState));
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
                    finished: parseState.IsAtEndOfDocument,
                    possibleContinuations: GetPossibleContinuations(),
                    errors: new IError[0],
                    identifiers: GetAllIdentifiers()
                );
        }

        private CompletionItem[] GetPossibleContinuations()
        {
            var identifiers = GetAllIdentifiers();
            var continuableRules = GetContinuableRules().ToList();
            var nextRules = GetNextRules().ToList();

            var halfFinishedIdentifiers = new List<CompletionItem>();
            var nextUpTypeCompatibleIdentifiers = new List<CompletionItem>();
            var nextUpTypeIncompatibleIdentifiers = new List<CompletionItem>();
            var otherIdentifiers = new List<CompletionItem>();

            var halfFinishedKeywords = new List<CompletionItem>();
            var nextUpKeywords = new List<CompletionItem>();
            var otherKeywords = new List<CompletionItem>();

            foreach (var ident in identifiers)
            {
                if (IsHalfFinished(continuableRules, ident, out var startPosition))
                    halfFinishedIdentifiers.Add(IdentifierToCompletionItem(ident, startPosition));
                else if (IsNextUpIdentifierOfCompatibleType(nextRules, ident, out var leadupWhitespace))
                    nextUpTypeCompatibleIdentifiers.Add(IdentifierToCompletionItem(ident, null, leadupWhitespace + ident.Name));
                else if (IsNextUp(nextRules, ident, out leadupWhitespace))
                    nextUpTypeIncompatibleIdentifiers.Add(IdentifierToCompletionItem(ident, null, leadupWhitespace + ident.Name));
                else
                    otherIdentifiers.Add(IdentifierToCompletionItem(ident));
            }

            var keywords = LSPUtils.GetAllKeywords(languageDefinition);

            foreach (var kw in keywords)
            {
                if (IsHalfFinishedKeyword(kw, out var startPosition))
                    halfFinishedKeywords.Add(KeywordToCompletionItem(kw, startPosition));
                else if (IsNextUpKeyword(nextRules, kw, out var leadupWhitespace))
                    nextUpKeywords.Add(KeywordToCompletionItem(leadupWhitespace + kw));
                else
                    otherKeywords.Add(KeywordToCompletionItem(kw));
            }

            var continations = halfFinishedIdentifiers
                .Union(halfFinishedKeywords)
                .Union(nextUpTypeCompatibleIdentifiers)
                .Union(nextUpKeywords)
                .Union(nextUpTypeIncompatibleIdentifiers)
                .Union(otherIdentifiers)
                .Union(otherKeywords)
                .ToArray();

            AddFilterTextToContinuations(continations);

            return continations;
        }

        private void AddFilterTextToContinuations(CompletionItem[] continations)
        {
            for (int i = 0; i < continations.Length; ++i)
            {
                continations[i].SortText = i.ToString();
            }
        }

        private Identifier[] GetAllIdentifiers()
        {
            return Identifier.CreateIdentifierSet(
                    parseState.RuleStates
                        .Union(unfinishedRuleStates.Select(urs => urs.RuleState))
                        .SelectMany(rs => rs.Identifiers))
                .ToArray();
        }


        private IEnumerable<(Position Position, RuleState)> GetContinuableRules()
        {
            return parseState.RuleStates
                .Select(rs => (parseState.Position, rs))
                .Union(unfinishedRuleStates);
        }

        private bool IsHalfFinished(IEnumerable<(Position Position, RuleState RuleState)> continuableRules, Identifier ident, [MaybeNullWhen(false)] out Position startPosition)
        {
            foreach (var continuableRule in continuableRules)
            {
                if (continuableRule.RuleState.Markers.TryGetValue(IAction.IDENTIFIER, out var position))
                {
                    string textSinceMarker = parseState.GetTextBetweenPositions(position);

                    if (ident.Name.StartsWith(textSinceMarker) && ident.Name != textSinceMarker)
                    {
                        startPosition = position;
                        return true;
                    }
                }
            }

            startPosition = null;
            return false;
        }

        private bool IsNextUpIdentifierOfCompatibleType(IEnumerable<NextRule> nextRules, Identifier ident, out string leadupWhitespace)
        {
            var nextRule = nextRules
                .Select(nr => new NextRule?(nr))
                .FirstOrDefault((rule) => rule.HasValue
                            && rule.Value.RuleState.Markers.TryGetValue(IAction.IDENTIFIER, out var position)
                            && ident.Name.StartsWith(parseState.GetTextBetweenPositions(position))
                            && (rule.Value.PossibleTypes == null
                                || ident.Type.Match(str => rule.Value.PossibleTypes.Contains(str), any => true)));

            leadupWhitespace = nextRule.HasValue ? nextRule.Value.LeadupString : "";
            return nextRule.HasValue;
        }

        private bool IsNextUp(IEnumerable<NextRule> nextRules, Identifier ident, out string leadupWhitespace)
        {
            var nextRule = nextRules
                .Select(nr => new NextRule?(nr))
                .FirstOrDefault((rule) => rule.HasValue
                            && rule.Value.RuleState.Markers.TryGetValue(IAction.IDENTIFIER, out var position)
                            && ident.Name.StartsWith(parseState.GetTextBetweenPositions(position)));

            leadupWhitespace = nextRule.HasValue ? nextRule.Value.LeadupString : "";
            return nextRule.HasValue;
        }

        private bool IsHalfFinishedKeyword(string kw, [MaybeNullWhen(false)] out Position startPosition)
        {
            foreach (var unfinishedRuleState in unfinishedRuleStates)
            {
                if (kw.StartsWith(parseState.GetTextBetweenPositions(unfinishedRuleState.Position)))
                {
                    startPosition = unfinishedRuleState.Position;
                    return true;
                }
            }

            startPosition = null;
            return false;
        }

        private static bool IsNextUpKeyword(IEnumerable<NextRule> nextRules, string kw, out string leadupWhitespace)
        {
            var nextRule = nextRules
                .Select(nr => new NextRule?(nr))
                .FirstOrDefault(rule => rule.HasValue && rule.Value.PossibleContent.Contains(kw));

            leadupWhitespace = nextRule.HasValue ? nextRule.Value.LeadupString : "";
            return nextRule.HasValue;
        }

        private CompletionItem IdentifierToCompletionItem(Identifier ident, Position? startPosition = null, string? newText = null)
        {
            if (startPosition == null && newText != null)
                startPosition = GetLastPosition();

            return new CompletionItem()
            {
                Label = ident.Name,
                Kind = ident.Kind,
                Detail = ident.Type.ToString(),
                TextEdit = startPosition == null
                    ? null
                    : new TextEdit()
                    {
                        NewText = newText ?? ident.Name,
                        Range = new Range(startPosition, GetLastPosition())
                    }
            };
        }

        private CompletionItem KeywordToCompletionItem(string keyword, Position? startPosition = null)
        {
            if (startPosition == null && keyword.Trim() != keyword)
                startPosition = GetLastPosition();

            return new CompletionItem()
            {
                Label = keyword.Trim(),
                Kind = CompletionItemKind.Keyword,
                TextEdit = startPosition == null
                    ? null
                    : new TextEdit()
                    {
                        NewText = keyword,
                        Range = new Range(startPosition, GetLastPosition())
                    }
            };
        }

        private Position GetLastPosition()
        {
            if (parseState.Text.Length == 0)
                return new Position(0, 0);

            return new Position(
                    parseState.Text.Length - 1,
                    parseState.Text[^1].Length
                );
        }

        private readonly struct NextRule
        {
            public readonly RuleState RuleState;
            public readonly string LeadupString;
            public readonly string[] PossibleContent;
            /// <summary>
            /// null means any type is valid if identifiers are syntactically valid
            /// </summary>
            public readonly string[]? PossibleTypes;

            public NextRule(RuleState ruleState, string leadupString, string[] possibleContent, string[]? possibleTypes)
            {
                RuleState = ruleState;
                LeadupString = leadupString;
                PossibleContent = possibleContent;
                PossibleTypes = possibleTypes;
            }

            public NextRule WithoutEmptyPossibleContent()
                => new NextRule(RuleState, LeadupString, PossibleContent.Where(content => content.Trim() != "").ToArray(), PossibleTypes);
        }

        private IEnumerable<NextRule> GetNextRules()
        {
            var nextRules = GetNextRulesWithLeadup(parseState.RuleStates.Select(rs => rs.Clone().WithNextSymbol().TryBuild()).WhereNotNull(), "").ToList();

            nextRules.Sort((nr1, nr2) => nr1.LeadupString.Length - nr2.LeadupString.Length);
            return nextRules;
        }

        private IEnumerable<NextRule> GetNextRulesWithLeadup(IEnumerable<RuleState> ruleStates, string leadup)
        {
            var nextRules = ruleStates.SelectMany(rs =>
                            LSPUtils.FollowUntilNextTerminalOrAction(
                                new LSPUtils.FollowUntilNextTerminalOrActionArgs<NextRule>(
                                    ruleState: rs,
                                    rules: languageDefinition.Rules,
                                    onTerminal: (rs, terminal) =>
                                    {
                                        string[]? possibleTypes = null;

                                        if (rs.ValueStore.TryGetValue(RuleStateValueStoreKey.NextType, out var nextType))
                                            possibleTypes = new[] { nextType };

                                        return new NextRule(rs, leadup, terminal.PossibleContent, possibleTypes);
                                    },
                                    onAction: (rs, action) => ActionParser.ParseAction(parseState, rs, action)
                                )));

            foreach (var nextRule in nextRules)
            {
                if (nextRule.PossibleContent.Any(content => content.Trim() != ""))
                {
                    yield return nextRule.WithoutEmptyPossibleContent();
                }
                else if (nextRule.PossibleContent.Length == 0)
                {
                    // if it doesn't have any possible content, but is in the middle of an identifier, return anyways
                    if (nextRule.RuleState.Markers.ContainsKey(IAction.IDENTIFIER))
                        yield return nextRule;
                }
                else if (nextRule.PossibleContent.Any(content => content.Length + leadup.Length > 2))
                {
                    // stop forward look to stop infinite recursion with 'Rule -> whitespace OneOf(Rule)'
                }
                else
                {
                    var nextRuleState = nextRule.RuleState.Clone().WithNextSymbol().TryBuild();

                    if (nextRuleState != null)
                        foreach (var newNextRule in GetNextRulesWithLeadup(new[] { nextRuleState }, leadup + nextRule.PossibleContent[0]))
                            yield return newNextRule;
                }
            }
        }
    }
}
