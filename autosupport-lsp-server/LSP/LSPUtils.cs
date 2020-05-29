using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;

namespace autosupport_lsp_server.LSP
{
    internal class LSPUtils
    {
        private static DocumentSelector? documentSelector;

        public static DocumentSelector GetDocumentSelector(IAutosupportLanguageDefinition languageDefinition) {
            if (documentSelector == null)
            {
                documentSelector = new DocumentSelector(
                            DocumentFilter.ForPattern(languageDefinition.LanguageFilePattern),
                            DocumentFilter.ForLanguage(languageDefinition.LanguageId)
                            );
            }

            return documentSelector;
        }

    }
}
