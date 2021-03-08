using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace uld.server.LSP
{
    internal class TextDocumentSyncHandler : ITextDocumentSyncHandler
    {
        private readonly TextDocumentSyncKind syncKind = TextDocumentSyncKind.Full;
        private SynchronizationCapability? capability;
        private readonly IDocumentStore documentStore;
        private readonly ValidationHandler validationHandler;

        public TextDocumentSyncHandler(IDocumentStore documentStore, ValidationHandler validationHandler)
        {
            this.documentStore = documentStore;
            this.validationHandler = validationHandler;
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

        public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
        {
            var task = new Task<Unit>(() =>
            {
                ApplyChangeToDocument(request, cancellationToken);
                validationHandler.RunValidation(request.TextDocument.Uri, cancellationToken);
                return Unit.Value;
            },
            cancellationToken);

            task.Start();

            return task;
        }

        private Unit ApplyChangeToDocument(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
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

        public Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            var task = new Task<Unit>(() =>
                {
                    var documentUri = request.TextDocument.Uri;
                    if (documentStore.Documents.TryGetValue(documentUri.ToString(), out var document))
                        document.UpdateText(request.TextDocument.Text);
                    else
                        documentStore.Documents.Add(
                            documentUri.ToString(),
                            Document.FromText(documentUri, request.TextDocument.Text, documentStore.CreateDefaultParser()));

                    validationHandler.RunValidation(documentUri, cancellationToken);

                    return Unit.Value;
                },
                cancellationToken);

            task.Start();

            return task;
        }

        public Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }

        public Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {
            var task = new Task<Unit>(() =>
            {
                validationHandler.RunValidation(request.TextDocument.Uri, cancellationToken);

                return Unit.Value;
            }, cancellationToken);

            task.Start();
            return task;
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
