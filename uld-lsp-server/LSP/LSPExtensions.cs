using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace uld.server.LSP
{
    public static class LSPExtensions
    {
        public static bool IsBefore(this Position pos1, Position pos2)
        {
            return pos1.Line != pos2.Line
                ? pos1.Line < pos2.Line
                : pos1.Character < pos2.Character;
        }

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

        public static int CompareTo(this Position p1, Position p2)
        {
            var lineResult = p1.Line.CompareTo(p2.Line);

            if (lineResult == 0)
                return p1.Character.CompareTo(p2.Character);
            else
                return lineResult;
        }

        public static string ToNiceString(this Position position)
        {
            return $"({position.Line}, {position.Character})";
        }

        public static Position Minus(this Position p1, Position p2)
        {
            return new Position(
                    p1.Line - p2.Line,
                    p1.Line == p2.Line
                        ? p1.Character - p2.Character
                        : p1.Character
                );
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
