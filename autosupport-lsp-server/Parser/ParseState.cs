using autosupport_lsp_server.Symbols;
using Microsoft.Extensions.Primitives;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace autosupport_lsp_server.Parser
{
    internal class ParseState
    {
        private ParseState(Document document, Position position, RuleState ruleState, bool failed)
        {
            Document = document;
            Position = position;
            RuleState = ruleState;
            Failed = failed;
        }

        internal Document Document { get; }
        internal Position Position { get; }
        internal RuleState RuleState { get; }

        internal bool Failed { get; }

        internal string GetNextTextFromPosition()
        {
            return Document.Text
                .Skip((int)Position.Line)
                .Aggregate(new StringBuilder(), (s1, s2) => s1.Append('\n').Append(s2))
                .Remove(0, (int)Position.Character)
                .ToString();
        }

        internal ParseStateBuilder Clone() => new ParseStateBuilder(this);
        internal static ParseStateBuilder FromDocumentAndRuleState(Document document, RuleState ruleState) => new ParseStateBuilder(document, ruleState);

        internal class ParseStateBuilder
        {
            private ParseState? state;
            private Position? position;
            private Document? document;
            private RuleState? ruleState;

            private bool? failed;
            private int? positionOffset;

            public ParseStateBuilder(ParseState state)
            {
                this.state = state;
            }

            public ParseStateBuilder(Document document, RuleState ruleState)
            {
                this.document = document;
                this.ruleState = ruleState;
            }
            
            public ParseStateBuilder WithPositionOffsetBy(int numberOfCharacters)
            {
                positionOffset = numberOfCharacters;
                return this;
            }

            public ParseStateBuilder WithNewPosition(Position position)
            {
                this.position = position;
                return this;
            }

            public ParseStateBuilder WithFailed(bool failed)
            {
                this.failed = failed;
                return this;
            }

            public ParseState Build()
            {
                var document = this.document ?? state?.Document ?? throw new ArgumentException("Document cannot be null");
                var position = this.position ?? state?.Position ?? new Position(0, 0);

                if (positionOffset.HasValue)
                {
                    position.Character += positionOffset.Value;

                    while (position.Character > document.Text[(int)position.Line].Length)
                    {
                        // +1 for the line termination character
                        position.Character -= document.Text[(int)position.Line].Length + 1;
                        position.Line++;
                    }
                }

                return new ParseState(
                    document,
                    position,
                    ruleState: ruleState ?? state?.RuleState ?? throw new ArgumentException("RuleState cannot be null"),
                    failed: failed ?? state?.Failed ?? false
                    );
            }
        }
    }
}
