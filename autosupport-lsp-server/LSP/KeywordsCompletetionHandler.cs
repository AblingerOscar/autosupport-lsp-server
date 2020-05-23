using autosupport_lsp_server.Symbols;
using autosupport_lsp_server.Symbols.Impl.Terminals;
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
    public class KeywordsCompletetionHandler : ICompletionHandler
    {
        CompletionCapability? completionCapability = null;
        CompletionItem[]? tokens = null;

        CompletionList? keywordsCompletionList = null;

        CompletionList KeywordsCompletionList {
            get {
                if (keywordsCompletionList == null)
                {
                    if (DocumentStore.LanguageDefinition == null)
                        throw new InvalidOperationException("Server not yet properly set up");

                    IEnumerable<CompletionItem> keywords = DocumentStore.LanguageDefinition.Rules
                        .SelectMany(rule =>
                            rule.Value.Symbols)
                        .Select(symbol =>
                            symbol.Match(
                                terminal: terminal =>
                                    terminal is StringTerminal stringTerminal
                                        ? stringTerminal.String
                                        : null,
                                nonTerminal: (_) => null,
                                oneOf: (_) => null,
                                action: (_) => null))
                        .Where(str => str != null)
                        .Select(str => {
                            return new CompletionItem()
                            {
                                Label = str
                            };
                        });

                    keywordsCompletionList = new CompletionList(keywords);
                }

                return keywordsCompletionList!;
            }
        }


        public CompletionRegistrationOptions GetRegistrationOptions()
        {
            return new CompletionRegistrationOptions()
            {
                DocumentSelector = LSPUtils.DocumentSelector,
                ResolveProvider = false,
                WorkDoneProgress = false
            };
        }

        public Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken) =>
            Task.FromResult(KeywordsCompletionList);

        public void SetCapability(CompletionCapability capability)
        {
            completionCapability = capability;
        }
    }
}
