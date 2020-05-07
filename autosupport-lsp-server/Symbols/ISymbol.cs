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
        Uri Source { get; } // prob. remove
        string Documentation { get; } // prob. remove

        bool IsTerminal { get; }
        void Match(
            Action<ITerminal> terminal,
            Action<INonTerminal> nonTerminal,
            Action<IAction> action,
            Action<IOperation> operation);
        R Match<R>(
            Func<ITerminal, R> terminal,
            Func<INonTerminal, R> nonTerminal,
            Func<IAction, R> action,
            Func<IOperation, R> operation);

        XElement SerializeToXLinq();
    }
}
