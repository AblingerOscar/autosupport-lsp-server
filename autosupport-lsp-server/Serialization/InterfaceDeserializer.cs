using autosupport_lsp_server.Terminals;
using autosupport_lsp_server.Terminals.Impl;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace autosupport_lsp_server.Serialization
{
    internal class InterfaceDeserializer : IInterfaceDeserializer
    {
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
