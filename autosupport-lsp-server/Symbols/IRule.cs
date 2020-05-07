using autosupport_lsp_server.Serialization;
using System.Collections.Generic;

namespace autosupport_lsp_server.Symbols
{
    interface IRule : IParsable, IXLinqSerializable
    {
        string Name { get; }

        IList<ISymbol> Symbols { get; }
    }
}
