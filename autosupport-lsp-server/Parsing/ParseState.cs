using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace autosupport_lsp_server.Parsing
{
    internal class ParseState
    {
        internal ParseState(string[] text, Position position, IList<RuleState> ruleStates)
        {
            Text = text;
            Position = position;
            RuleStates = ruleStates;

            currentCharacterCount = 0;
            scheduledRuleStates = new Dictionary<long, List<RuleState>>();

            IsAtEndOfDocument = Text.Length == 0
                || PositionIsAfterEndOfDocument();
        }

        private long currentCharacterCount;
        private readonly IDictionary<long, List<RuleState>> scheduledRuleStates;



        internal string[] Text { get; }
        internal Position Position { get; }
        internal IList<RuleState> RuleStates { get; private set; }
        internal bool Failed { get; private set; } = false;

        internal bool IsAtEndOfDocument { get; private set; }

        internal string GetNextTextFromPosition(int minimumNumberOfCharacters)
        {
            return Text
                .Skip((int)Position.Line)
                .AggregateWhile(
                    new StringBuilder(),
                    (s1, s2) => s1.Append(Constants.NewLine).Append(s2),
                    (aggr, newStr) => aggr.Length - (int)Position.Character < minimumNumberOfCharacters)
                // adding +1 in order to skip the newline added at the start
                // by the aggregate
                .Remove(0, (int)Position.Character + 1)
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

            if (IsAtEndOfDocument)
            {
                RuleStates = scheduledRuleStates[nextRulesCharCount];
                scheduledRuleStates.Remove(nextRulesCharCount);
            }
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

            while (Position.Character >= Text[(int)Position.Line].Length)
            {
                if (PositionIsAfterEndOfDocument())
                {
                    IsAtEndOfDocument = true;
                    return;
                }

                // +1 for the line termination character
                Position.Character -= Text[(int)Position.Line].Length + 1;
                Position.Line++;
            }

            IsAtEndOfDocument = PositionIsAtEndOfDocument();
        }

        private bool PositionIsAfterEndOfDocument() =>
            (Position.Line >= Text.Length
                || (Position.Line == Text.Length - 1
                    && Position.Character >= Text[^1].Length));

        private bool PositionIsAtEndOfDocument() =>
            (Position.Line >= Text.Length
                || (Position.Line == Text.Length - 1
                    && Position.Character >= Text[^1].Length - 1));
    }
}
