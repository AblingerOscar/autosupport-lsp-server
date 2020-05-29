using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace autosupport_lsp_server.Parsing
{
    public interface IError
    {
        Position Position { get; }

        string Reason { get; }
    }
}
