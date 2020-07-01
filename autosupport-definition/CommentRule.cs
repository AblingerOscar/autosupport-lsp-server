using autosupport_lsp_server.Serialization;
using autosupport_lsp_server.Serialization.Annotation;
using System;
using System.Xml.Linq;

namespace autosupport_lsp_server
{
    [XLinqName("comment")]
    public readonly struct CommentRule
    {
        public CommentRule(string start, string end, string treatAs)
        {
            Start = start;
            End = end;
            TreatAs = treatAs;
        }

        [XLinqName("start")]
        public string Start { get; }
        [XLinqName("end")]
        public string End { get; }
        [XLinqName("treatAs")]
        public string TreatAs { get; }

        public override bool Equals(object? obj)
        {
            return obj is CommentRule rule &&
                   Start == rule.Start &&
                   End == rule.End;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Start, End);
        }

        public override string? ToString()
        {
            return $"{Start} until {End}";
        }

        private readonly static AnnotationUtils.XLinqClassAnnotationUtil annotation = AnnotationUtils.XLinqOf(typeof(CommentRule));

        internal XElement SerializeToXLinq()
        {
            return new XElement(annotation.ClassName(),
                    new XElement(annotation.PropertyName(nameof(Start)), Start),
                    new XElement(annotation.PropertyName(nameof(End)), End),
                    new XElement(annotation.PropertyName(nameof(TreatAs)), TreatAs)
                );
        }

        internal static CommentRule FromXLinq(XElement element, InterfaceDeserializer interfaceDeserializer)
            => new CommentRule(
                    element.Element(annotation.PropertyName(nameof(Start))).Value,
                    element.Element(annotation.PropertyName(nameof(End))).Value,
                    element.Element(annotation.PropertyName(nameof(TreatAs))).Value
                );
    }
}