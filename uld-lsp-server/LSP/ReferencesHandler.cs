using uld.server.Parsing;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace uld.server.LSP
{
    public class ReferencesHandler : IReferencesHandler
    {
        private readonly IDocumentStore documentStore;

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
                var selectedIdentifiers = documentStore.Documents[uri].GetIdentifiersAtPosition(request.Position);

                var identifierComparer = new Identifier.IdentifierComparer();
                var allIdentifiers =
                    Identifier.MergeIdentifiers(
                        documentStore.Documents
                            .SelectMany(doc => doc.Value.ParseResult?.Identifiers ?? Enumerable.Empty<Identifier>()));

                var references = allIdentifiers
                        .Where(identifier =>
                            selectedIdentifiers.Any(selectedIdent =>
                                identifierComparer.Equals(identifier, selectedIdent)))
                        .SelectMany(identifier => identifier.References);

                return new LocationContainer(
                    references
                    .Select(reference => new Location()
                    {
                        Uri = reference.Uri,
                        Range = reference.Range
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
