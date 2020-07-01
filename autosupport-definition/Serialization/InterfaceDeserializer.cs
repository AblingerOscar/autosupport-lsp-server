using autosupport_lsp_server.Serialization.Annotation;
using autosupport_lsp_server.Symbols;
using autosupport_lsp_server.Symbols.Impl;
using System.Xml.Linq;

namespace autosupport_lsp_server.Serialization
{
    public class InterfaceDeserializer : IInterfaceDeserializer
    {
        private InterfaceDeserializer() { }

        private static IInterfaceDeserializer? instance = null;
        public static IInterfaceDeserializer Instance {
            get {
                if (instance == null)
                    instance = new InterfaceDeserializer();

                return instance;
            }
        }

        public IAutosupportLanguageDefinition DeserializeAutosupportLanguageDefinition(XElement element)
        {
            return AutosupportLanguageDefinition.FromXLinq(element, this);
        }

        public ISymbol DeserializeSymbol(XElement element)
        {
            var symbol = AnnotationUtils.FindTypeWithName(element.Name.ToString());

            if (symbol != null)
            {
                if (typeof(ITerminal).IsAssignableFrom(symbol))
                {
                    return DeserializeTerminalSymbol(element);
                }
                else if (typeof(INonTerminal).IsAssignableFrom(symbol))
                {
                    return DeserializeNonTerminalSymbol(element);
                }
                else if (typeof(IOneOf).IsAssignableFrom(symbol))
                {
                    return DeserializeOneOf(element);
                }
                else if (typeof(IAction).IsAssignableFrom(symbol))
                {
                    return DeserializeAction(element);
                }
            }

            throw new System.ArgumentException($"The given Element '{element.Name}' does not exist or is not a symbol");
        }

        public INonTerminal DeserializeNonTerminalSymbol(XElement element)
        {
            return NonTerminal.FromXLinq(element, this);
        }

        public ITerminal DeserializeTerminalSymbol(XElement element)
        {
            return Terminal.FromXLinq(element, this);
        }

        private ISymbol DeserializeOneOf(XElement element)
        {
            return OneOf.FromXLinq(element, this);
        }

        public IAction DeserializeAction(XElement element)
        {
            return Action.FromXLinq(element, this);
        }

        public IRule DeserializeRule(XElement element)
        {
            return Rule.FromXLinq(element, this);
        }

        public CommentRule DeserializeCommentRule(XElement commentRule)
        {
            return CommentRule.FromXLinq(commentRule, this);
        }

        public CommentRules DeserializeCommentRules(XElement commentRules)
        {
            return CommentRules.FromXLinq(commentRules, this);
        }
    }
}
