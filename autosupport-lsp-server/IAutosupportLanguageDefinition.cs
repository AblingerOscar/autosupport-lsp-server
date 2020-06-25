using autosupport_lsp_server.Symbols;
using System.Collections.Generic;

namespace autosupport_lsp_server
{
    public interface IAutosupportLanguageDefinition
    {
        string LanguageId { get; }
        string LanguageFilePattern { get; }

        CommentRules CommentRules { get; }

        string[] StartRules { get; }
        IDictionary<string, IRule> Rules { get; }
    }
}
