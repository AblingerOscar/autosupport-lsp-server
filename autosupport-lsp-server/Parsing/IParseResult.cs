namespace autosupport_lsp_server.Parsing
{
    public interface IParseResult
    {
        bool Finished { get; }

        string[] PossibleContinuations { get; }

        IError[] Errors { get; }
    }
}
