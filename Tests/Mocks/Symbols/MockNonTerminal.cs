using autosupport_lsp_server.Symbols;
using autosupport_lsp_server.Symbols.Impl;
using System;

namespace Tests.Mocks.Symbols
{
    public abstract class MockNonTerminal : Symbol, INonTerminal
    {
        public abstract string ReferencedRule { get; }

        public override void Match(Action<ITerminal> terminal, Action<INonTerminal> nonTerminal, Action<IAction> action, Action<IOneOf> oneOf)
        {
            nonTerminal.Invoke(this);
        }

        public override R Match<R>(Func<ITerminal, R> terminal, Func<INonTerminal, R> nonTerminal, Func<IAction, R> action, Func<IOneOf, R> oneOf)
        {
            return nonTerminal.Invoke(this);
        }
    }
}
