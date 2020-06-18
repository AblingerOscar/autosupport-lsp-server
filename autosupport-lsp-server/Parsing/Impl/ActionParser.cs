using autosupport_lsp_server.LSP;
using autosupport_lsp_server.Shared;
using autosupport_lsp_server.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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


        private delegate IConcreteRuleStateBuilder SpecificPostActionParser(ParserInformation parseInfo, RuleState ruleState, IAction action, Position startOfMarkings);

        internal static IConcreteRuleStateBuilder ParseAction(ParserInformation parseInfo, RuleState ruleState, IAction action)
        {
            switch(action.GetBaseCommand())
            {
                case IAction.IDENTIFIER:
                    return ParsePostAction(parseInfo, ruleState, action, ParseIdentifierAction);
                case IAction.IDENTIFIER_TYPE:
                    if (action.GetArguments()[0] == IAction.IDENTIFIER_TYPE_ARG_SET)
                        // do immediate action
                        return ruleState.Clone().WithValue(RuleStateValueStoreKey.NextType, action.GetArguments()[1]);

                    return ParsePostAction(parseInfo, ruleState, action, ParseIdentifierTypeAction);
                case IAction.DECLARATION:
                    return ParseDeclaration(ruleState);
            }

            throw new ArgumentException("Given action is not supported: " + action.ToString());
        }

        private static IConcreteRuleStateBuilder ParsePostAction(
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

        private static IConcreteRuleStateBuilder ParseIdentifierAction(ParserInformation parseInfo, RuleState ruleState, IAction action, Position startOfMarkings)
        {
            var textBetweenMarkers = parseInfo.GetTextUpToPosition(startOfMarkings);

            ruleState.ValueStore.TryGetValue(RuleStateValueStoreKey.NextType, out string? type);

            var declaration = GetIdentifierDeclaration(parseInfo, ruleState, startOfMarkings);

            if (textBetweenMarkers.Trim() != "")
            {
                var identifier = ruleState.Identifiers.FirstOrDefault(i => i.Name == textBetweenMarkers);

                if (identifier == null)
                {
                    ruleState.Identifiers.Add(new Identifier()
                    {
                        Name = textBetweenMarkers,
                        References = new List<Reference>() {
                            new Reference(parseInfo.Uri, new Range(startOfMarkings, parseInfo.Position.Clone()))
                        },
                        Type = Either.If(type != null, type!, Identifier.IdentifierType.Any),
                        Declaration = declaration
                    });
                }
                else
                {
                    identifier.References.Add(
                        new Reference(parseInfo.Uri, new Range(startOfMarkings, parseInfo.Position.Clone())));

                    if (declaration != null)
                        identifier.Declaration = declaration;
                }
            }

            var nextRuleState = ruleState.Clone();

            if (type != null)
                nextRuleState = nextRuleState.WithoutValue(RuleStateValueStoreKey.NextType);

            if (declaration != null)
                nextRuleState = nextRuleState.WithoutValue(RuleStateValueStoreKey.IsDeclaration);

            return nextRuleState;
        }

        private static DeclarationReference? GetIdentifierDeclaration(ParserInformation parseInfo, RuleState ruleState, Position startOfMarkings)
        {
            if (ruleState.ValueStore.ContainsKey(RuleStateValueStoreKey.IsDeclaration))
                return new DeclarationReference(parseInfo.Uri, new Range(startOfMarkings, parseInfo.Position.Clone()), null);

            return null;
        }

        private static IConcreteRuleStateBuilder ParseIdentifierTypeAction(ParserInformation parseInfo, RuleState ruleState, IAction action, Position startOfMarkings)
        {
            return ruleState.Clone().WithValue(RuleStateValueStoreKey.NextType, parseInfo.GetTextUpToPosition(startOfMarkings));
        }

        private static IConcreteRuleStateBuilder ParseDeclaration(RuleState ruleState)
        {
            return ruleState.Clone().WithValue(RuleStateValueStoreKey.IsDeclaration);
        }
    }
}
