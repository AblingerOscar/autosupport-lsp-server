using System;
using System.Collections.Generic;

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
    }
}
