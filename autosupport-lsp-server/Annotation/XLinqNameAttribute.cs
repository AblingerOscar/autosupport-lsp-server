using System;
using System.Collections.Generic;
using System.Text;

namespace autosupport_lsp_server.Annotation
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal class XLinqNameAttribute: Attribute
    {
        public string Name { get; }

        public XLinqNameAttribute(string name)
        {
            Name = name;
        }
    }
}
