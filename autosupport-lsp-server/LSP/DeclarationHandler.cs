using autosupport_lsp_server.Parsing;
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
    public class DeclarationHandler : IDeclarationHandler
    {
        private IDocumentStore documentStore;
        private DeclarationCapability capability;

        public DeclarationHandler(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;
            capability = new DeclarationCapability();
        }

        public DeclarationRegistrationOptions GetRegistrationOptions()
        {
            return new DeclarationRegistrationOptions()
            {
                DocumentSelector = LSPUtils.GetDocumentSelector(documentStore.LanguageDefinition),
                WorkDoneProgress = false
            };
        }

        public Task<LocationOrLocationLinks> Handle(DeclarationParams request, CancellationToken cancellationToken)
        {
            var task = new Task<LocationOrLocationLinks>(GetDeclarationLocations(request), cancellationToken);

            task.Start();
            return task;
        }

        private Func<LocationOrLocationLinks> GetDeclarationLocations(DeclarationParams request)
        {
            return () =>
            {
                var uri = request.TextDocument.Uri.ToString();

                var selectedIdentifiers = documentStore.Documents[uri].GetIdentifiersAtPosition(request.Position);

                return MergeWithSameIdentifiersOfOtherDocuments(selectedIdentifiers)
                    .Select(iden => TransformToLocationOrLocationLink(request.Position, iden))
                    .WhereNotNull()
                    .ToList();
            };
        }

        private IEnumerable<Identifier> MergeWithSameIdentifiersOfOtherDocuments(Identifier[] selectedIdentifiers)
        {
            var identifierComparer = new Identifier.IdentifierComparer();
            return Identifier.MergeIdentifiers(
                documentStore.Documents
                    .Select(doc => doc.Value.ParseResult?.Identifiers)
                    .WhereNotNull()
                    .ToArray())
                .Where(identifier => selectedIdentifiers.Any(selIden => identifierComparer.Equals(selIden, identifier)));
        }

        private LocationOrLocationLink? TransformToLocationOrLocationLink(Position requestPosition, Identifier identifier)
        {
            if (identifier.Declaration == null)
                return null;

            if (capability.LinkSupport)
            {
                var originalReference = identifier.References.First(reference => requestPosition.IsIn(reference.Range));

                return new LocationLink()
                {
                    OriginSelectionRange = originalReference.Range,
                    TargetRange = identifier.Declaration.EnclosingRange,
                    TargetSelectionRange = identifier.Declaration.Range,
                    TargetUri = identifier.Declaration.Uri

                };
            }

            return new Location()
            {
                Range = identifier.Declaration.Range,
                Uri = identifier.Declaration.Uri
            };
        }

        public void SetCapability(DeclarationCapability capability)
        {
            this.capability = capability;
        }
    }
}
