using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace autosupport_lsp_server.Parsing.Impl
{
    internal class ParseResult : IParseResult
    {
        public ParseResult(bool finished, CompletionItem[] possibleContinuations, IError[] errors)
        {
            Finished = finished;
            PossibleContinuations = possibleContinuations;
            Errors = errors;
        }

        public bool Finished { get; set; }

        public CompletionItem[] PossibleContinuations { get; set; }

        public IError[] Errors { get; set; }
    }
}
