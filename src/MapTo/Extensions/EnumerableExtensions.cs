using System;
using System.Collections.Generic;
using System.Linq;

namespace MapTo.Extensions
{
    internal static class EnumerableExtensions
    {
        internal static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
            {
                action(item);
            }
        }

        internal static bool IsEmpty<T>(this IEnumerable<T> enumerable) => !enumerable.Any();
    }
}