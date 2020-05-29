using autosupport_lsp_server.Parsing.Impl;
using System.Collections.Generic;

namespace autosupport_lsp_server
{
    public interface IDocumentStore
    {
        IDictionary<string, Document> Documents { get; }
        IAutosupportLanguageDefinition LanguageDefinition { get; }
        IParser CreateDefaultParser();
    }
}