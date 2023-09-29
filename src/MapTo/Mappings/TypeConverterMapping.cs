using MapTo.Extensions;

namespace MapTo.Mappings;

internal enum EnumerableType
{
    None,
    Array,
    List
}

internal readonly record struct TypeConverterMapping(
    string ContainingType,
    string MethodName,
    string? Parameter,
    string? EnumerableElementTypeName = null,
    EnumerableType EnumerableType = EnumerableType.None,
    bool IsMapToExtensionMethod = false,
    bool ReferenceHandling = false,
    ImmutableArray<string>? UsingDirectives = null)
{
    public TypeConverterMapping(IMethodSymbol method, TypedConstant? parameters)
        : this(method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), method.Name, parameters?.ToSourceCodeString()) { }

    [MemberNotNullWhen(true, nameof(Parameter))]
    public bool HasParameter => Parameter is not null;

    [MemberNotNullWhen(true, nameof(EnumerableElementTypeName))]
    public bool IsEnumerable => EnumerableElementTypeName is not null;

    public string MethodFullName => $"{ContainingType}.{MethodName}";
}