using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace autosupport_lsp_server
{
    internal class TextDocumentSyncHandler : ITextDocumentSyncHandler
    {
        private DocumentSelector? documentSelector;
        private readonly TextDocumentSyncKind syncKind = TextDocumentSyncKind.Full;
        private SynchronizationCapability? capability;

        public TextDocumentChangeRegistrationOptions GetRegistrationOptions()
        {
            if (DocumentStore.LanguageDefinition == null)
                throw new InvalidOperationException("Server not yet properly set up");

            return new TextDocumentChangeRegistrationOptions()
            {
                DocumentSelector = documentSelector,
                SyncKind = syncKind
            };
        }

        public TextDocumentAttributes GetTextDocumentAttributes(Uri uri)
        {
            if (DocumentStore.LanguageDefinition == null)
                throw new InvalidOperationException("Server not yet properly set up");

            if (documentSelector == null)
            {
                documentSelector = new DocumentSelector(
                            DocumentFilter.ForPattern("**/*.atg"),
                            DocumentFilter.ForLanguage("Cocol-2")
                            );
            }

            return new TextDocumentAttributes(uri, DocumentStore.LanguageDefinition.LanguageId);
        }

        public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
        {
            return new Task<Unit>(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var documentUri = request.TextDocument.Uri.ToString();
                bool didCreateNewDocument = false;
                if (!DocumentStore.Documents.ContainsKey(documentUri))
                {
                    DocumentStore.Documents[documentUri] = Document.CreateEmptyDocument(documentUri);
                    didCreateNewDocument = true;
                }

                foreach (var change in request.ContentChanges)
                {
                    DocumentStore.Documents[documentUri].ApplyChange(change);

                    if (cancellationToken.IsCancellationRequested && didCreateNewDocument)
                    {
                        DocumentStore.Documents.Remove(documentUri);
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }

                return Unit.Value;
            }, cancellationToken);
        }

        public Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            return new Task<Unit>(() =>
            {
                var documentUri = request.TextDocument.Uri.ToString();
                DocumentStore.Documents.Add(documentUri, Document.FromText(documentUri, request.TextDocument.Text));

                return Unit.Value;
            }, cancellationToken);
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
                DocumentSelector = documentSelector
            };
        }

        TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentSaveRegistrationOptions()
            {
                DocumentSelector = documentSelector,
                // Whether the client is supposed to send the text on a save
                IncludeText = false
            };
        }
    }
}
