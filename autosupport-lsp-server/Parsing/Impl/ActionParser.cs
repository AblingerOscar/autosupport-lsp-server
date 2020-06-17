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
        private delegate IConcreteRuleStateBuilder SpecificPostActionParser(ParseState parseState, RuleState ruleState, IAction action, Position startOfMarkings);

        internal static IConcreteRuleStateBuilder ParseAction(ParseState parseState, RuleState ruleState, IAction action)
        {
            switch(action.GetBaseCommand())
            {
                case IAction.IDENTIFIER:
                    return ParsePostAction(parseState, ruleState, action, ParseIdentifierAction);
                case IAction.IDENTIFIER_TYPE:
                    if (action.GetArguments()[0] == IAction.IDENTIFIER_TYPE_ARG_SET)
                        // do immediate action
                        return ruleState.Clone().WithValue(RuleStateValueStoreKey.NextType, action.GetArguments()[1]);

                    return ParsePostAction(parseState, ruleState, action, ParseIdentifierTypeAction);
                case IAction.DECLARATION:
                    return ParseDeclaration(ruleState);
            }

            throw new ArgumentException("Given action is not supported: " + action.ToString());
        }

        private static IConcreteRuleStateBuilder ParsePostAction(
            ParseState parseState,
            RuleState ruleState,
            IAction action,
            SpecificPostActionParser specificActionParser)
        {
            if (ruleState.Markers.TryGetValue(action.GetBaseCommand(), out var pos))
            {
                var newRuleState = specificActionParser.Invoke(parseState, ruleState, action, pos);
                return newRuleState.WithoutMarker(action.GetBaseCommand());
            }
            else
            {
                return ruleState.Clone().WithMarker(action.GetBaseCommand(), parseState.Position);
            }
        }

        private static IConcreteRuleStateBuilder ParseIdentifierAction(ParseState parseState, RuleState ruleState, IAction action, Position startOfMarkings)
        {
            var textBetweenMarkers = parseState.GetTextBetweenPositions(startOfMarkings);

            ruleState.ValueStore.TryGetValue(RuleStateValueStoreKey.NextType, out string? type);

            var declaration = GetIdentifierDeclaration(parseState, ruleState, startOfMarkings);

            if (textBetweenMarkers.Trim() != "")
            {
                var identifier = ruleState.Identifiers.FirstOrDefault(i => i.Name == textBetweenMarkers);

                if (identifier == null)
                {
                    ruleState.Identifiers.Add(new Identifier()
                    {
                        Name = textBetweenMarkers,
                        References = new List<Reference>() {
                            new Reference(parseState.Uri, new Range(startOfMarkings, parseState.Position.Clone()))
                        },
                        Type = Either.If(type != null, type!, Identifier.IdentifierType.Any),
                        Declaration = declaration
                    });
                }
                else
                {
                    identifier.References.Add(
                        new Reference(parseState.Uri, new Range(startOfMarkings, parseState.Position.Clone())));

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

        private static DeclarationReference? GetIdentifierDeclaration(ParseState parseState, RuleState ruleState, Position startOfMarkings)
        {
            if (ruleState.ValueStore.ContainsKey(RuleStateValueStoreKey.IsDeclaration))
                return new DeclarationReference(parseState.Uri, new Range(startOfMarkings, parseState.Position), null);

            return null;
        }

        private static IConcreteRuleStateBuilder ParseIdentifierTypeAction(ParseState parseState, RuleState ruleState, IAction action, Position startOfMarkings)
        {
            return ruleState.Clone().WithValue(RuleStateValueStoreKey.NextType, parseState.GetTextBetweenPositions(startOfMarkings));
        }

        private static IConcreteRuleStateBuilder ParseDeclaration(RuleState ruleState)
        {
            return ruleState.Clone().WithValue(RuleStateValueStoreKey.IsDeclaration);
        }
    }
}
