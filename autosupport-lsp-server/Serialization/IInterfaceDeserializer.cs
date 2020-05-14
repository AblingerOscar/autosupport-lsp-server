using autosupport_lsp_server.Symbols;
using System.Xml.Linq;

namespace autosupport_lsp_server.Serialization
{
    public interface IInterfaceDeserializer
    {
        public ISymbol DeserializeSymbol(XElement element);
        public ITerminal DeserializeTerminalSymbol(XElement element);
        public INonTerminal DeserializeNonTerminalSymbol(XElement element);
        public IAutosupportLanguageDefinition DeserializeAutosupportLanguageDefinition(XElement element);
    }
}
