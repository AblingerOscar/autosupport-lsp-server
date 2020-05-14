using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace autosupport_lsp_server.Serialization
{
    public interface IXLinqSerializable
    {
        XElement SerializeToXLinq();
    }
}
