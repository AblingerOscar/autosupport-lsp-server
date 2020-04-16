using System;
using System.Collections.Generic;
using System.Text;

namespace autosupport_lsp_server
{
    internal class AutosupportLanguageDefinition : IAutosupportLanguageDefinition
    {
        public AutosupportLanguageDefinition(string languageId, string languageFilePattern)
        {
            LanguageId = languageId;
            LanguageFilePattern = languageFilePattern;
        }

        public string LanguageId { get; }
        public string LanguageFilePattern  { get; }
    }
}
