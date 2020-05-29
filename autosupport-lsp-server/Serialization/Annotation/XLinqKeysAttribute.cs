using System;

namespace autosupport_lsp_server.Serialization.Annotation
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal class XLinqKeysAttribute : Attribute
    {
        public string KeysName { get; }

        public XLinqKeysAttribute(string name)
        {
            KeysName = name;
        }
    }
}
