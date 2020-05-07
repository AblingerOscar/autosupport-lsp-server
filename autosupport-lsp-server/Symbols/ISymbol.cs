using autosupport_lsp_server.Serialization;
using System;

namespace autosupport_lsp_server.Symbols
{
    interface ISymbol : IXLinqSerializable
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
    }
}
