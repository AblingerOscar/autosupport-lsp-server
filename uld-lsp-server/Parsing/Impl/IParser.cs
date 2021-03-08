using System;

namespace uld.server.Parsing.Impl
{
    public interface IParser
    {
        IParseResult Parse(Uri uri, string[] text);
    }
}