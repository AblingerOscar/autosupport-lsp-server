using autosupport_lsp_server.Serialization;
using autosupport_lsp_server.Serialization.Annotation;
using System;
using System.Xml.Linq;

namespace autosupport_lsp_server.Symbols.Impl
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

        [XLinqName("referencedRule")]
        public string ReferencedRule { get; private set; } = "";

        public override void Match(Action<ITerminal> terminal, Action<INonTerminal> nonTerminal, Action<IAction> action, Action<IOperation> operation)
        {
            nonTerminal.Invoke(this);
        }

        public override R Match<R>(Func<ITerminal, R> terminal, Func<INonTerminal, R> nonTerminal, Func<IAction, R> action, Func<IOperation, R> operation)
        {
            return nonTerminal.Invoke(this);
        }

        private static readonly AnnotationUtils.XLinqClassAnnotationUtil annotation = AnnotationUtils.XLinqOf(typeof(NonTerminal));

        public override XElement SerializeToXLinq()
        {
            var element = base.SerializeToXLinq();

            element.SetAttributeValue(
                    annotation.PropertyName(nameof(ReferencedRule)),
                    ReferencedRule
                );

            return element;
        }

        public static new NonTerminal FromXLinq(XElement element, IInterfaceDeserializer interfaceDeserializer)
        {
            var symbol = new NonTerminal()
            {
                ReferencedRule = element.Attribute(annotation.PropertyName(nameof(ReferencedRule))).Value
            };

            AddSymbolValuesFromXLinq(symbol, element, interfaceDeserializer);

            return symbol;
        }
    }
}
