using autosupport_lsp_server.Serialization;
using autosupport_lsp_server.Serialization.Annotation;
using autosupport_lsp_server.Terminals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace autosupport_lsp_server
{
    [XLinqName("LanguageDefinition")]
    internal class AutosupportLanguageDefinition : IAutosupportLanguageDefinition
    {
        private AutosupportLanguageDefinition()
        {
            LanguageId = "";
            LanguageFilePattern = "";
            StartingSymbols = new string[0];
            TerminalSymbols = new Dictionary<string, ITerminal>();
            NonTerminalSymbols = new Dictionary<string, INonTerminal>();
        }

        [XLinqName("name")]
        public string LanguageId { get; private set; }
        [XLinqName("filePattern")]
        public string LanguageFilePattern  { get; private set; }

        [XLinqName("startingSymbols")]
        [XLinqValue("startingSymbol")]
        public string[] StartingSymbols { get; private set; }

        [XLinqName("terminalSymbols")]
        public IDictionary<string, ITerminal> TerminalSymbols { get; private set; }

        [XLinqName("nonterminalSymbols")]
        public IDictionary<string, INonTerminal> NonTerminalSymbols { get; private set; }

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
            return new XElement(annotation.ClassName(),
                new XAttribute(annotation.PropertyName(nameof(LanguageId)), LanguageId),
                new XAttribute(annotation.PropertyName(nameof(LanguageFilePattern)), LanguageFilePattern),
                new XElement(annotation.PropertyName(nameof(StartingSymbols)),
                    from node in StartingSymbols
                    select new XElement(annotation.ValuesName(nameof(StartingSymbols)), node)),
                new XElement(annotation.PropertyName(nameof(TerminalSymbols)),
                    from term in TerminalSymbols
                    select term.Value.SerializeToXLinq()),
                new XElement(annotation.PropertyName(nameof(NonTerminalSymbols)),
                    from term in NonTerminalSymbols
                    select term.Value.SerializeToXLinq())
                );
        }

        private readonly static AnnotationUtils.XLinqClassAnnotationUtil annotation = AnnotationUtils.XLinqOf(typeof(AutosupportLanguageDefinition));

        public static AutosupportLanguageDefinition FromXLinq(XElement element, IInterfaceDeserializer interfaceDeserializer)
        {
            return new AutosupportLanguageDefinition()
            {
                LanguageId = element.Attribute(annotation.PropertyName(nameof(LanguageId))).Value,
                LanguageFilePattern = element.Attribute(annotation.PropertyName(nameof(LanguageFilePattern))).Value,
                StartingSymbols = (from node in element
                                        .Element(annotation.PropertyName(nameof(StartingSymbols)))
                                        .Elements(annotation.ValuesName(nameof(StartingSymbols)))
                                      select node.Value)
                                      .ToArray(),
                TerminalSymbols = (from node in element
                                    .Element(annotation.PropertyName(nameof(TerminalSymbols)))
                                    .Elements()
                                  select interfaceDeserializer.DeserializeTerminalSymbol(node))
                                  .ToDictionary(term => term.Id),
                NonTerminalSymbols = (from node in element
                                    .Element(annotation.PropertyName(nameof(NonTerminalSymbols)))
                                    .Elements()
                                  select interfaceDeserializer.DeserializeNonTerminalSymbol(node))
                                  .ToDictionary(term => term.Id)
            };
        }
    }
}
