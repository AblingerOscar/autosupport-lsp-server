using System;

namespace autosupport_lsp_server.Parsing.Impl
{
    public interface IParser
    {
        IParseResult Parse(Uri uri, string[] text);
    }
}