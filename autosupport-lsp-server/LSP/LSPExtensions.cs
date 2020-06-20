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

        public static string ToNiceString(this Position position)
        {
            return $"({position.Line}, {position.Character})";
        }

        public static Range Clone(this Range range)
        {
            return new Range(range.Start.Clone(), range.End.Clone());
        }

        public static string ToNiceString(this Range range)
        {
            return $"{range.Start}-{range.End}";
        }
    }
}
