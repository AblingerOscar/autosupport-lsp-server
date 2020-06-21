using autosupport_lsp_server.LSP;
using autosupport_lsp_server.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using static autosupport_lsp_server.Parsing.Error;
using static autosupport_lsp_server.Parsing.RuleState;

using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace autosupport_lsp_server.Parsing.Impl
{
    public class ActionParser
    {
        internal readonly struct ParserInformation
        {
            public readonly Uri Uri;
            public readonly Position Position;
            public readonly Func<Position, string> GetTextUpToPosition;

            public ParserInformation(Uri uri, Position position, Func<Position, string> getTextUpToPosition)
            {
                Uri = uri;
                Position = position;
                GetTextUpToPosition = getTextUpToPosition;
            }

            public static implicit operator ParserInformation(ParseState parseState)
                => new ParserInformation(parseState.Uri, parseState.Position, start => parseState.GetTextBetweenPositions(start));
        }


        private delegate IRuleStateBuilder SpecificPostActionParser(ParserInformation parseInfo, RuleState ruleState, IAction action, Position startOfMarkings);

        internal static IRuleStateBuilder ParseAction(ParserInformation parseInfo, RuleState ruleState, IAction action)
        {
            switch (action.GetBaseCommand())
            {
                case IAction.IDENTIFIER:
                    return ParsePostAction(parseInfo, ruleState, action, ParseIdentifierAction);
                case IAction.IDENTIFIER_TYPE:
                    if (action.GetArguments()[0] == IAction.IDENTIFIER_TYPE_ARG_SET)
                        // do immediate action
                        return ruleState.Clone().WithValue(RuleStateValueStoreKey.NextType, action.GetArguments()[1]);

                    return ParsePostAction(parseInfo, ruleState, action, ParseIdentifierTypeAction);
                case IAction.IDENTIFIER_KIND:
                    if (action.GetArguments()[0] == IAction.IDENTIFIER_KIND_ARG_SET)
                        return ruleState.Clone().WithValue(RuleStateValueStoreKey.NextKind, action.GetArguments()[1]);
                    throw new ArgumentException($"Given action is not supported without parameter '{IAction.IDENTIFIER_KIND_ARG_SET}': {action}");
                case IAction.DECLARATION:
                    return ParseDeclaration(ruleState);
                case IAction.IMPLEMENTATION:
                    return ParseImplementation(ruleState);
            }

            throw new ArgumentException("Given action is not supported: " + action.ToString());
        }

        private static IRuleStateBuilder ParsePostAction(
            ParserInformation parseInfo,
            RuleState ruleState,
            IAction action,
            SpecificPostActionParser specificActionParser)
        {
            if (ruleState.Markers.TryGetValue(action.GetBaseCommand(), out var pos))
            {
                var newRuleState = specificActionParser.Invoke(parseInfo, ruleState, action, pos);
                return newRuleState.WithoutMarker(action.GetBaseCommand());
            }
            else
            {
                return ruleState.Clone().WithMarker(action.GetBaseCommand(), parseInfo.Position);
            }
        }

        private static IRuleStateBuilder ParseIdentifierAction(ParserInformation parseInfo, RuleState ruleState, IAction action, Position startOfMarkings)
        {
            var textBetweenMarkers = parseInfo.GetTextUpToPosition(startOfMarkings);
            var errors = new List<Error>();

            CompletionItemKind? kind = null;

            if (ruleState.ValueStore.TryGetValue(RuleStateValueStoreKey.NextKind, out string? kindStr))
                kind = LSPUtils.String2Kind(kindStr);

            ruleState.ValueStore.TryGetValue(RuleStateValueStoreKey.NextType, out string? type);

            var declaration = GetIdentifierDeclaration(parseInfo, ruleState, startOfMarkings);
            var implementation = GetIdentifierImplementation(parseInfo, ruleState, startOfMarkings);

            if (textBetweenMarkers.Trim() != "")
            {
                var identifier = ruleState.Identifiers.FirstOrDefault(i => i.Name == textBetweenMarkers);

                if (identifier == null)
                {
                    ruleState.Identifiers.Add(new Identifier()
                    {
                        Name = textBetweenMarkers,
                        References = new List<IReference>() {
                            new Reference(parseInfo.Uri, new Range(startOfMarkings, parseInfo.Position.Clone()))
                       },
                        Types = new IdentifierType(type),
                        Kind = kind,
                        Declaration = declaration,
                        Implementation = implementation
                    });
                }
                else
                {
                    identifier.References.Add(
                        new Reference(parseInfo.Uri, new Range(startOfMarkings, parseInfo.Position.Clone())));

                    if (kind != null)
                    {
                        if (identifier.Kind == null)
                            identifier.Kind = kind.Value;
                        else if (kind != identifier.Kind)
                            errors.Add(new Error(
                                    parseInfo.Uri,
                                    new Range(startOfMarkings, parseInfo.Position.Clone()),
                                    DiagnosticSeverity.Error,
                                    $"Expected {kind}, but found {identifier.Kind}"
                                ));
                    }


                    if (declaration != null)
                    {
                        if (identifier.Declaration == null)
                            identifier.Declaration = declaration;
                        else if (identifier.Declaration != null)
                            errors.Add(new Error(
                                    parseInfo.Uri,
                                    new Range(startOfMarkings, parseInfo.Position.Clone()),
                                    DiagnosticSeverity.Error,
                                    $"{identifier.Name} is already declared",
                                    new ConnectedError(
                                        identifier.Declaration.Uri,
                                        identifier.Declaration.Range,
                                        $"Declaration of {identifier.Name}"
                                        )
                                ));
                    }

                    if (implementation != null)
                    {
                        if (identifier.Implementation == null)
                            identifier.Implementation = implementation;
                        else if (identifier.Implementation != null)
                            errors.Add(new Error(
                                    parseInfo.Uri,
                                    new Range(startOfMarkings, parseInfo.Position.Clone()),
                                    DiagnosticSeverity.Error,
                                    $"{identifier.Name} is already implemented"
                                ));
                    }
                }
            }

            var nextRuleState = ruleState.Clone();

            if (kind != null)
                nextRuleState = nextRuleState.WithoutValue(RuleStateValueStoreKey.NextKind);

            if (type != null)
                nextRuleState = nextRuleState.WithoutValue(RuleStateValueStoreKey.NextType);

            if (declaration != null)
                nextRuleState = nextRuleState.WithoutValue(RuleStateValueStoreKey.IsDeclaration);

            if (implementation != null)
                nextRuleState = nextRuleState.WithoutValue(RuleStateValueStoreKey.IsImplementation);

            return nextRuleState.WithAdditionalErrors(errors);
        }

        private static IReferenceWithEnclosingRange? GetIdentifierDeclaration(ParserInformation parseInfo, RuleState ruleState, Position startOfMarkings)
        {
            if (ruleState.ValueStore.ContainsKey(RuleStateValueStoreKey.IsDeclaration))
                return new ReferenceWithEnclosingRange(parseInfo.Uri, new Range(startOfMarkings, parseInfo.Position.Clone()), null);

            return null;
        }

        private static IReferenceWithEnclosingRange? GetIdentifierImplementation(ParserInformation parseInfo, RuleState ruleState, Position startOfMarkings)
        {
            if (ruleState.ValueStore.ContainsKey(RuleStateValueStoreKey.IsImplementation))
                return new ReferenceWithEnclosingRange(parseInfo.Uri, new Range(startOfMarkings, parseInfo.Position.Clone()), null);

            return null;
        }

        private static IRuleStateBuilder ParseIdentifierTypeAction(ParserInformation parseInfo, RuleState ruleState, IAction action, Position startOfMarkings)
            => ruleState.Clone().WithValue(RuleStateValueStoreKey.NextType, parseInfo.GetTextUpToPosition(startOfMarkings));

        private static IRuleStateBuilder ParseDeclaration(RuleState ruleState)
            => ruleState.Clone().WithValue(RuleStateValueStoreKey.IsDeclaration);

        private static IRuleStateBuilder ParseImplementation(RuleState ruleState)
            => ruleState.Clone().WithValue(RuleStateValueStoreKey.IsImplementation);
    }
}
