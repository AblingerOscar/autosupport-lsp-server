using System;
using System.Collections.Generic;
using System.Text;

namespace autosupport_lsp_server.Annotation
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal class XLinqKeysAttribute: Attribute
    {
        public string KeysName { get; }

        public XLinqKeysAttribute(string name)
        {
            KeysName = name;
        }
    }
}
