namespace autosupport_lsp_server.Symbols
{
    public interface IParsable
    {
        int MinimumNumberOfCharactersToParse { get; }

        bool TryParse(string str);
    }
}
