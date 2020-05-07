using autosupport_lsp_server.Serialization;
using autosupport_lsp_server.Serialization.Annotation;
using autosupport_lsp_server.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace autosupport_lsp_server
{
    [XLinqName("languageDefinition")]
    internal class AutosupportLanguageDefinition : IAutosupportLanguageDefinition
    {
        private AutosupportLanguageDefinition()
        {
            LanguageId = "";
            LanguageFilePattern = "";
            StartRules = new string[0];
        }

        [XLinqName("name")]
        public string LanguageId { get; private set; }
        [XLinqName("filePattern")]
        public string LanguageFilePattern  { get; private set; }

        [XLinqName("startRules")]
        [XLinqValue("startRule")]
        public string[] StartRules { get; private set; }

        [XLinqName("rules")]
        public IDictionary<string, IRule> Rules { get; private set; }

        public XElement SerializeToXLinq()
        {
            return new XElement(annotation.ClassName(),
                new XAttribute(annotation.PropertyName(nameof(LanguageId)), LanguageId),
                new XAttribute(annotation.PropertyName(nameof(LanguageFilePattern)), LanguageFilePattern),
                new XElement(annotation.PropertyName(nameof(StartRules)),
                    from node in StartRules
                    select new XElement(annotation.ValuesName(nameof(StartRules)), node)),
                new XElement(annotation.PropertyName(nameof(Rules)),
                    (from rule in Rules
                    select rule.Value.SerializeToXLinq()))
                );
        }

        private readonly static AnnotationUtils.XLinqClassAnnotationUtil annotation = AnnotationUtils.XLinqOf(typeof(AutosupportLanguageDefinition));

        public static AutosupportLanguageDefinition FromXLinq(XElement element, IInterfaceDeserializer interfaceDeserializer)
        {
            return new AutosupportLanguageDefinition()
            {
                LanguageId = element.Attribute(annotation.PropertyName(nameof(LanguageId))).Value,
                LanguageFilePattern = element.Attribute(annotation.PropertyName(nameof(LanguageFilePattern))).Value,
                StartRules = (from node in element
                                        .Element(annotation.PropertyName(nameof(StartRules)))
                                        .Elements(annotation.ValuesName(nameof(StartRules)))
                                      select node.Value)
                                      .ToArray(),
                // TODO: Deserialize Rules
            };
        }
    }
}
