using autosupport_lsp_server.SyntaxTree;
using System.Collections.Generic;

namespace autosupport_lsp_server
{
    interface IAutosupportLanguageDefinition
    {
        string LanguageId { get; }
        string LanguageFilePattern { get; }

        string[] SyntaxStartingNodes { get; } 
        IDictionary<string, ISyntaxTreeNode> SyntaxNodes { get; }
    }
}
