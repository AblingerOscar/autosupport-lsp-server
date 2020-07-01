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

        public const string IDENTIFIER_KIND = "identifierKind";
        public const string IDENTIFIER_KIND_ARG_SET = "set";

        public const string DECLARATION = "declaration";

        public const string IMPLEMENTATION = "implementation";

        public const string FOLDING = "folding";
        public const string FOLDING_START = "start";
        public const string FOLDING_END = "end";
    }
}
