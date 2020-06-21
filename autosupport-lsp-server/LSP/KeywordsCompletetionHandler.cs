using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace autosupport_lsp_server.LSP
{
    /// <summary>
    /// A CompletionHandler that only ever returns all keywords
    /// </summary>
    public class KeywordsCompletetionHandler : ICompletionHandler
    {
        private CompletionCapability? completionCapability = null;

        private CompletionList? keywordsCompletionList = null;
        private readonly IDocumentStore documentStore;

        public KeywordsCompletetionHandler(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;
        }

        CompletionList KeywordsCompletionList {
            get {
                if (keywordsCompletionList == null)
                {
                    keywordsCompletionList = new CompletionList(
                            LSPUtils.GetAllKeywordsAsCompletionItems(documentStore.LanguageDefinition)
                        );
                }

                return keywordsCompletionList!;
            }
        }


        public CompletionRegistrationOptions GetRegistrationOptions()
        {
            return new CompletionRegistrationOptions()
            {
                DocumentSelector = LSPUtils.GetDocumentSelector(documentStore.LanguageDefinition),
                ResolveProvider = false
            };
        }

        public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            return new CompletionList(KeywordsCompletionList.Select(item =>
            {
                item.TextEdit = new TextEdit()
                {
                    NewText = item.Label,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                            start: request.Position,
                            end: request.Position
                        )
                };
                return item;
            }));
        }

        public void SetCapability(CompletionCapability capability)
        {
            completionCapability = capability;
        }
    }
}
