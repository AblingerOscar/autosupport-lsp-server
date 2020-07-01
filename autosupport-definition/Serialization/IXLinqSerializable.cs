using System.Xml.Linq;

namespace autosupport_lsp_server.Serialization
{
    public interface IXLinqSerializable
    {
        XElement SerializeToXLinq();
    }
}
