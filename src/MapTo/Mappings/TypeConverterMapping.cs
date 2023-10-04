using MapTo.Extensions;

namespace MapTo.Mappings;

internal readonly record struct TypeConverterMapping(
    string ContainingType,
    string MethodName,
    string? Parameter,
    TypeMapping Type,
    bool IsMapToExtensionMethod = false,
    bool ReferenceHandling = false,
    ImmutableArray<string>? UsingDirectives = null)
{
    public TypeConverterMapping(IMethodSymbol method, TypedConstant? parameters)
        : this(
            ContainingType: method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            MethodName: method.Name,
            Parameter: parameters?.ToSourceCodeString(),
            Type: method.ReturnType.ToTypeMapping()) { }

    [MemberNotNullWhen(true, nameof(Parameter))]
    public bool HasParameter => Parameter is not null;

    public string MethodFullName => $"{ContainingType}.{MethodName}";
}