namespace autosupport_lsp_server.Symbols
{
    interface IParsable
    {
        int MinimumNumberOfCharactersToParse { get; }

        bool TryParse(string str);
    }
}
