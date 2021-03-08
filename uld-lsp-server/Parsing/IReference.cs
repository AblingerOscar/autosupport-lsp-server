using System;

namespace uld.server.Parsing
{
    public interface IReference
    {
        OmniSharp.Extensions.LanguageServer.Protocol.Models.Range Range { get; }
        Uri Uri { get; }
    }
}