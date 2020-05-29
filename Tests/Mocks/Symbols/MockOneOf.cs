using autosupport_lsp_server.Symbols;
using System;
using System.Xml.Linq;

namespace Tests.Mocks.Symbols
{
    public abstract class MockOneOf : IOneOf
    {
        public abstract string[] Options { get; }

        public abstract bool AllowNone { get; }

        public void Match(Action<ITerminal> terminal, Action<INonTerminal> nonTerminal, Action<IAction> action, Action<IOneOf> oneOf)
        {
            oneOf.Invoke(this);
        }

        public R Match<R>(Func<ITerminal, R> terminal, Func<INonTerminal, R> nonTerminal, Func<IAction, R> action, Func<IOneOf, R> oneOf)
        {
            return oneOf.Invoke(this);
        }

        public abstract XElement SerializeToXLinq();
    }
}
