using System.Collections.Concurrent;
using System.Collections.Generic;

namespace autosupport_lsp_server
{
    internal static class DocumentStore
    {
        static DocumentStore()
        {
            Documents = new ConcurrentDictionary<string, Document>();
        }

        public static IDictionary<string, Document> Documents { get;  }

        public static IAutosupportLanguageDefinition? LanguageDefinition;
    }
}
