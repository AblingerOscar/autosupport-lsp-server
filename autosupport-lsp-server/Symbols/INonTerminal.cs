namespace autosupport_lsp_server.Symbols
{
    public interface INonTerminal : ISymbol
    {
        string ReferencedRule { get; }
    }
}
