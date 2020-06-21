using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace autosupport_lsp_server.Parsing.Impl
{
    internal class ParseResult : IParseResult
    {
        public ParseResult(bool finished, CompletionItem[] possibleContinuations, Error[] errors, Identifier[] identifiers, Range[] foldingRanges)
        {
            Finished = finished;
            PossibleContinuations = possibleContinuations;
            Errors = errors;
            Identifiers = identifiers;
            FoldingRanges = foldingRanges;
        }

        public bool Finished { get; }

        public CompletionItem[] PossibleContinuations { get; }

        public Error[] Errors { get; }

        public Identifier[] Identifiers { get; }

        public Range[] FoldingRanges { get; }
    }
}
