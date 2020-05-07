using autosupport_lsp_server.Parser;

namespace autosupport_lsp_server.Symbols
{
    interface IParsable
    {
        ParseState TryParseNext(ParseState state);
    }
}
