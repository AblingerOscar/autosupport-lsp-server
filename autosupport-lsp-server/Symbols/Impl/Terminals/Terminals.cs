using autosupport_lsp_server.Parser;
using Sprache;
using System;
using System.Linq;

namespace autosupport_lsp_server.Symbols.Impl.Terminals
{
    internal class StringTerminal : Terminal<string>
    {
        public string String { get; private set; } = "";

        protected override Parser<string> Parser =>
            Parse.Ref(() => Parse.String(String)).Select(s => s.ToString() ?? "");

        protected override int LengthOfParseResult(string parseResult) => parseResult.Length;
    }

    internal class AnyLetterTerminal : Terminal<char>
    {
        protected override Parser<char> Parser => Parse.Letter;

        protected override int LengthOfParseResult(char parseResult) => 1;
    }

    internal class AnyLetterOrDigitTerminal : Terminal<char>
    {
        protected override Parser<char> Parser => Parse.LetterOrDigit;

        protected override int LengthOfParseResult(char parseResult) => 1;
    }

    internal class AnyLowercaseLetterTerminal : Terminal<char>
    {
        protected override Parser<char> Parser => Parse.Lower;

        protected override int LengthOfParseResult(char parseResult) => 1;
    }

    internal class AnyUppercaseLetterTerminal : Terminal<char>
    {
        protected override Parser<char> Parser => Parse.Upper;

        protected override int LengthOfParseResult(char parseResult) => 1;
    }

    internal class AnyCharacterTerminal : Terminal<char>
    {
        protected override Parser<char> Parser => Parse.AnyChar;

        protected override int LengthOfParseResult(char parseResult) => 1;
    }

    internal class AnyDigitTerminal : Terminal<char>
    {
        protected override Parser<char> Parser => Parse.Digit;

        protected override int LengthOfParseResult(char parseResult) => 1;
    }

    internal class AnyWhitespaceTerminal : Terminal<char>
    {
        protected override Parser<char> Parser => Parse.WhiteSpace;

        protected override int LengthOfParseResult(char parseResult) => 1;
    }

    internal class AnyLineEndTerminal : Terminal<string>
    {
        protected override Parser<string> Parser => Parse.LineEnd;

        protected override int LengthOfParseResult(string parseResult) => parseResult.Length;
    }
}
