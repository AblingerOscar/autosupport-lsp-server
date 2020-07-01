using autosupport_lsp_server.Serialization;
using System.Collections.Immutable;

namespace autosupport_lsp_server.Symbols
{
    public interface IRule : IXLinqSerializable
    {
        string Name { get; }

        IImmutableList<ISymbol> Symbols { get; }
    }
}
