using autosupport_lsp_server.Serialization;
using autosupport_lsp_server.Serialization.Annotation;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using static autosupport_lsp_server.Serialization.Annotation.AnnotationUtils;

namespace autosupport_lsp_server.Symbols.Impl
{
    [XLinqName("rule")]
    public class Rule : IRule
    {
        [XLinqName("name")]
        public string Name { get; private set; } = "";

        public IImmutableList<ISymbol> Symbols { get; private set; } = ImmutableList<ISymbol>.Empty;

        public XElement SerializeToXLinq()
        {
            return new XElement(annotation.ClassName(),
                Symbols.Select<ISymbol, object>(symbol => symbol.SerializeToXLinq())
                .Prepend(new XAttribute(annotation.PropertyName(nameof(Name)), Name))
                .ToArray());
        }

        public static IRule FromXLinq(XElement element, IInterfaceDeserializer interfaceDeserializer)
        {
            return new Rule()
            {
                Name = element.Attribute(annotation.PropertyName(nameof(Name))).Value,
                Symbols = element.Elements()
                            .Select(symbol => interfaceDeserializer.DeserializeSymbol(symbol))
                            .ToImmutableList()
            };
        }

        private static readonly XLinqClassAnnotationUtil annotation = AnnotationUtils.XLinqOf(typeof(Rule));

        public override string? ToString()
        {
            return $"{Name} -> {string.Join(' ', Symbols.Select(s => s.ToString()))}.";
        }
    }
}
