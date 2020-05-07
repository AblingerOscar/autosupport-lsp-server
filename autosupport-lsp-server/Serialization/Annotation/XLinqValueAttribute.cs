﻿using System;
using System.Collections.Generic;
using System.Text;

namespace autosupport_lsp_server.Serialization.Annotation
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal class XLinqValueAttribute : Attribute
    {
        public string ValuesName { get; }

        public XLinqValueAttribute(string name)
        {
            ValuesName = name;
        }
    }
}