using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;

namespace autosupport_lsp_server.LSP
{
    internal class LSPUtils
    {
        private static DocumentSelector? documentSelector;

        public static DocumentSelector DocumentSelector {
            get {
                if (DocumentStore.LanguageDefinition == null)
                    throw new InvalidOperationException("Server not yet properly set up");

                if (documentSelector == null)
                {
                    documentSelector = new DocumentSelector(
                                DocumentFilter.ForPattern(DocumentStore.LanguageDefinition.LanguageFilePattern),
                                DocumentFilter.ForLanguage(DocumentStore.LanguageDefinition.LanguageId)
                                );
                }

                return documentSelector;
            }
        }

    }
}
