namespace MapTo.Mappings;

internal enum EnumerableType
{
    None,
    Array,
    List,
    Enumerable,
    ReadOnlyCollection
}

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Reviewed.")]
internal static class EnumerableTypeExtensions
{
    internal static string ToLinqSourceCodeString(this EnumerableType enumerableType) => enumerableType switch
    {
        EnumerableType.Array => "ToArray()",
        EnumerableType.List => "ToList()",
        EnumerableType.Enumerable => "ToArray()",
        EnumerableType.ReadOnlyCollection => "ToArray()",
        _ => "ToList()"
    };
}