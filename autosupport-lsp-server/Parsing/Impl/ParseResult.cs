using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace autosupport_lsp_server.Parsing.Impl
{
    internal class ParseResult : IParseResult
    {
        public ParseResult(bool finished, CompletionItem[] possibleContinuations, IError[] errors, Identifier[] identifiers)
        {
            Finished = finished;
            PossibleContinuations = possibleContinuations;
            Errors = errors;
            Identifiers = identifiers;
        }

        public bool Finished { get; set; }

        public CompletionItem[] PossibleContinuations { get; set; }

        public IError[] Errors { get; set; }

        public Identifier[] Identifiers { get; set; }
    }
}
