using autosupport_lsp_server.Symbols;
using System.Xml.Linq;

namespace autosupport_lsp_server.Serialization
{
    public interface IInterfaceDeserializer
    {
        ISymbol DeserializeSymbol(XElement element);
        ITerminal DeserializeTerminalSymbol(XElement element);
        INonTerminal DeserializeNonTerminalSymbol(XElement element);
        IAction DeserializeAction(XElement element);

        IAutosupportLanguageDefinition DeserializeAutosupportLanguageDefinition(XElement element);
        IRule DeserializeRule(XElement element);
        CommentRule DeserializeCommentRule(XElement commentRule);
        CommentRules DeserializeCommentRules(XElement commentRules);
    }
}
