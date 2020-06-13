namespace autosupport_lsp_server.Symbols
{
    public interface IAction : ISymbol
    {
        string Command { get; }

        string GetBaseCommand();

        string[] GetArguments();

        public const string IDENTIFIER = "identifier";
    }
}
