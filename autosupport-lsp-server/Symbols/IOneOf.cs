namespace autosupport_lsp_server.Symbols
{
    public interface IOneOf : ISymbol
    {
        string[] Options { get; }
    }
}
