using System;
using System.Collections.Generic;
using System.Linq;

namespace Curly.Helpers
{
    public static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action)
        {
            using (IEnumerator<T> enumerator = ie.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    action(enumerator.Current);
                }
            }
        }
        public static object ToArrayOfType<T>(this IEnumerable<T> enumerable, Type type)
        {
            T[] res = enumerable.ToArray();
            var arr = Array.CreateInstance(type, res.Length);
            Array.Copy(res, arr, res.Length);
            return arr;
        }
        public static IEnumerable<string> NotNullOrEmpty(this IEnumerable<string> other)
        {
            return other.Where(a => !string.IsNullOrWhiteSpace(a));
        }

        public static IEnumerable<T> NotNull<T>(this IEnumerable<T> other)
        {
            return other.Where(a => a != null);
        }
    }
}
