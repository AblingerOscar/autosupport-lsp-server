using autosupport_lsp_server.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace autosupport_lsp_server.Symbols
{
    interface ISymbol
    {
        string Id { get; }
        Uri Source { get; }
        string Documentation { get; }

        bool IsTerminal { get; }
        void Match(Action<ITerminal> terminal, Action<INonTerminal> nonTerminal);
        R Match<R>(Func<ITerminal, R> terminal, Func<INonTerminal, R> nonTerminal);
        XElement SerializeToXLinq();
    }
}
