namespace autosupport_lsp_server.Symbols
{
    public interface ITerminal : IParsable, ISymbol
    {
        /// <summary>
        /// Possible valid string that will be successfully
        /// parsed by this terminal.
        /// Used to allow autocompleting.
        /// For terminals where there are too many valid string,
        /// it's better to return an empty array (or select a few
        /// sensible ones)
        /// Note that an empty array means no suggestion and the
        /// predictions will end there.
        /// return an array with an empty string if the suggestions
        /// should continue afterwards
        /// </summary>
        string[] PossibleContent { get; }
    }
}
