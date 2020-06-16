using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace autosupport_lsp_server.LSP
{
    public static class LSPExtensions
    {
        public static bool IsIn(this Position position, Range range)
        {
            return range.Start.Line <= position.Line
                && range.Start.Character <= position.Character
                && range.End.Line >= position.Line
                && range.End.Character >= position.Character;
        }

        public static Position Clone(this Position position)
        {
            return new Position(position.Line, position.Character);
        }
    }
}
