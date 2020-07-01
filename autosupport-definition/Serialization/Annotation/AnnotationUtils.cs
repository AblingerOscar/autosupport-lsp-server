using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace autosupport_lsp_server.Serialization.Annotation
{
    internal static class AnnotationUtils
    {
        internal static XLinqClassAnnotationUtil XLinqOf(Type type)
        {
            return new XLinqClassAnnotationUtil(type);
        }

        private static readonly IDictionary<string, Type> annotatedTypes =
                (from type in Assembly.GetExecutingAssembly().GetTypes()
                 where type.IsClass
                 select new Tuple<XLinqNameAttribute?, Type>(type.GetCustomAttribute<XLinqNameAttribute>(true), type))
                 .Where(tuple => tuple.Item1 != null && tuple.Item2 != null)
                .ToDictionary(tuple => tuple.Item1.Name, tuple => tuple.Item2);

        internal static Type? FindTypeWithName(string annotationName)
        {
            return annotatedTypes.ContainsKey(annotationName)
                ? annotatedTypes[annotationName]
                : null;
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

            internal string ClassName()
            {
                return ThrowIfNull(
                    type.GetCustomAttribute<XLinqNameAttribute>()
                        ?.Name,
                    $"{nameof(XLinqValueAttribute)} not found on '{type.FullName}'");

            }

            internal string ClassName(Type clazz)
            {
                return ThrowIfNull(
                    clazz.GetCustomAttribute<XLinqNameAttribute>()
                        ?.Name,
                    $"{nameof(XLinqValueAttribute)} not found on '{clazz.FullName}'");

            }

            internal string RuntimeClassName(Type t)
            {
                return ThrowIfNull(
                    t.GetCustomAttribute<XLinqNameAttribute>()
                        ?.Name,
                    $"{nameof(XLinqValueAttribute)} not found on '{t.FullName}'");
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
