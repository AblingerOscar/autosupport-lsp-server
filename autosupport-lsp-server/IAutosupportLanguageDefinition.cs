namespace autosupport_lsp_server
{
    interface IAutosupportLanguageDefinition
    {
        string LanguageId { get; }
        string LanguageFilePattern { get; }
    }
}
