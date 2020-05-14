using System;
using System.Xml.Linq;

namespace autosupport_lsp_server.Symbols.Impl
{
    public class OneOf : IOneOf
    {
        public string[] Options { get; private set; } = new string[0];

        public void Match(Action<ITerminal> terminal, Action<INonTerminal> nonTerminal, Action<IAction> action, Action<IOneOf> oneOf)
        {
            oneOf.Invoke(this);
        }

        public R Match<R>(Func<ITerminal, R> terminal, Func<INonTerminal, R> nonTerminal, Func<IAction, R> action, Func<IOneOf, R> oneOf)
        {
            return oneOf.Invoke(this);
        }

        public XElement SerializeToXLinq()
        {
            throw new NotImplementedException();
        }
    }
}
