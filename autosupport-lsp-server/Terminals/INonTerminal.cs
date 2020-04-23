using System;
using System.Collections.Generic;
using System.Text;

namespace autosupport_lsp_server.Terminals
{
    interface INonTerminal: ISymbol
    {
        string[] PossibleNextSymbols { get; }
    }
}
