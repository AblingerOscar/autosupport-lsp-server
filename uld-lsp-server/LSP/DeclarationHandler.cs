using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using uld.definition;

namespace uld.server.LSP
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

                return LSPUtils.GetCrossDocumentsMergedIdentifiersOf(documentStore.Documents.Values, selectedIdentifiers)
                    .Select(iden =>
                        iden.Declaration == null
                            ? null
                            : LSPUtils.TransformToLocationOrLocationLink(
                                iden.References.First(reference => request.Position.IsIn(reference.Range)),
                                iden.Declaration,
                                capability.LinkSupport))
                    .WhereNotNull()
                    .ToList();
            };
        }

        public void SetCapability(DeclarationCapability capability)
        {
            this.capability = capability;
        }
    }
}
