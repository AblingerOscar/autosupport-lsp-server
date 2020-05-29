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

        public TextDocumentChangeRegistrationOptions GetRegistrationOptions()
        {
            if (DocumentStore.LanguageDefinition == null)
                throw new InvalidOperationException("Server not yet properly set up");

            return new TextDocumentChangeRegistrationOptions()
            {
                DocumentSelector = LSPUtils.DocumentSelector,
                SyncKind = syncKind
            };
        }

        public TextDocumentAttributes GetTextDocumentAttributes(Uri uri)
        {
            if (DocumentStore.LanguageDefinition == null)
                throw new InvalidOperationException("Server not yet properly set up");

            return new TextDocumentAttributes(uri, DocumentStore.LanguageDefinition.LanguageId);
        }

        public async Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
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
        }

        public async Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            Console.WriteLine("...............Opened a new document :O");
            var documentUri = request.TextDocument.Uri.ToString();
            DocumentStore.Documents.Add(documentUri, Document.FromText(documentUri, request.TextDocument.Text));

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
                DocumentSelector = LSPUtils.DocumentSelector
            };
        }

        TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentSaveRegistrationOptions()
            {
                DocumentSelector = LSPUtils.DocumentSelector,
                // Whether the client is supposed to send the text on a save
                IncludeText = false
            };
        }
    }
}
