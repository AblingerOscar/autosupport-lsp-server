using autosupport_lsp_server.Parser;
using autosupport_lsp_server.Serialization;
using Sprache;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace autosupport_lsp_server.Symbols.Impl
{
    internal abstract class Terminal<T> : Symbol, ITerminal
    {
        protected Terminal() { /* do nothing */ }

        protected abstract Parser<T> Parser { get; }
        protected abstract int LengthOfParseResult(T parseResult);

        public override bool IsTerminal {
            get => true;
            protected set {
                if (value)
                    throw new InvalidOperationException($"Cannot change {nameof(IsTerminal)} of a Terminal class to false");
            }
        }

        public override void Match(Action<ITerminal> terminal, Action<INonTerminal> nonTerminal, Action<IAction> action, Action<IOperation> operation)
        {
            terminal.Invoke(this);
        }

        public override R Match<R>(Func<ITerminal, R> terminal, Func<INonTerminal, R> nonTerminal, Func<IAction, R> action, Func<IOperation, R> operation)
        {
            return terminal.Invoke(this);
        }

        public override XElement SerializeToXLinq()
        {
            return base.SerializeToXLinq();
        }

        public static new ITerminal FromXLinq(XElement element, IInterfaceDeserializer interfaceDeserializer)
        {
            var symbol = new Terminal(); // TODO

            AddSymbolValuesFromXLinq(symbol, element, interfaceDeserializer);

            return symbol;
        }

        public ParseState TryParseNext(ParseState state)
        {
            var parseResult = Parser.TryParse(state.GetNextTextFromPosition());
            return state.Clone()
                .WithPositionOffsetBy(LengthOfParseResult(parseResult.Value))
                .WithFailed(!parseResult.WasSuccessful)
                .Build();
        }
    }
}
