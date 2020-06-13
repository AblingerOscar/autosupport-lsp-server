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

            if (textBetweenMarkers.Trim() != "")
            {
                var identifier = ruleState.Identifiers.FirstOrDefault(i => i.Name == textBetweenMarkers);

                if (identifier == null)
                {
                    ruleState.Identifiers.Add(new Identifier()
                    {
                        Name = textBetweenMarkers,
                        References = new List<Position>() { startOfMarkings }
                    });
                }
                else
                {
                    identifier.References.Add(startOfMarkings);
                }
            }

            return ruleState.Clone();
        }
    }
}
