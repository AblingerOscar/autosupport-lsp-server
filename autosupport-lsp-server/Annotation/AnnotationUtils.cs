using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace autosupport_lsp_server.Annotation
{
    internal static class AnnotationUtils
    {
        internal static XLinqClassAnnotationUtil XLinqOf(Type type)
        {
            return new XLinqClassAnnotationUtil(type);
        }

        private static string ThrowIfNull(string? str, string errorMsg)
        {
            if (str == null)
                throw new NullReferenceException(errorMsg);

            return str;
        }

        internal class XLinqClassAnnotationUtil
        {
            private readonly Type type;
            internal XLinqClassAnnotationUtil(Type type)
            {
                this.type = type;
            }

            public string PropertyName(string propertyName)
            {
                return ThrowIfNull(
                    type.GetProperty(propertyName)
                        ?.GetCustomAttribute<XLinqNameAttribute>()
                        ?.Name,
                    $"Property '{propertyName}' of type '{type.FullName}' not found or {nameof(XLinqNameAttribute)} not found on it"
                    );
            }

            public string ValuesName(string propertyName)
            {
                return ThrowIfNull(
                    type.GetProperty(propertyName)
                        ?.GetCustomAttribute<XLinqValueAttribute>()
                        ?.ValuesName,
                    $"Property '{propertyName}' of type '{type.FullName}' not found or {nameof(XLinqValueAttribute)} not found on it"
                    );
            }

            public string KeysName(string propertyName)
            {
                return ThrowIfNull(
                    type.GetProperty(propertyName)
                        ?.GetCustomAttribute<XLinqKeysAttribute>()
                        ?.KeysName,
                    $"Property '{propertyName}' of type '{type.FullName}' not found or {nameof(XLinqKeysAttribute)} not found on it"
                    );
            }
        }
    }
}
