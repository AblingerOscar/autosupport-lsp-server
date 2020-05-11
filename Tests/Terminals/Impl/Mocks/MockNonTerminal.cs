using autosupport_lsp_server.Symbols;
using autosupport_lsp_server.Symbols.Impl;
using System;

namespace Tests.Terminals.Impl.Mocks
{
    internal abstract class MockNonTerminal : Symbol, INonTerminal
    {
        public abstract string ReferencedRule { get; }

        public override void Match(Action<ITerminal> terminal, Action<INonTerminal> nonTerminal, Action<IAction> action, Action<IOperation> operation)
        {
            nonTerminal.Invoke(this);
        }

        public override R Match<R>(Func<ITerminal, R> terminal, Func<INonTerminal, R> nonTerminal, Func<IAction, R> action, Func<IOperation, R> operation)
        {
            return nonTerminal.Invoke(this);
        }
    }
}
