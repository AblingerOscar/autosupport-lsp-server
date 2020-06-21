using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace autosupport_lsp_server.Parsing
{
    public interface IParseResult
    {
        bool Finished { get; }
        CompletionItem[] PossibleContinuations { get; }
        Error[] Errors { get; }
        Identifier[] Identifiers { get; }
        Range[] FoldingRanges { get; }
    }
}
