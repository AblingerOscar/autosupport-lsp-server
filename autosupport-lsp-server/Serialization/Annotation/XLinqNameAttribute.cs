using System;
using System.Collections.Generic;
using System.Text;

namespace autosupport_lsp_server.Serialization.Annotation
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Interface, Inherited = true)]
    internal class XLinqNameAttribute : Attribute
    {
        public string Name { get; }

        public XLinqNameAttribute(string name)
        {
            Name = name;
        }
    }
}
