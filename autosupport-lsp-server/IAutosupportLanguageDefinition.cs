using autosupport_lsp_server.Symbols;
using System.Collections.Generic;

namespace autosupport_lsp_server
{
    interface IAutosupportLanguageDefinition
    {
        string LanguageId { get; }
        string LanguageFilePattern { get; }

        string[] StartRules { get; } 
        public IDictionary<string, IRule> Rules { get; }
    }
}
