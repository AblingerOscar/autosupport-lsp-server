using autosupport_lsp_server.Serialization;
using autosupport_lsp_server.Serialization.Annotation;
using System;
using System.Xml;
using System.Xml.Linq;

namespace autosupport_lsp_server.Terminals.Impl
{
    [XLinqName("symbol")]
    internal abstract class Symbol : ISymbol
    {
        [XLinqName("id")]
        public string Id { get; private set; } = "";
        [XLinqName("isTerminal")]
        public abstract bool IsTerminal { get; protected set; }
        [XLinqName("source")]
        public Uri Source { get; private set; } = new Uri("file:///root");
        [XLinqName("documentation")]
        public string Documentation { get; private set; } = "";

        public abstract void Match(Action<ITerminal> terminal, Action<INonTerminal> nonTerminal);

        public abstract R Match<R>(Func<ITerminal, R> terminal, Func<INonTerminal, R> nonTerminal);


        public virtual XElement SerializeToXLinq()
        {
            return new XElement(annotation.ClassName(),
                new XAttribute(annotation.PropertyName(nameof(Id)), Id),
                new XAttribute(annotation.PropertyName(nameof(IsTerminal)), IsTerminal.ToString().ToLower()),
                new XAttribute(annotation.PropertyName(nameof(Source)), Source.ToString()),
                new XElement(annotation.PropertyName(nameof(Documentation)), Documentation)
                );
        }

        protected static void AddSymbolValuesFromXLinq(Symbol symbol, XElement element, IInterfaceDeserializer interfaceDeserializer)
        {
            symbol.Id = element.Attribute(annotation.PropertyName(nameof(Id))).Value;
            symbol.Source = new Uri(element.Attribute(annotation.PropertyName(nameof(Source))).Value);
            symbol.Documentation = element.Element(annotation.PropertyName(nameof(Documentation))).Value;
        }

        private static readonly AnnotationUtils.XLinqClassAnnotationUtil annotation = AnnotationUtils.XLinqOf(typeof(Symbol));

        public static ISymbol FromXLinq(XElement element, IInterfaceDeserializer interfaceDeserializer)
        {
            if (XmlConvert.ToBoolean(element.Attribute(annotation.PropertyName(nameof(IsTerminal))).Value))
            {
                return interfaceDeserializer.DeserializeTerminalSymbol(element);
            } else
            {
                return interfaceDeserializer.DeserializeNonTerminalSymbol(element);
            }
        }
    }
}