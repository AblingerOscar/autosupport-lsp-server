using autosupport_lsp_server.Symbols;
using autosupport_lsp_server.Symbols.Impl;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace autosupport_lsp_server.Serialization
{
    internal class InterfaceDeserializer : IInterfaceDeserializer
    {
        private InterfaceDeserializer() { }

        private static IInterfaceDeserializer? instance = null;
        public static IInterfaceDeserializer Instance
        {
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
            return Symbol.FromXLinq(element, this);
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
