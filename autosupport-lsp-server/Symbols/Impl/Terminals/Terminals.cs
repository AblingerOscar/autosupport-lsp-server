using autosupport_lsp_server.Parsing;
using Sprache;
using System;
using System.Linq;

namespace autosupport_lsp_server.Symbols.Impl.Terminals
{
    internal class StringTerminal : Terminal
    {
        public StringTerminal(string str)
        {
            String = str;
            PossibleContent = new string[] { String };
        }

        public string String { get; }

        public override int MinimumNumberOfCharactersToParse => String.Length;

        public override string[] PossibleContent { get; }

        protected override Parser<string> Parser =>
            Parse.Ref(() => Parse.String(String)).Text();
    }

    internal abstract class CharTerminal : Terminal
    {
        public override int MinimumNumberOfCharactersToParse => 1;

        public override string[] PossibleContent { get; } = new string[0];

        protected abstract Parser<char> CharParser { get; }

        protected override Parser<string> Parser => CharParser.Select(ch => ch.ToString());
    }

    internal class AnyLineEndTerminal : CharTerminal
    {
        public override string[] PossibleContent { get; } = new string[] { Constants.NewLine.ToString() };

        // note that when adding Text together all newline will be converted to
        // Constants.Newline
        protected override Parser<char> CharParser => Parse.Char(Constants.NewLine);
    }

    internal class AnyLetterTerminal : CharTerminal
    {
        protected override Parser<char> CharParser => Parse.Letter;
    }

    internal class AnyLetterOrDigitTerminal : CharTerminal
    {
        protected override Parser<char> CharParser => Parse.LetterOrDigit;
    }

    internal class AnyLowercaseLetterTerminal : CharTerminal
    {
        protected override Parser<char> CharParser => Parse.Lower;
    }

    internal class AnyUppercaseLetterTerminal : CharTerminal
    {
        protected override Parser<char> CharParser => Parse.Upper;
    }

    internal class AnyCharacterTerminal : CharTerminal
    {
        protected override Parser<char> CharParser => Parse.AnyChar;
    }

    internal class AnyDigitTerminal : CharTerminal
    {
        protected override Parser<char> CharParser => Parse.Digit;
    }

    internal class AnyWhitespaceTerminal : CharTerminal
    {
        public override string[] PossibleContent { get; } = new string[] { " " };

        protected override Parser<char> CharParser => Parse.WhiteSpace;
    }
}
