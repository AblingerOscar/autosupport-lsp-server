using autosupport_lsp_server.LSP;
using autosupport_lsp_server.Parsing.Impl;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace autosupport_lsp_server.Parsing
{
    internal class ParseState
    {
        internal ParseState(Uri uri, string[] text, Position position, IList<RuleState> ruleStates, CommentParser commentParser)
        {
            Uri = uri;
            Text = text;
            PreCommentPosition = position;
            Position = position;
            RuleStates = ruleStates;
            this.commentParser = commentParser;

            currentCharacterCount = 0;
            scheduledRuleStates = new Dictionary<long, List<RuleState>>();
            prependText = "";
            appliedComments = new List<(Range Range, string Replacement)>();

            IsAtEndOfDocument = Text.Length == 0
                || PositionIsAfterEndOfDocument();
        }

        private long currentCharacterCount;
        private readonly CommentParser commentParser;
        private readonly IDictionary<long, List<RuleState>> scheduledRuleStates;
        /// <summary>
        /// string that is not in the actual text, but to the outside should be
        /// the next text at the Position, before the actual text continues
        /// Currently only used with comments that can then be treated as other
        /// strings
        /// 
        /// e.g.
        /// Text:        "Hello world"
        /// prependText: "foo"
        /// should be treated as if the Text was:
        ///              "fooHello world"
        /// 
        /// Note that the position should not change when traversing through this
        /// text.
        /// aka moving 4 characters in the previous example would make the
        /// Position (0, 1), but should remove the prependText so that the Text
        /// is treated as "ello world" again
        /// </summary>
        private string prependText;
        private IList<(Range Range, string Replacement)> appliedComments;

        internal Uri Uri { get; }

        internal ImmutableArray<(Range Range, string Replacement)> AppliedComments => appliedComments.ToImmutableArray();
        internal string[] Text { get; }
        internal Position PreCommentPosition { get; private set; }
        internal Position Position { get; }
        internal IList<RuleState> RuleStates { get; private set; }
        internal bool Failed { get; private set; } = false;

        internal bool IsAtEndOfDocument { get; private set; }
        internal bool HasFinishedParsing => RuleStates.Count == 0;

        internal string GetNextTextFromPosition(int minimumNumberOfCharacters)
        {
            if (Position.Line >= Text.Length || Position.Character < 0 || Position.Character > Text[Position.Line].Length)
                throw new ArgumentOutOfRangeException($"Position ({Position.Line}, {Position.Character}) not in text");

            StringBuilder text = new StringBuilder(prependText);

            if (Position.Character == Text[Position.Line].Length) // Position after line
                text.Append(Constants.NewLine);
            else
                text.Append(Text[Position.Line].Substring((int)Position.Character));

            var currLine = Position.Line + 1;
            while (text.Length < minimumNumberOfCharacters && currLine < Text.Length)
            {
                text.Append(Text[currLine]);
                ++currLine;
            }

            return text.ToString();
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

            var textWithComments = Text
                .Skip((int)start.Line)
                .Take((int)(end.Line - start.Line) + 1)
                .ToArray();

            textWithComments[^1] = textWithComments[^1].Substring(0, (int)end.Character);
            textWithComments[0] = textWithComments[0].Substring((int)start.Character);

            return RemoveComments(new Range(start, end), textWithComments);
        }

        private string RemoveComments(Range textRange, string[] textWithComments)
        {
            var relevantComments = AppliedComments.Where(tuple => HasOverlap(tuple.Range, textRange))
                .ToList();

            // sort from later to earlier (ranges should never be overlapping)
            relevantComments.Sort((t1, t2) => t2.Range.Start.CompareTo(t1.Range.Start));

            var textList = textWithComments.ToList();

            foreach (var comment in relevantComments)
            {
                var relativeStartPos = comment.Range.Start.Minus(textRange.Start);
                if (relativeStartPos.Line < 0) relativeStartPos.Line = 0;
                if (relativeStartPos.Character < 0) relativeStartPos.Character = 0;
                
                var relativeEndPos = comment.Range.End.Minus(textRange.Start);

                LSPUtils.RemoveTextInRange(textList, relativeStartPos, relativeEndPos);
            }

            return textList.JoinToString(Constants.NewLine.ToString());
        }

        private bool HasOverlap(Range r1, Range r2)
        {
            // - r1 starts in r2 and ends either in it or after
            // - r1 starts before r2 and ends in r2
            // - r1 starts before r2 and ends after r2 (hence r1 is fully in r2)
            return r1.Start.IsIn(r2) || r1.End.IsIn(r2) || r2.Start.IsIn(r1);
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
            PreCommentPosition = Position.Clone();
            SkipCommentsIfTheyAreNext();
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
            if (numberOfCharacters <= 0)
                throw new ArgumentException(nameof(numberOfCharacters) + " must be above 0");

            if (prependText.Length > 0)
            {
                int prependTextLength = prependText.Length;
                prependText = prependText.Remove(0, Math.Max((int)numberOfCharacters, prependTextLength));
                numberOfCharacters -= prependTextLength;

                if (numberOfCharacters <= 0)
                {
                    IsAtEndOfDocument = PositionIsAfterEndOfDocument();
                    return;
                }
            }

            currentCharacterCount += numberOfCharacters;
            Position.Character += numberOfCharacters;

            // > instead of >= as position can be after the last character
            while (Position.Character > Text[(int)Position.Line].Length)
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

        private void SkipCommentsIfTheyAreNext()
        {
            CommentParser.CommentParseResult? comment = null;
            do
            {
                var text = Text.Skip((int)Position.Line).JoinToString(Constants.NewLine.ToString()).Substring((int)Position.Character);

                comment = commentParser.GetNextComment(text);

                if (comment.HasValue)
                {
                    if (comment.Value.Documentation != null)
                    {
                        RuleStates = RuleStates
                            .Select(rs =>
                                rs.Clone()
                                .WithValue(RuleStateValueStoreKey.NextDocumentation, comment.Value.Documentation)
                                .Build())
                            .ToList();
                    }

                    var startPosition = Position.Clone();

                    OffsetPositionBy(comment.Value.CommentLength);

                    prependText += comment.Value.Replacement;
                    appliedComments.Add((new Range(startPosition, Position.Clone()), comment.Value.Replacement));
                }
            } while (comment.HasValue);
        }

        private bool PositionIsAfterEndOfDocument() =>
            prependText.Length == 0
            && (Position.Line >= Text.Length
                || (Position.Line == Text.Length - 1
                    && Position.Character >= Text[^1].Length));

        public override string? ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Position {Position.ToNiceString()} with {RuleStates.Count} ruleStates. ");

            if (Text.Length > Position.Line && Text[Position.Line].Length == Position.Character)
                sb.AppendLine(Text[Position.Line] + "|");
            else if (Text.Length <= Position.Line || Text[Position.Line].Length <= Position.Character)
                sb.Append("(Position OOB)");
            else
                sb.AppendLine(Text[Position.Line].Insert((int)Position.Character, "|"));

            return sb.ToString();
        }
    }
}
