using uld.definition;

namespace uld.server.Parsing.Impl
{
    public class CommentParser
    {
        private readonly CommentRules commentRules;

        public CommentParser(CommentRules commentRules)
            => this.commentRules = commentRules;

        /// <summary>
        /// Analyses whether the text starts with an comment and finds the matching end
        /// </summary>
        /// <param name="text">Text to analyse</param>
        /// <returns>The CommentParseResult if it's a comment; otherwise null
        /// </returns>
        internal CommentParseResult? GetNextComment(string text)
        {
            var result = GetCommentFromRules(text, commentRules.DocumentationComments);

            if (result.HasValue)
                return result;
            
            result = GetCommentFromRules(text, commentRules.NormalComments);

            if (result.HasValue)
                return new CommentParseResult(result.Value.CommentLength, null, result.Value.Replacement);

            return null;
        }

        private CommentParseResult? GetCommentFromRules(string text, CommentRule[] rules)
        {
            foreach (var rule in rules)
            {
                if (text.StartsWith(rule.Start))
                {
                    var textAfterStart = text.Substring(rule.Start.Length);
                    var idx = textAfterStart.IndexOf(rule.End);

                    if (idx == -1)
                        return new CommentParseResult(-1, textAfterStart.Substring(0, idx), rule.TreatAs);
                    else
                        return new CommentParseResult(idx + rule.Start.Length + rule.End.Length, textAfterStart.Substring(0, idx), rule.TreatAs);
                }
            }
            return null;
        }

        internal readonly struct CommentParseResult
        {
            public readonly int CommentLength;
            public readonly string? Documentation;
            public readonly string Replacement;

            public CommentParseResult(int commentLength, string? documentation, string replacement)
            {
                CommentLength = commentLength;
                Documentation = documentation;
                Replacement = replacement;
            }
        }
    }
}
