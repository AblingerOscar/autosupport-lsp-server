using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace autosupport_lsp_server.Serialization
{
    interface IXLinqSerializable
    {
        XElement SerializeToXLinq();
    }
}
