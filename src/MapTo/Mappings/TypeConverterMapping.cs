using MapTo.Extensions;

namespace MapTo.Mappings;

internal readonly record struct TypeConverterMapping(string ContainingType, string MethodName, string? Parameter, string? EnumerableTypeName = null)
{
    public TypeConverterMapping(IMethodSymbol method, TypedConstant? parameters)
        : this(method.ContainingType.ToDisplayString(), method.Name, parameters?.ToSourceCodeString()) { }

    [MemberNotNullWhen(true, nameof(Parameter))]
    public bool HasParameter => Parameter is not null;

    [MemberNotNullWhen(true, nameof(EnumerableTypeName))]
    public bool IsEnumerable => EnumerableTypeName is not null;
}