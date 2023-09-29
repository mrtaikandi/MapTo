namespace MapTo.Extensions;

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

    internal static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable)
        where T : struct => enumerable.Where(item => item is not null).Select(item => item!.Value);
}