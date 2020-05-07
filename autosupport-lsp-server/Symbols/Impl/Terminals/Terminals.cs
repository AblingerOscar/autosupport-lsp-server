using System;
using System.Collections.Generic;
using System.Text;

namespace autosupport_lsp_server.Symbols.Impl.Terminals
{
    internal class StringTerminal : Terminal
    {
        public string String { get; private set; } = "";
    }

    internal class AnyLetterTerminal : Terminal
    {
    }

    internal class AnyLetterOrDigitTerminal : Terminal
    {
    }

    internal class AnyLowercaseLetterTerminal : Terminal
    {
    }

    internal class AnyUppercaseLetterTerminal : Terminal
    {
    }

    internal class AnyCharacterTerminal : Terminal
    {
    }

    internal class AnyDigitTerminal : Terminal
    {
    }

    internal class AnyWhitespaceTerminal : Terminal
    {
    }

    internal class AnyLineTerminatorTerminal : Terminal
    {
    }
}
