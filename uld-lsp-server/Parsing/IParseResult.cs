using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace uld.server.Parsing
{
    public interface IParseResult
    {
        bool Finished { get; }
        CompletionItem[] PossibleContinuations { get; }
        Error[] Errors { get; }
        Identifier[] Identifiers { get; }
        Range[] FoldingRanges { get; }
        Range[] Comments { get; }
    }
}
