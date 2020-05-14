using autosupport_lsp_server.Symbols;
using Microsoft.Extensions.Primitives;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace autosupport_lsp_server.Parsing
{
    internal class ParseState
    {
        internal ParseState(Document document, Position position, IList<RuleState> ruleStates)
        {
            Document = document;
            Position = position;
            RuleStates = ruleStates;

            currentCharacterCount = 0;
            scheduledRuleStates = new Dictionary<long, List<RuleState>>();

            IsAtEndOfDocument = Document.Text.Count == 0
                || PositionIsAfterEndOfDocument();
        }

        private long currentCharacterCount;
        private readonly IDictionary<long, List<RuleState>> scheduledRuleStates;

        internal Document Document { get; }
        internal Position Position { get; }
        internal IList<RuleState> RuleStates { get; private set; }
        internal bool Failed { get; private set; } = false;

        internal bool IsAtEndOfDocument { get; private set; }

        internal string GetNextTextFromPosition(int minimumNumberOfCharacters)
        {
            return Document.Text
                .Skip((int)Position.Line)
                .AggregateWhile(
                    new StringBuilder(),
                    (s1, s2) => s1.Append(Constants.NewLine).Append(s2),
                    (aggr, newStr) => aggr.Length - (int)Position.Character < minimumNumberOfCharacters)
                .Remove(0, (int)Position.Character)
                .ToString();
        }

        internal void NextStep()
        {
            if (scheduledRuleStates.Count == 0)
            {
                RuleStates.Clear();
                Failed = !IsAtEndOfDocument;
                return;
            }

            var nextRulesCharCount = scheduledRuleStates.Keys.Min();

            OffsetPositionBy(nextRulesCharCount - currentCharacterCount);

            RuleStates = scheduledRuleStates[nextRulesCharCount];
            scheduledRuleStates.Remove(nextRulesCharCount);
        }

        internal void ScheduleNewRuleStatesIn(int numberOfCharacters, IEnumerable<RuleState> ruleStates)
        {
            long scheduledCharacterCount = currentCharacterCount + numberOfCharacters;
            if (!scheduledRuleStates.ContainsKey(scheduledCharacterCount))
                scheduledRuleStates.Add(scheduledCharacterCount, new List<RuleState>());

            scheduledRuleStates[scheduledCharacterCount].AddRange(ruleStates);
        }

        private void OffsetPositionBy(long numberOfCharacters)
        {
            currentCharacterCount += numberOfCharacters;
            Position.Character += numberOfCharacters;

            while (Position.Character >= Document.Text[(int)Position.Line].Length)
            {
                if (PositionIsAfterEndOfDocument())
                {
                    IsAtEndOfDocument = true;
                    return;
                }

                // +1 for the line termination character
                Position.Character -= Document.Text[(int)Position.Line].Length + 1;
                Position.Line++;
            }
        }

        private bool PositionIsAfterEndOfDocument() =>
            (Position.Line >= Document.Text.Count
                || (Position.Line == Document.Text.Count - 1
                    && Position.Character >= Document.Text[Document.Text.Count - 1].Length));
    }
}
