using uld.definition;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace uld.server.LSP
{
    internal class ImplementationHandler : IImplementationHandler
    {
        private readonly IDocumentStore documentStore;
        private bool linkSupport = false;

        public ImplementationHandler(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;
        }

        public ImplementationRegistrationOptions GetRegistrationOptions()
        {
            return new ImplementationRegistrationOptions()
            {
                DocumentSelector = LSPUtils.GetDocumentSelector(documentStore.LanguageDefinition),
                WorkDoneProgress = false
            };
        }

        public Task<LocationOrLocationLinks> Handle(ImplementationParams request, CancellationToken cancellationToken)
        {
            var task = new Task<LocationOrLocationLinks>(GetImplementationLocations(request), cancellationToken);
            task.Start();
            return task;
        }

        private Func<LocationOrLocationLinks> GetImplementationLocations(ImplementationParams request)
        {
            return () =>
            {
                var uri = request.TextDocument.Uri.ToString();

                var selectedIdentifiers = documentStore.Documents[uri].GetIdentifiersAtPosition(request.Position);

                return LSPUtils.GetCrossDocumentsMergedIdentifiersOf(documentStore.Documents.Values, selectedIdentifiers)
                    .Select(iden =>
                        iden.Implementation == null
                            ? null
                            : LSPUtils.TransformToLocationOrLocationLink(
                                iden.References.First(reference => request.Position.IsIn(reference.Range)),
                                iden.Implementation,
                                linkSupport))
                    .WhereNotNull()
                    .ToList();
            };
        }

        public void SetCapability(ImplementationCapability capability)
        {
            linkSupport = capability.LinkSupport;
        }
    }
}
