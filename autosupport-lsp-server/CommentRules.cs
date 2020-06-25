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
        public readonly CommentRule[] normalComments;
        [XLinqName("documentationComments")]
        public readonly CommentRule[] documentationComments;

        public CommentRules(CommentRule[] normalComments, CommentRule[] documentationComments)
            => (this.normalComments, this.documentationComments) = (normalComments, documentationComments);

        public override bool Equals(object? obj)
            => obj is CommentRules rules &&
                   EqualityComparer<CommentRule[]>.Default.Equals(normalComments, rules.normalComments) &&
                   EqualityComparer<CommentRule[]>.Default.Equals(documentationComments, rules.documentationComments);

        public override int GetHashCode()
            => HashCode.Combine(normalComments, documentationComments);

        public override string? ToString()
            => $"normal comments: {normalComments.JoinToString(", ")}\n" +
               $"documentation comments: {documentationComments.JoinToString(", ")}";

        private readonly static AnnotationUtils.XLinqClassAnnotationUtil annotation = AnnotationUtils.XLinqOf(typeof(CommentRules));

        internal object SerializeToXLinq()
        {
            return new XElement(annotation.ClassName(),
                    new XElement(
                        annotation.PropertyName(nameof(normalComments)),
                        normalComments.Select(nc => nc.SerializeToXLinq())),
                    new XElement(
                        annotation.PropertyName(nameof(documentationComments)),
                        documentationComments.Select(nc => nc.SerializeToXLinq()))
                );
        }

        internal static CommentRules FromXLinq(XElement? element, InterfaceDeserializer interfaceDeserializer)
        {
            if (element == null)
                return new CommentRules(new CommentRule[0], new CommentRule[0]);

            return new CommentRules(
                               (from rule in element
                                               .Element(annotation.PropertyName(nameof(normalComments)))
                                               .Elements()
                                select interfaceDeserializer.DeserializeCommentRule(rule)).ToArray(),
                               (from rule in element
                                               .Element(annotation.PropertyName(nameof(documentationComments)))
                                               .Elements()
                                select interfaceDeserializer.DeserializeCommentRule(rule)).ToArray()
                           );
        }
    }
}