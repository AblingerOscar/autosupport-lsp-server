using autosupport_lsp_server.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace autosupport_lsp_server
{
    public static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> list, Action<T> action)
        {
            foreach (T item in list)
            {
                action.Invoke(item);
            }
        }

        public static string JoinToString<T>(this IEnumerable<T> list, string separator = "\n")
        {
            using var enumerator = list.GetEnumerator();
            var sb = new StringBuilder();

            if (enumerator.MoveNext())
                sb.Append(enumerator.Current);

            while (enumerator.MoveNext())
                sb.Append(separator).Append(enumerator.Current);

            return sb.ToString();
        }

        // efficient cloning taken from https://stackoverflow.com/a/45200965
        public static Stack<T> Clone<T>(this Stack<T> original)
        {
            var arr = new T[original.Count];
            original.CopyTo(arr, 0);
            Array.Reverse(arr);
            return new Stack<T>(arr);
        }

        public static TAccumulate AggregateWhile<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func, Func<TAccumulate, TSource, bool> predicate)
        {
            foreach (var item in source)
            {
                if (!predicate.Invoke(seed, item))
                    return seed;
                else
                    seed = func.Invoke(seed, item);
            }
            return seed;
        }

        public static IEnumerable<T> Distinct<T, K>(this IEnumerable<T> source, Func<T, K> selector, IEqualityComparer<K>? equalityComparer = null)
        {
            var passedKeys = new HashSet<K>(equalityComparer);

            foreach (var item in source)
            {
                if (passedKeys.Add(selector.Invoke(item)))
                    yield return item;
            }
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
        {
            foreach (var item in source)
            {
                if (item != null)
                    yield return item;
            }
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : struct
        {
            foreach (var item in source)
            {
                if (item.HasValue)
                    yield return item.Value;
            }
        }

        public static StringBuilder If(this StringBuilder sb, Either<Func<bool>, bool> condition, Action<StringBuilder> then, Action<StringBuilder>? @else = null)
        {
            if (condition.Match(f => f.Invoke(), b => b))
                then.Invoke(sb);
            else
                @else?.Invoke(sb);

            return sb;
        }
    }
}
