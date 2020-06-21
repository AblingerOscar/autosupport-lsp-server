using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using static autosupport_lsp_server.Parsing.RuleStateValueStoreKey;

namespace autosupport_lsp_server.Parsing
{
    internal class RuleStateValueStore: IEnumerable<KeyValuePair<IRuleStateValueStoreKey, object>>, IEnumerable
    {
        private readonly IDictionary<IRuleStateValueStoreKey, object> values = new Dictionary<IRuleStateValueStoreKey, object>();

        public RuleStateValueStore() { }

        public RuleStateValueStore(RuleStateValueStore valueStore)
        {
            values = new Dictionary<IRuleStateValueStoreKey, object>(valueStore.values);
        }

        public int Count => values.Count;

        public void Add(RuleStateValueStoreKey<NoValue> key)
        {
            values.Add(key, NoValue.Instance);
        }

        public void Add<T>(RuleStateValueStoreKey<T> key, T value) where T : class
        {
            values.Add(key, value);
        }

        public void Clear()
        {
            values.Clear();
        }

        public bool ContainsKey(IRuleStateValueStoreKey key)
        {
            return values.ContainsKey(key);
        }

        public T Get<T>(RuleStateValueStoreKey<T> key)
        {
            if (!values.TryGetValue(key, out object? value))
                throw new IndexOutOfRangeException();

            if (!(value is T castValue))
                throw new ArgumentException($"Value was not of type {typeof(T)}");

            return castValue;
        }

        public bool TryGetValue<T>(RuleStateValueStoreKey<T> key, [MaybeNullWhen(false)] out T value)
        {
            value = default;

            if (!values.TryGetValue(key, out object? obj) || !(obj is T castObj))
                return false;

            value = castObj;
            return true;
        }

        public IEnumerator<KeyValuePair<IRuleStateValueStoreKey, object>> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        public bool Remove(IRuleStateValueStoreKey key)
        {
            return values.Remove(key);
        }

        public bool Remove(KeyValuePair<IRuleStateValueStoreKey, object> item)
        {
            return values.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)values).GetEnumerator();
        }
    }

    internal interface IRuleStateValueStoreKey { }

    internal static class RuleStateValueStoreKey {
        public struct NoValue {
            public static NoValue Instance = new NoValue();
        }

#pragma warning disable CS0618 // Type or member is obsolete
        public static readonly RuleStateValueStoreKey<string> NextType = new RuleStateValueStoreKey<string>(0, nameof(NextType));
        public static readonly RuleStateValueStoreKey<string> NextKind = new RuleStateValueStoreKey<string>(1, nameof(NextKind));
        public static readonly RuleStateValueStoreKey<NoValue> IsDeclaration = new RuleStateValueStoreKey<NoValue>(2, nameof(IsDeclaration));
        public static readonly RuleStateValueStoreKey<NoValue> IsImplementation = new RuleStateValueStoreKey<NoValue>(3, nameof(IsImplementation));
#pragma warning restore CS0618 // Type or member is obsolete
    }

    internal readonly struct RuleStateValueStoreKey<T>: IRuleStateValueStoreKey
    {
        private readonly byte id;
        private readonly string name;

        [Obsolete("only use the static methods of " + nameof(RuleStateValueStoreKey))]
        internal RuleStateValueStoreKey(byte id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public override bool Equals(object? obj) => obj is RuleStateValueStoreKey<T> that && id == that.id;

        public override int GetHashCode() => id;

        public override string? ToString() => name;
    }
}
