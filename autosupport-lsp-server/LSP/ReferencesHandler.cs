using autosupport_lsp_server.Parsing;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace autosupport_lsp_server.LSP
{
    public class ReferencesHandler: IReferencesHandler
    {
        private IDocumentStore documentStore;

        public ReferencesHandler(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;
        }

        public ReferenceRegistrationOptions GetRegistrationOptions()
        {
            return new ReferenceRegistrationOptions()
            {
                DocumentSelector = LSPUtils.GetDocumentSelector(documentStore.LanguageDefinition),
                WorkDoneProgress = false
            };
        }

        public Task<LocationContainer> Handle(ReferenceParams request, CancellationToken cancellationToken)
        {
            var task = new Task<LocationContainer>(() =>
            {
                var uri = request.TextDocument.Uri.ToString();
                var identifiers = documentStore.Documents[uri].GetIdentifiersAtPosition(request.Position);

                var references = documentStore.Documents
                    .SelectMany(doc => doc.Value.ParseResult?.Identifiers.Select(iden => (Uri: doc.Key, Identifier: iden)))
                    .Distinct(kvp => (kvp.Uri, kvp.Identifier.Name, kvp.Identifier.Type))
                    .Where(kvp => identifiers.Any(identifier => identifier.Name == kvp.Identifier.Name && identifier.Type == kvp.Identifier.Type))
                    .SelectMany(kvp => kvp.Identifier.References.Select(reference => (kvp.Uri, Reference: reference, kvp.Identifier.Name)));

                return new LocationContainer(
                    references
                    .Select(reference => new Location()
                    {
                        Uri = new Uri(reference.Uri),
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                            reference.Reference,
                            new Position(reference.Reference.Line, reference.Reference.Character + reference.Name.Length)
                            )
                    }));
            },
            cancellationToken);

            task.Start();

            return task;
        }

        public void SetCapability(ReferenceCapability capability)
        {
            // do nothing
        }
    }
}
