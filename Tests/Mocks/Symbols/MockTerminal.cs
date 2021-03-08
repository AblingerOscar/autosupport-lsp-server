using System;
using uld.definition.Symbols;
using uld.definition.Symbols.Impl;

namespace Tests.Mocks.Symbols
{
    public abstract class MockTerminal : Symbol, ITerminal
    {
        public abstract int MinimumNumberOfCharactersToParse { get; }
        public abstract string[] PossibleContent { get; }

        public abstract bool TryParse(string str);

        public override void Match(Action<ITerminal> terminal, Action<INonTerminal> nonTerminal, Action<IAction> action, Action<IOneOf> oneOf)
        {
            terminal.Invoke(this);
        }

        public override R Match<R>(Func<ITerminal, R> terminal, Func<INonTerminal, R> nonTerminal, Func<IAction, R> action, Func<IOneOf, R> oneOf)
        {
            return terminal.Invoke(this);
        }

        public override string? ToString()
        {
            return "Mock Terminal";
        }
    }
}
