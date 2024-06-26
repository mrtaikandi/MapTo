﻿using MapTo.Extensions;

namespace MapTo.Mappings;

internal readonly record struct TypeConverterMapping(
    string ContainingType,
    string MethodName,
    TypeMapping Type,
    bool Explicit,
    string? Parameter = null,
    bool IsMapToExtensionMethod = false,
    bool ReferenceHandling = false,
    ImmutableArray<string>? UsingDirectives = null,
    EnumTypeMapping EnumMapping = default)
{
    public TypeConverterMapping(IMethodSymbol method, TypedConstant parameters)
        : this(
            ContainingType: method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            MethodName: method.Name,
            Parameter: parameters.IsNull ? null : parameters.ToSourceCodeString(),
            Type: method.ReturnType.ToTypeMapping(),
            Explicit: true) { }

    [MemberNotNullWhen(true, nameof(Parameter))]
    public bool HasParameter => Parameter is not null;

    public string MethodFullName => string.IsNullOrEmpty(ContainingType) ? MethodName : $"{ContainingType}.{MethodName}";
}