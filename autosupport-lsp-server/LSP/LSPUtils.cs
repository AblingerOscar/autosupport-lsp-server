using autosupport_lsp_server.Parsing;
using autosupport_lsp_server.Symbols;
using autosupport_lsp_server.Symbols.Impl.Terminals;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using static autosupport_lsp_server.Parsing.RuleState;

namespace autosupport_lsp_server.LSP
{
    internal static class LSPUtils
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
            public Func<RuleState, IAction, IRuleStateBuilder> OnAction;
            public Func<RuleState, T> OnFinishedRuleState;

            public FollowUntilNextTerminalOrActionArgs(
                RuleState ruleState,
                IDictionary<string, IRule> rules,
                Func<RuleState, ITerminal, T> onTerminal,
                Func<RuleState, IAction, IRuleStateBuilder> onAction,
                Func<RuleState, T> onFinishedRuleState)
            {
                RuleState = ruleState;
                Rules = rules;
                OnTerminal = onTerminal;
                OnAction = onAction;
                OnFinishedRuleState = onFinishedRuleState;
            }
        }


        public static IEnumerable<T> FollowUntilNextTerminalOrAction<T>(FollowUntilNextTerminalOrActionArgs<T> args)
        {
            if (args.RuleState.IsFinished || args.RuleState.CurrentSymbol == null)
                return new[] { args.OnFinishedRuleState(args.RuleState) };

            return args.RuleState.CurrentSymbol.Match(
                    terminal: nt => (new[] { args.OnTerminal(args.RuleState, nt) }),
                    nonTerminal: FollowThroughNonTerminal(args),
                    action: FollowThroughAction(args),
                    oneOf: FollowThroughOneOf(args)
                );
        }

        private static Func<INonTerminal, IEnumerable<T>> FollowThroughNonTerminal<T>(FollowUntilNextTerminalOrActionArgs<T> args)
        {
            return nt =>
                FollowUntilNextTerminalOrAction(
                    new FollowUntilNextTerminalOrActionArgs<T>(
                        args.RuleState.Clone().WithNewRule(args.Rules[nt.ReferencedRule]).Build(),
                        args.Rules,
                        args.OnTerminal,
                        args.OnAction,
                        args.OnFinishedRuleState));
        }

        private static Func<IAction, IEnumerable<T>> FollowThroughAction<T>(FollowUntilNextTerminalOrActionArgs<T> args)
        {
            return action =>
            {
                var ruleStateBuilder = args.OnAction.Invoke(args.RuleState, action);
                return FollowUntilNextTerminalOrAction(
                    new FollowUntilNextTerminalOrActionArgs<T>(
                        ruleStateBuilder.WithNextSymbol().Build(),
                        args.Rules,
                        args.OnTerminal,
                        args.OnAction,
                        args.OnFinishedRuleState));
            };
        }

        private static Func<IOneOf, IEnumerable<T>> FollowThroughOneOf<T>(FollowUntilNextTerminalOrActionArgs<T> args)
        {
            return oneOf =>
            {
                var newRuleStates = oneOf.Options
                    .Select(ruleName => args.RuleState.Clone()
                        .WithNewRule(args.Rules[ruleName])
                        .Build());

                if (oneOf.AllowNone)
                {
                    newRuleStates = newRuleStates.Append(args.RuleState.Clone()
                            .WithNextSymbol()
                            .Build());
                }

                return newRuleStates
                    .SelectMany(nrs => FollowUntilNextTerminalOrAction(
                        new FollowUntilNextTerminalOrActionArgs<T>(nrs, args.Rules, args.OnTerminal, args.OnAction, args.OnFinishedRuleState)));
            };
        }

        public static IEnumerable<Identifier> GetCrossDocumentsMergedIdentifiersOf(IEnumerable<Document> documents, Identifier[] selectedIdentifiers)
        {
            var identifierComparer = new Identifier.IdentifierComparer();
            return Identifier.MergeIdentifiers(
                    documents
                        .Select(doc => (doc.ParseResult?.Identifiers))
                        .WhereNotNull()
                        .ToArray())
                .Where(identifier => selectedIdentifiers.Any(selIden => identifierComparer.Equals(selIden, identifier)));
        }

        public static LocationOrLocationLink? TransformToLocationOrLocationLink(IReference originalReference, IReferenceWithEnclosingRange targetReference, bool hasLinkSupport)
        {
            if (hasLinkSupport && targetReference.EnclosingRange != null)
            {
                return new LocationLink()
                {
                    OriginSelectionRange = originalReference.Range,
                    TargetRange = targetReference.EnclosingRange,
                    TargetSelectionRange = targetReference.Range,
                    TargetUri = targetReference.Uri

                };
            }

            return new Location()
            {
                Range = targetReference.Range,
                Uri = targetReference.Uri
            };
        }

        public static CompletionItemKind String2Kind(string kind)
        {
            return kind switch
            {
                "Text" => CompletionItemKind.Text,
                "Method" => CompletionItemKind.Method,
                "Function" => CompletionItemKind.Function,
                "Constructor" => CompletionItemKind.Constructor,
                "Field" => CompletionItemKind.Field,
                "Variable" => CompletionItemKind.Variable,
                "Class" => CompletionItemKind.Class,
                "Interface" => CompletionItemKind.Interface,
                "Module" => CompletionItemKind.Module,
                "Property" => CompletionItemKind.Property,
                "Unit" => CompletionItemKind.Unit,
                "Value" => CompletionItemKind.Value,
                "Enum" => CompletionItemKind.Enum,
                "Keyword" => CompletionItemKind.Keyword,
                "Snippet" => CompletionItemKind.Snippet,
                "Color" => CompletionItemKind.Color,
                "File" => CompletionItemKind.File,
                "Reference" => CompletionItemKind.Reference,
                "Folder" => CompletionItemKind.Folder,
                "EnumMember" => CompletionItemKind.EnumMember,
                "Constant" => CompletionItemKind.Constant,
                "Struct" => CompletionItemKind.Struct,
                "Event" => CompletionItemKind.Event,
                "Operator" => CompletionItemKind.Operator,
                "TypeParameter" => CompletionItemKind.TypeParameter,
                _ => throw new System.ArgumentException($"Could not convert {kind} into the correct {nameof(CompletionItemKind)}"),
            };
        }

        public static string Kind2String(CompletionItemKind kind)
        {
            return kind switch
            {
                CompletionItemKind.Text => "Text",
                CompletionItemKind.Method => "Method",
                CompletionItemKind.Function => "Function",
                CompletionItemKind.Constructor => "Constructor",
                CompletionItemKind.Field => "Field",
                CompletionItemKind.Variable => "Variable",
                CompletionItemKind.Class => "Class",
                CompletionItemKind.Interface => "Interface",
                CompletionItemKind.Module => "Module",
                CompletionItemKind.Property => "Property",
                CompletionItemKind.Unit => "Unit",
                CompletionItemKind.Value => "Value",
                CompletionItemKind.Enum => "Enum",
                CompletionItemKind.Keyword => "Keyword",
                CompletionItemKind.Snippet => "Snippet",
                CompletionItemKind.Color => "Color",
                CompletionItemKind.File => "File",
                CompletionItemKind.Reference => "Reference",
                CompletionItemKind.Folder => "Folder",
                CompletionItemKind.EnumMember => "EnumMember",
                CompletionItemKind.Constant => "Constant",
                CompletionItemKind.Struct => "Struct",
                CompletionItemKind.Event => "Event",
                CompletionItemKind.Operator => "Operator",
                CompletionItemKind.TypeParameter => "TypeParameter",
                _ => throw new System.ArgumentException($"Could not convert supposed {nameof(CompletionItemKind)} {kind} into a string"),
            };
        }
        
        public static void ReplaceInText(IList<string> text, Position start, Position end, IList<string> replacementText)
        {
            RemoveTextInRange(text, start, end);
            InsertText(text, start, replacementText);
        }

        public static void RemoveTextInRange(IList<string> text, Position start, Position end)
        {
            // Merge first and last line
            string restStrOnEndLine = 
                text.Count > end.Line
                ? text[(int)end.Line].Substring((int)end.Character)
                : "";
            text[(int)start.Line] = text[(int)start.Line].Substring(0, (int)start.Character) + restStrOnEndLine;

            // Remove all lines in between and the last line
            for (int i = (int)end.Line; i > (int)start.Line; --i)
            {
                text.RemoveAt(i);
            }
        }

        private static void InsertText(IList<string> baseText, Position pos, IList<string> insertionText)
        {
            if (insertionText.Count == 0)
            {
                return;
            }

            string restStrOnEndLine = baseText[(int)pos.Line].Substring((int)pos.Character);

            if (insertionText.Count == 1)
            {
                baseText[(int)pos.Line] = baseText[(int)pos.Line].Substring(0, (int)pos.Character) + insertionText[0] + restStrOnEndLine;
            }
            else
            {
                baseText.Insert((int)pos.Line + 1, insertionText[insertionText.Count - 1] + baseText[(int)pos.Line].Substring((int)pos.Character));
                baseText[(int)pos.Line] = baseText[(int)pos.Line].Substring(0, (int)pos.Character) + insertionText[0];

                for (int i = insertionText.Count - 2; i > 0; ++i)
                {
                    baseText.Insert((int)pos.Line + 1, insertionText[i]);
                }
            }
        }

        public static IList<string> ConvertTextToList(string text)
        {
            return new List<string>(text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None));
        }
    }
}
