using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace autosupport_lsp_server.LSP
{
    internal class TextDocumentSyncHandler : ITextDocumentSyncHandler
    {
        private readonly TextDocumentSyncKind syncKind = TextDocumentSyncKind.Incremental;
        private SynchronizationCapability? capability;
        private IDocumentStore documentStore;

        public TextDocumentSyncHandler(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;
        }

        public TextDocumentChangeRegistrationOptions GetRegistrationOptions()
        {
            return new TextDocumentChangeRegistrationOptions()
            {
                DocumentSelector = LSPUtils.GetDocumentSelector(documentStore.LanguageDefinition),
                SyncKind = syncKind
            };
        }

        public TextDocumentAttributes GetTextDocumentAttributes(Uri uri)
        {
            return new TextDocumentAttributes(uri, documentStore.LanguageDefinition.LanguageId);
        }

        public async Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var uri = request.TextDocument.Uri;
            var uriStr = uri.ToString();
            bool didCreateNewDocument = false;

            if (!documentStore.Documents.ContainsKey(uriStr))
            {
                documentStore.Documents[uriStr] = Document.CreateEmptyDocument(uri, documentStore.CreateDefaultParser());
                didCreateNewDocument = true;
            }

            foreach (var change in request.ContentChanges)
            {
                documentStore.Documents[uriStr].ApplyChange(change);

                if (cancellationToken.IsCancellationRequested && didCreateNewDocument)
                {
                    documentStore.Documents.Remove(uriStr);
                }
                cancellationToken.ThrowIfCancellationRequested();
            }

            return Unit.Value;
        }

        public async Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            var documentUri = request.TextDocument.Uri;
            documentStore.Documents.Add(
                documentUri.ToString(),
                Document.FromText(documentUri, request.TextDocument.Text, documentStore.CreateDefaultParser()));

            return Unit.Value;
        }

        public Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }

        public Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }

        public void SetCapability(SynchronizationCapability capability)
        {
            this.capability = capability;
        }

        TextDocumentRegistrationOptions IRegistration<TextDocumentRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentRegistrationOptions()
            {
                DocumentSelector = LSPUtils.GetDocumentSelector(documentStore.LanguageDefinition)
            };
        }

        TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentSaveRegistrationOptions()
            {
                DocumentSelector = LSPUtils.GetDocumentSelector(documentStore.LanguageDefinition),
                // Whether the client is supposed to send the text on a save
                IncludeText = false
            };
        }
    }
}
