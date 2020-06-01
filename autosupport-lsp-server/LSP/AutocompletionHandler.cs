using autosupport_lsp_server.Parsing.Impl;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace autosupport_lsp_server.LSP
{
    public class AutocompletionHandler : ICompletionHandler
    {
        private readonly IDocumentStore documentStore;
        private List<CompletionItem>? keywordsCompletionList = null;

        public AutocompletionHandler(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;
        }

        public CompletionCapability? Capability { get; private set; }

        public CompletionRegistrationOptions GetRegistrationOptions() => new CompletionRegistrationOptions()
        {
            DocumentSelector = LSPUtils.GetDocumentSelector(documentStore.LanguageDefinition),
            WorkDoneProgress = false
        };

        public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            var uri = request.TextDocument.Uri.ToString();

            if (!documentStore.Documents.ContainsKey(uri))
                // should never happen as the document is latest created at first opening
                return KeywordsCompletionList;

            var parseResult = GetParseResult(request.Position, uri);

            return MergeParseResultWithKeywords(parseResult);
        }

        private CompletionList MergeParseResultWithKeywords(Parsing.IParseResult? parseResult)
        {
            if (parseResult == null)
                return KeywordsCompletionList;

            var completions = new List<CompletionItem>(parseResult.PossibleContinuations);
            var equalityComparator = new CompletionItemContentEqualityComparer();

            foreach (var kw in KeywordsCompletionList)
            {
                if (!parseResult.PossibleContinuations.Contains(kw, equalityComparator))
                    completions.Add(kw);
            }

            return completions;
        }

        private Parsing.IParseResult? GetParseResult(Position position, string uri)
        {
            var documentText = documentStore.Documents[uri].Text;

            if (position.Line == documentText.Count
                && (position.Line == 0 || position.Character == documentText[^1].Length))
                return documentStore.Documents[uri].ParseResult;

            var textUpToPosition = documentText.Take((int)position.Line).ToArray();

            if (textUpToPosition.Length > 0)
                textUpToPosition[^1] = textUpToPosition[^1].Substring(0, (int)position.Character);

            return new Parser(documentStore.LanguageDefinition).Parse(textUpToPosition);
        }

        public void SetCapability(CompletionCapability capability)
        {
            Capability = capability;
        }

        private List<CompletionItem> KeywordsCompletionList {
            get {
                if (keywordsCompletionList == null)
                {
                    keywordsCompletionList = new List<CompletionItem>(
                            LSPUtils.GetAllKeywordsAsCompletionItems(documentStore.LanguageDefinition)
                        );
                }

                return keywordsCompletionList;
            }
        }

        private class CompletionItemContentEqualityComparer : IEqualityComparer<CompletionItem>
        {
            public bool Equals([AllowNull] CompletionItem x, [AllowNull] CompletionItem y)
            {
                if (x == null)
                    return y == null;

                if (y == null)
                    return false;

                return x.Label == y.Label && x.Kind == y.Kind;
            }

            public int GetHashCode([DisallowNull] CompletionItem ci)
            {
                return $"{ci.Kind}{ci.Label.GetHashCode()}".GetHashCode();
            }
        }
    }
}
