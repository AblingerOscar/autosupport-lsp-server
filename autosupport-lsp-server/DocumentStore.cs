using autosupport_lsp_server.Parsing.Impl;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;

namespace autosupport_lsp_server
{
    internal class DocumentStore : IDocumentStore
    {
        public DocumentStore(IAutosupportLanguageDefinition languageDefinition)
        {
            Documents = new ConcurrentDictionary<string, Document>();
            LanguageDefinition = languageDefinition;
        }

        public IDictionary<string, Document> Documents { get; }

        public IAutosupportLanguageDefinition LanguageDefinition { get; }

        public IParser CreateDefaultParser() => new Parser(LanguageDefinition);
    }
}
