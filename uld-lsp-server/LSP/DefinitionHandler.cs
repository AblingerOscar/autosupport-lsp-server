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
    public class DefinitionHandler : IDefinitionHandler
    {
        private IDocumentStore documentStore;
        private DefinitionCapability capability;

        public DefinitionHandler(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;
            this.capability = new DefinitionCapability()
            {
                DynamicRegistration = false,
                LinkSupport = false
            };
        }

        public DefinitionRegistrationOptions GetRegistrationOptions()
        {
            return new DefinitionRegistrationOptions()
            {
                DocumentSelector = LSPUtils.GetDocumentSelector(documentStore.LanguageDefinition),
                WorkDoneProgress = false
            };
        }

        public Task<LocationOrLocationLinks> Handle(DefinitionParams request, CancellationToken cancellationToken)
        {
            var task = new Task<LocationOrLocationLinks>(GetDefinitionLocations(request), cancellationToken);
            task.Start();
            return task;
        }

        private Func<LocationOrLocationLinks> GetDefinitionLocations(DefinitionParams request)
        {
            return () =>
            {
                var uri = request.TextDocument.Uri.ToString();

                var selectedIdentifiers = documentStore.Documents[uri].GetIdentifiersAtPosition(request.Position);

                return LSPUtils.GetCrossDocumentsMergedIdentifiersOf(documentStore.Documents.Values, selectedIdentifiers)
                    .Select(iden =>
                        iden.Definition == null
                            ? null
                            : LSPUtils.TransformToLocationOrLocationLink(
                                iden.References.First(reference => request.Position.IsIn(reference.Range)),
                                iden.Definition,
                                capability.LinkSupport))
                    .WhereNotNull()
                    .ToList();
            };
        }

        public void SetCapability(DefinitionCapability capability)
        {
            this.capability = capability;
        }
    }
}
