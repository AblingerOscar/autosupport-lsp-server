namespace autosupport_lsp_server.Symbols
{
    public interface IAction : ISymbol
    {
        string Command { get; }

        string GetBaseCommand();

        string[] GetArguments();

        public const string IDENTIFIER = "identifier";

        public const string IDENTIFIER_TYPE = "identifierType";
        public const string IDENTIFIER_TYPE_ARG_SET = "set";
        public const string IDENTIFIER_TYPE_ARG_INNER = "inner";
    }
}
