using autosupport_lsp_server.Shared;
using autosupport_lsp_server.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using static autosupport_lsp_server.Parsing.RuleState;

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

            if (textBetweenMarkers.Trim() != "")
            {
                var identifier = ruleState.Identifiers.FirstOrDefault(i => i.Name == textBetweenMarkers);

                if (identifier == null)
                {
                    ruleState.Identifiers.Add(new Identifier()
                    {
                        Name = textBetweenMarkers,
                        References = new List<Position>() { startOfMarkings },
                        Type = Either.If(type != null, type!, Identifier.IdentifierType.Any)
                    });
                }
                else
                {
                    identifier.References.Add(startOfMarkings);
                }
            }

            if (type != null)
                return ruleState.Clone().WithoutValue(RuleStateValueStoreKey.NextType);

            return ruleState.Clone();
        }

        private static IConcreteRuleStateBuilder ParseIdentifierTypeAction(ParseState parseState, RuleState ruleState, IAction action, Position startOfMarkings)
        {
            return ruleState.Clone().WithValue(RuleStateValueStoreKey.NextType, parseState.GetTextBetweenPositions(startOfMarkings));
        }
    }
}
