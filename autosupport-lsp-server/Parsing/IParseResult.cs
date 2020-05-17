namespace autosupport_lsp_server.Parsing
{
    interface IParseResult
    {
        bool Finished { get; }

        string[] PossibleContinuations { get; }

        IError[] Errors { get; }
    }
}
