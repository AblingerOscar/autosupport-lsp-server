using autosupport_lsp_server.Terminals;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace autosupport_lsp_server.Serialization
{
    internal interface IInterfaceDeserializer
    {
        public ISymbol DeserializeSymbol(XElement element);
        public ITerminal DeserializeTerminalSymbol(XElement element);
        public INonTerminal DeserializeNonTerminalSymbol(XElement element);
        public IAutosupportLanguageDefinition DeserializeAutosupportLanguageDefinition(XElement element);
    }
}
