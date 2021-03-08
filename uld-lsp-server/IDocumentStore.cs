using uld.server.Parsing.Impl;
using System.Collections.Generic;
using uld.definition;

namespace uld.server
{
    public interface IDocumentStore
    {
        IDictionary<string, Document> Documents { get; }
        ILanguageDefinition LanguageDefinition { get; }
        IParser CreateDefaultParser();
    }
}