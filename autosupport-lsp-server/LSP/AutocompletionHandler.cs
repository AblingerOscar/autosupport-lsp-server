using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace autosupport_lsp_server.LSP
{
    public class AutocompletionHandler : ICompletionHandler
    {

        private CompletionRegistrationOptions registrationOptions;

        public AutocompletionHandler(CompletionRegistrationOptions registrationOptions)
        {
            this.registrationOptions = registrationOptions;
        }

        public CompletionCapability? Capability { get; private set; }

        public CompletionRegistrationOptions GetRegistrationOptions() => registrationOptions;

        public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void SetCapability(CompletionCapability capability)
        {
            Capability = capability;
        }
    }
}
