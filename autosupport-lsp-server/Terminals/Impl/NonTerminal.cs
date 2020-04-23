using autosupport_lsp_server.Serialization;
using autosupport_lsp_server.Serialization.Annotation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace autosupport_lsp_server.Terminals.Impl
{
    internal class NonTerminal : Symbol, INonTerminal
    {
        public override bool IsTerminal {
            get => false;
            protected set {
                if (!value)
                    throw new InvalidOperationException($"Cannot change {nameof(IsTerminal)} of a NonTerminal class to true");
            }
        }

        [XLinqName("nextSymbols")]
        [XLinqValue("symbol")]
        public string[] PossibleNextSymbols { get; private set; } = new string[0];

        public override void Match(Action<ITerminal> terminal, Action<INonTerminal> nonTerminal)
        {
            nonTerminal.Invoke(this);
        }

        public override R Match<R>(Func<ITerminal, R> terminal, Func<INonTerminal, R> nonTerminal)
        {
            return nonTerminal.Invoke(this);
        }

        private static readonly AnnotationUtils.XLinqClassAnnotationUtil annotation = AnnotationUtils.XLinqOf(typeof(NonTerminal));

        public override XElement SerializeToXLinq()
        {
            var element = base.SerializeToXLinq();

            element.Add(
                new XElement(annotation.PropertyName(nameof(PossibleNextSymbols)),
                    from symbol in PossibleNextSymbols
                    select new XElement(annotation.ValuesName(nameof(PossibleNextSymbols)))));

            return element;
        }

        public static new NonTerminal FromXLinq(XElement element, IInterfaceDeserializer interfaceDeserializer)
        {
            var symbol = new NonTerminal()
            {
                PossibleNextSymbols = element
                    .Element(annotation.PropertyName(nameof(PossibleNextSymbols)))
                    .Elements(annotation.ValuesName(nameof(PossibleNextSymbols)))
                    .Select(el => el.Value)
                    .ToArray()
            };

            AddSymbolValuesFromXLinq(symbol, element, interfaceDeserializer);

            return symbol;
        }
    }
}
