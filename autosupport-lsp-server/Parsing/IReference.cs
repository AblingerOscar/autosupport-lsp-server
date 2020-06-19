using System;

namespace autosupport_lsp_server.Parsing
{
    public interface IReference
    {
        OmniSharp.Extensions.LanguageServer.Protocol.Models.Range Range { get; }
        Uri Uri { get; }
    }
}