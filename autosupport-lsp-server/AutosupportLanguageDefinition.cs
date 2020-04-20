using autosupport_lsp_server.Annotation;
using autosupport_lsp_server.SyntaxTree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace autosupport_lsp_server
{
    internal class AutosupportLanguageDefinition : IAutosupportLanguageDefinition
    {
        private AutosupportLanguageDefinition()
        {
            LanguageId = "";
            LanguageFilePattern = "";
            SyntaxStartingNodes = new string[0];
            SyntaxNodes = new Dictionary<string, ISyntaxTreeNode>();
        }

        [XLinqName("name")]
        public string LanguageId { get; private set; }
        [XLinqName("filePattern")]
        public string LanguageFilePattern  { get; private set; }

        [XLinqName("startingNodes")]
        [XLinqValue("startingNode")]
        public string[] SyntaxStartingNodes { get; private set; }

        [XLinqName("syntaxNodes")]
        [XLinqKeys("name")]
        [XLinqValue("syntaxNode")]
        public IDictionary<string, ISyntaxTreeNode> SyntaxNodes { get; private set; }

        public void SerializeToFile(string fileName)
        {
            using TextWriter writer = new StreamWriter(fileName);
            SerializeToStream(writer);
        }

        public void SerializeToStream(TextWriter writer)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(AutosupportLanguageDefinition));
            serializer.Serialize(writer, this);
        }

        public XElement SerializeToXLinq()
        {
            return new XElement("LanguageDefinition",
                new XAttribute(annotation.PropertyName(nameof(LanguageId)), LanguageId),
                new XAttribute(annotation.PropertyName(nameof(LanguageFilePattern)), LanguageFilePattern),
                new XElement(annotation.PropertyName(nameof(SyntaxStartingNodes)),
                    from node in SyntaxStartingNodes
                    select new XElement(annotation.ValuesName(nameof(SyntaxStartingNodes)), node)),
                new XElement(annotation.PropertyName(nameof(SyntaxNodes)),
                    from node in SyntaxNodes
                    select new XElement(annotation.ValuesName(nameof(SyntaxNodes)),
                        new XAttribute(annotation.KeysName(nameof(SyntaxNodes)), node.Key),
                        node.Value.SerializeToXLinq()
                    )
                );
        }

        private readonly static AnnotationUtils.XLinqClassAnnotationUtil annotation = AnnotationUtils.XLinqOf(typeof(AutosupportLanguageDefinition));

        public static AutosupportLanguageDefinition FromXLinq(XElement element)
        {
            return new AutosupportLanguageDefinition()
            {
                LanguageId = element.Attribute(annotation.PropertyName(nameof(LanguageId))).Value,
                LanguageFilePattern = element.Attribute(annotation.PropertyName(nameof(LanguageFilePattern))).Value,
                SyntaxStartingNodes = (from node in element
                                        .Element(annotation.PropertyName(nameof(SyntaxStartingNodes)))
                                        .Elements(annotation.ValuesName(nameof(SyntaxStartingNodes)))
                                      select node.Value)
                                      .ToArray(),
                SyntaxNodes = (from node in element
                                    .Element(annotation.PropertyName(nameof(SyntaxNodes)))
                                    .Elements(annotation.ValuesName(nameof(SyntaxNodes)))
                               select node)
                               .ToDictionary(
                                    node => node.Attribute(annotation.KeysName(nameof(SyntaxNodes))).Value,
                                    node => ISyntaxTreeNode.FromXLinq(node))
            };
        }
    }
}
