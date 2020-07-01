namespace autosupport_lsp_server.Symbols
{
    public interface IOneOf : ISymbol
    {
        string[] Options { get; }

        /// <summary>
        /// If this is true, then, additionaly, to the Options, it is also valid,
        /// to use none of them.
        /// E.g. an optional "NonTerminal?" can be made using a IOneOf with
        /// the only option of "NonTerminal" and AllowNone = true
        /// </summary>
        bool AllowNone { get; }
    }
}
