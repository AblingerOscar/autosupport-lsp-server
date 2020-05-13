using autosupport_lsp_server.Serialization.Annotation;
using autosupport_lsp_server.Symbols;
using autosupport_lsp_server.Symbols.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace autosupport_lsp_server.Serialization
{
    internal class InterfaceDeserializer : IInterfaceDeserializer
    {
        private InterfaceDeserializer() { }

        private static IInterfaceDeserializer? instance = null;
        public static IInterfaceDeserializer Instance {
            get {
                if (instance == null)
                {
                    instance = new InterfaceDeserializer();
                }
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
                } // TODO: operations & Actions
            }

            throw new ArgumentException($"The given Element '{element.Name}' does not exist or is not a symbol");
        }

        public INonTerminal DeserializeNonTerminalSymbol(XElement element)
        {
            return NonTerminal.FromXLinq(element, this);
        }

        public ITerminal DeserializeTerminalSymbol(XElement element)
        {
            return Terminal.FromXLinq(element, this);
        }
    }
}
