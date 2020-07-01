using autosupport_lsp_server.Serialization.Annotation;
using Sprache;
using System;
using System.Linq;

namespace autosupport_lsp_server.Symbols.Impl.Terminals
{
    [XLinqName("string")]
    public class StringTerminal : Terminal
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

        public override string? ToString()
        {
            return base.ToString() + $"({String})";
        }
    }

    public abstract class CharTerminal : Terminal
    {
        public override int MinimumNumberOfCharactersToParse => 1;

        public override string[] PossibleContent { get; } = new string[0];

        protected abstract Parser<char> CharParser { get; }

        protected override Parser<string> Parser => CharParser.Select(ch => ch.ToString());
    }

    [XLinqName("lineEnd")]
    public class AnyLineEndTerminal : CharTerminal
    {
        public override string[] PossibleContent { get; } = new string[] { Constants.NewLine.ToString() };

        // note that when adding Text together all newline will be converted to
        // Constants.Newline
        protected override Parser<char> CharParser => Parse.Char(Constants.NewLine);
    }

    [XLinqName("letter")]
    public class AnyLetterTerminal : CharTerminal
    {
        protected override Parser<char> CharParser => Parse.Letter;
    }

    [XLinqName("letterOrDigit")]
    public class AnyLetterOrDigitTerminal : CharTerminal
    {
        protected override Parser<char> CharParser => Parse.LetterOrDigit;
    }

    [XLinqName("lowercaseLetter")]
    public class AnyLowercaseLetterTerminal : CharTerminal
    {
        protected override Parser<char> CharParser => Parse.Lower;
    }

    [XLinqName("uppercaseLetter")]
    public class AnyUppercaseLetterTerminal : CharTerminal
    {
        protected override Parser<char> CharParser => Parse.Upper;
    }

    [XLinqName("character")]
    public class AnyCharacterTerminal : CharTerminal
    {
        protected override Parser<char> CharParser => Parse.AnyChar;
    }

    [XLinqName("digit")]
    public class AnyDigitTerminal : CharTerminal
    {
        protected override Parser<char> CharParser => Parse.Digit;
    }

    [XLinqName("whitespace")]
    public class AnyWhitespaceTerminal : CharTerminal
    {
        public override string[] PossibleContent { get; } = new string[] { " " };

        protected override Parser<char> CharParser => Parse.WhiteSpace;
    }
}
