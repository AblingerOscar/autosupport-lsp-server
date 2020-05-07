using autosupport_lsp_server.Serialization;
using System.Xml.Linq;

namespace autosupport_lsp_server.Symbols
{
    interface IRule : IParsable, IXLinqSerializable
    {
    }
}
