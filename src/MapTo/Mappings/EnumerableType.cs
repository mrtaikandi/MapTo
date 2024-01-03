namespace MapTo.Mappings;

internal enum EnumerableType
{
    None,
    Array,
    Enumerable,
    List,
    ReadOnlyList,
    Collection,
    ReadOnlyCollection,
    Queryable,
    Span,
    ReadOnlySpan,
    Memory,
    ReadOnlyMemory,
    ImmutableArray,
}

internal static class EnumerableTypeExtensions
{
    internal static bool IsCountable(this EnumerableType enumerableType) =>
        enumerableType is not EnumerableType.None and not EnumerableType.Enumerable and not EnumerableType.Queryable;

    internal static bool IsFixedSize(this EnumerableType enumerableType) => enumerableType is
        EnumerableType.Array or EnumerableType.Span or EnumerableType.Memory or EnumerableType.ImmutableArray or
        EnumerableType.ReadOnlySpan or EnumerableType.ReadOnlyMemory;

    internal static bool IsImmutable(this EnumerableType enumerableType) => enumerableType is EnumerableType.ImmutableArray;

    internal static bool HasIndexer(this EnumerableType enumerableType) => enumerableType is
        EnumerableType.Array or EnumerableType.Span or EnumerableType.List or EnumerableType.ReadOnlyList or
        EnumerableType.ImmutableArray or EnumerableType.Memory or EnumerableType.ReadOnlyMemory or EnumerableType.ReadOnlySpan;

    internal static bool IsQueryable(this EnumerableType enumerableType) => enumerableType is EnumerableType.Queryable;

    internal static string ToLinqSourceCodeString(this EnumerableType enumerableType) => enumerableType switch
    {
        EnumerableType.Array => "ToArray()",
        EnumerableType.List => "ToList()",
        EnumerableType.Enumerable => "ToArray()",
        EnumerableType.ReadOnlyCollection => "ToArray()",
        EnumerableType.Span => "AsSpan()",
        EnumerableType.ReadOnlySpan => "AsSpan()",
        EnumerableType.Memory => "AsMemory()",
        EnumerableType.ReadOnlyMemory => "AsMemory()",
        EnumerableType.ImmutableArray => "ToImmutableArray()",
        EnumerableType.Queryable => "AsQueryable()",
        _ => "ToList()"
    };
}