using autosupport_lsp_server.Serialization;
using autosupport_lsp_server.Serialization.Annotation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace autosupport_lsp_server
{
    [XLinqName("comments")]
    public readonly struct CommentRules
    {
        [XLinqName("normalComments")]
        public readonly CommentRule[] NormalComments { get; }
        [XLinqName("documentationComments")]
        public readonly CommentRule[] DocumentationComments { get; }

        public CommentRules(CommentRule[] normalComments, CommentRule[] documentationComments)
            => (NormalComments, DocumentationComments) = (normalComments, documentationComments);

        public override bool Equals(object? obj)
            => obj is CommentRules rules &&
                   EqualityComparer<CommentRule[]>.Default.Equals(NormalComments, rules.NormalComments) &&
                   EqualityComparer<CommentRule[]>.Default.Equals(DocumentationComments, rules.DocumentationComments);

        public override int GetHashCode()
            => HashCode.Combine(NormalComments, DocumentationComments);

        public override string? ToString()
            => $"normal comments: {NormalComments.JoinToString(", ")}\n" +
               $"documentation comments: {DocumentationComments.JoinToString(", ")}";

        private readonly static AnnotationUtils.XLinqClassAnnotationUtil annotation = AnnotationUtils.XLinqOf(typeof(CommentRules));

        internal object SerializeToXLinq()
        {
            return new XElement(annotation.ClassName(),
                    new XElement(
                        annotation.PropertyName(nameof(NormalComments)),
                        NormalComments.Select(nc => nc.SerializeToXLinq())),
                    new XElement(
                        annotation.PropertyName(nameof(DocumentationComments)),
                        DocumentationComments.Select(nc => nc.SerializeToXLinq()))
                );
        }

        internal static CommentRules FromXLinq(XElement? element, InterfaceDeserializer interfaceDeserializer)
        {
            if (element == null)
                return new CommentRules(new CommentRule[0], new CommentRule[0]);

            return new CommentRules(
                               (from rule in element
                                               .Element(annotation.PropertyName(nameof(NormalComments)))
                                               .Elements()
                                select interfaceDeserializer.DeserializeCommentRule(rule)).ToArray(),
                               (from rule in element
                                               .Element(annotation.PropertyName(nameof(DocumentationComments)))
                                               .Elements()
                                select interfaceDeserializer.DeserializeCommentRule(rule)).ToArray()
                           );
        }
    }
}