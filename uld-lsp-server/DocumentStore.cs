using uld.server.Parsing.Impl;
using System.Collections.Concurrent;
using System.Collections.Generic;
using uld.definition;

namespace uld.server
{
    internal class DocumentStore : IDocumentStore
    {
        public DocumentStore(ILanguageDefinition languageDefinition)
        {
            Documents = new ConcurrentDictionary<string, Document>();
            LanguageDefinition = languageDefinition;
        }

        public IDictionary<string, Document> Documents { get; }

        public ILanguageDefinition LanguageDefinition { get; }

        public IParser CreateDefaultParser() => new Parser(LanguageDefinition);
    }
}
