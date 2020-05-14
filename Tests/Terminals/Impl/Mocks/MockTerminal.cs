using autosupport_lsp_server.Symbols;
using autosupport_lsp_server.Symbols.Impl;
using System;

namespace Tests.Terminals.Impl.Mocks
{
    public abstract class MockTerminal : Symbol, ITerminal
    {
        public abstract int MinimumNumberOfCharactersToParse { get; }

        public abstract bool TryParse(string str);

        public override void Match(Action<ITerminal> terminal, Action<INonTerminal> nonTerminal, Action<IAction> action, Action<IOperation> operation)
        {
            terminal.Invoke(this);
        }

        public override R Match<R>(Func<ITerminal, R> terminal, Func<INonTerminal, R> nonTerminal, Func<IAction, R> action, Func<IOperation, R> operation)
        {
            return terminal.Invoke(this);
        }
    }
}
