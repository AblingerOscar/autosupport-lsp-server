using autosupport_lsp_server.Symbols.Impl.Terminals;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.Linq;

namespace autosupport_lsp_server.LSP
{
    internal class LSPUtils
    {
        private static DocumentSelector? documentSelector;

        public static DocumentSelector GetDocumentSelector(IAutosupportLanguageDefinition languageDefinition)
        {
            if (documentSelector == null)
            {
                documentSelector = new DocumentSelector(
                            DocumentFilter.ForPattern(languageDefinition.LanguageFilePattern),
                            DocumentFilter.ForLanguage(languageDefinition.LanguageId)
                            );
            }

            return documentSelector;
        }

        public static IEnumerable<CompletionItem> GetAllKeywordsAsCompletionItems(IAutosupportLanguageDefinition languageDefinition)
        {
            return languageDefinition.Rules
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
                .Distinct()
                .Select(str =>
                {
                    return new CompletionItem()
                    {
                        Label = str,
                        Kind = CompletionItemKind.Keyword
                    };
                });
        }
    }
}
