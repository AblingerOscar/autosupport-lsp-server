﻿using autosupport_lsp_server.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace autosupport_lsp_server.Symbols.Impl
{
    internal class Terminal: Symbol, ITerminal
    {
        private Terminal() { /* do nothing */ }

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

        public static new Terminal FromXLinq(XElement element, IInterfaceDeserializer interfaceDeserializer)
        {
            var symbol = new Terminal();

            AddSymbolValuesFromXLinq(symbol, element, interfaceDeserializer);

            return symbol;
        }
    }
}
