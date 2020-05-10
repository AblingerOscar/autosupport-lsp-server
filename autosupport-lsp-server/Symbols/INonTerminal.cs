using System;
using System.Collections.Generic;
using System.Text;

namespace autosupport_lsp_server.Symbols
{
    interface INonTerminal : ISymbol
    {
        string ReferencedRule { get; }
    }
}
