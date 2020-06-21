using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace autosupport_lsp_server.LSP
{
    internal class FoldingHandler : IFoldingRangeHandler
    {
        private bool lineFoldingOnly = false;
        private readonly IDocumentStore documentStore;

        public FoldingHandler(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;
        }

        public FoldingRangeRegistrationOptions GetRegistrationOptions()
            => new FoldingRangeRegistrationOptions()
            {
                DocumentSelector = LSPUtils.GetDocumentSelector(documentStore.LanguageDefinition)
            };

        // TODO: Fold Comments and Import statements
        public Task<Container<FoldingRange>> Handle(FoldingRangeRequestParam request, CancellationToken cancellationToken)
        {
            var task = new Task<Container<FoldingRange>>(() =>
            {
                IEnumerable<Range>? ranges = documentStore.Documents[request.TextDocument.Uri.ToString()].ParseResult?.FoldingRanges;

                if (ranges == null)
                    return new FoldingRange[0];

                if (lineFoldingOnly)
                    ranges = ranges.Where(range => range.Start.Line != range.End.Line);

                return ranges
                    .Select(Range2FoldingRange)
                    .ToList();
            }, cancellationToken);

            task.Start();
            return task;
        }

        private static FoldingRange Range2FoldingRange(Range range)
            => new FoldingRange()
            {
                StartLine = range.Start.Line,
                StartCharacter = range.Start.Character,
                EndLine = range.End.Line,
                EndCharacter = range.End.Line,
                Kind = FoldingRangeKind.Region
            };

        public void SetCapability(FoldingRangeCapability capability)
        {
            lineFoldingOnly = capability.LineFoldingOnly;
        }
    }
}
