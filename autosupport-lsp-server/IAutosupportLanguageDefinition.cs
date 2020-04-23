using autosupport_lsp_server.Terminals;
using System.Collections.Generic;

namespace autosupport_lsp_server
{
    interface IAutosupportLanguageDefinition
    {
        string LanguageId { get; }
        string LanguageFilePattern { get; }

        string[] StartingSymbols { get; } 
        public IDictionary<string, ITerminal> TerminalSymbols { get; }
        public IDictionary<string, INonTerminal> NonTerminalSymbols { get; }
    }
}
