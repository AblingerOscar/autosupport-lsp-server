using autosupport_lsp_server.Serialization;
using autosupport_lsp_server.Serialization.Annotation;
using autosupport_lsp_server.Symbols;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace autosupport_lsp_server
{
    [XLinqName("languageDefinition")]
    public class AutosupportLanguageDefinition : IAutosupportLanguageDefinition
    {
        private AutosupportLanguageDefinition()
        {
            LanguageId = "";
            LanguageFilePattern = "";
            CommentRules = new CommentRules();
            StartRules = new string[0];
            Rules = new Dictionary<string, IRule>();
        }

        [XLinqName("name")]
        public string LanguageId { get; private set; }
        [XLinqName("filePattern")]
        public string LanguageFilePattern { get; private set; }

        public CommentRules CommentRules { get; private set; }

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
                CommentRules.SerializeToXLinq(),
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
            if (element == null)
                throw new System.ArgumentException("'element' is null");

            return new AutosupportLanguageDefinition()
            {
                LanguageId = element.Attribute(annotation.PropertyName(nameof(LanguageId))).Value,
                LanguageFilePattern = element.Attribute(annotation.PropertyName(nameof(LanguageFilePattern))).Value,
                CommentRules = interfaceDeserializer
                    .DeserializeCommentRules(element.Element(annotation.ClassName(typeof(CommentRules)))),
                StartRules = (from startRuleElement in element
                                        .Element(annotation.PropertyName(nameof(StartRules)))
                                        .Elements(annotation.ValuesName(nameof(StartRules)))
                              select startRuleElement.Value)
                              .ToArray(),
                Rules = (from ruleElement in element
                                        .Element(annotation.PropertyName(nameof(Rules)))
                                        .Elements()
                         select interfaceDeserializer.DeserializeRule(ruleElement))
                         .ToDictionary(rule => rule.Name)
            };
        }
    }
}
