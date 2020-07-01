using autosupport_lsp_server.Serialization;
using autosupport_lsp_server.Serialization.Annotation;
using System;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using static autosupport_lsp_server.Serialization.Annotation.AnnotationUtils;

namespace autosupport_lsp_server.Symbols.Impl
{
    [XLinqName("oneOf")]
    public class OneOf : IOneOf
    {
        [XLinqKeys("option")]
        public string[] Options { get; private set; } = new string[0];

        [XLinqName("allowNone")]
        public bool AllowNone { get; private set; } = false;

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
            return new XElement(annotation.ClassName(),
                Options.Select<string, object>(option =>
                    new XElement(annotation.PropertyName(nameof(Options)), option))
                .Prepend(new XAttribute(annotation.PropertyName(nameof(AllowNone)), XmlConvert.ToString(AllowNone)))
                .ToArray());
        }

        public static IOneOf FromXLinq(XElement element, IInterfaceDeserializer interfaceDeserializer)
        {
            return new OneOf()
            {
                AllowNone = XmlConvert.ToBoolean(element.Attribute(annotation.PropertyName(nameof(AllowNone))).Value),
                Options = (from optionXml in element.Elements(annotation.KeysName(nameof(Options)))
                           select optionXml.Value).ToArray()
            };
        }

        public override string? ToString()
        {
            return new StringBuilder()
                .Append("OneOf(")
                .AppendJoin(", ", Options)
                .Append(')')
                .If(AllowNone, sb => sb.Append('?'))
                .ToString();
        }

        private static readonly XLinqClassAnnotationUtil annotation = AnnotationUtils.XLinqOf(typeof(OneOf));
    }
}
