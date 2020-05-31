using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System;
using System.Collections.Generic;
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

            if (!documentStore.Documents.ContainsKey(uri) || documentStore.Documents[uri].ParseResult == null)
            {
                // should never happen as the document is latest created at first opening
                return KeywordsCompletionList;
            }

            var parseResult = documentStore.Documents[uri].ParseResult;

            return parseResult.PossibleContinuations
                .Select(str => new CompletionItem()
                {
                    Label = str
                })
                .Union(KeywordsCompletionList)
                .ToList();
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
    }
}
