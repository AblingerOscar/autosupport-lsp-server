namespace autosupport_lsp_server.Parsing.Impl
{
    public interface IParser
    {
        IParseResult Parse(string[] text);
    }
}