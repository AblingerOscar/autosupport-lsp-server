using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
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
        internal bool HasFinishedParsing => RuleStates.Count == 0;

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

        /// <summary>
        /// Gets the text between the two positions, with the ned being exclusive
        /// </summary>
        /// <param name="start">Start position, inclusively</param>
        /// <param name="end">End position, exclusively. If null, current position is used</param>
        /// <returns>The text between the two positions</returns>
        internal string GetTextBetweenPositions(Position start, Position? end = null)
        {
            if (end == null)
                end = Position;

            if (start == end)
                return "";

            var sb = Text
                .Skip((int)start.Line)
                .Take((int)(end.Line - start.Line) + 1)
                .Aggregate(new StringBuilder(), (sb, str) => sb.Append(str));

            int hangingCharNumber = Text[(int)end.Line].Length - (int)end.Character;

            return sb.Remove(sb.Length - hangingCharNumber, hangingCharNumber)
                .Remove(0, (int)start.Character)
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
            RuleStates = scheduledRuleStates[nextRulesCharCount];
            scheduledRuleStates.Remove(nextRulesCharCount);

            OffsetPositionBy(nextRulesCharCount - currentCharacterCount);
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

            IsAtEndOfDocument = PositionIsAfterEndOfDocument();
        }

        private bool PositionIsAfterEndOfDocument() =>
            (Position.Line >= Text.Length
                || (Position.Line == Text.Length - 1
                    && Position.Character >= Text[^1].Length));

        public override string? ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Position ({Position.Line}, {Position.Character}) with {RuleStates.Count} ruleStates. ");

            if (Text.Length <= Position.Line || Text[Position.Line].Length <= Position.Character)
            {
                sb.Append("(Position OOB)");
            }
            else
            {
                sb.AppendLine(Text[Position.Line].Insert((int)Position.Character, "|"));
                Enumerable.Repeat(" ", (int)Position.Character).ForEach(s => sb.Append(s));
                sb.Append("^");
            }

            return sb.ToString();
        }
    }
}
