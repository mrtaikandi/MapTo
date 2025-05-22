using System.Diagnostics;

namespace MapTo.Mappings;

[DebuggerDisplay($"{{{nameof(FullName)}}}")]
internal readonly record struct TypeMapping(
    string Name,
    string FullName,
    string QualifiedName,
    bool IsNullable,
    NullableAnnotation NullableAnnotation,
    bool IsArray,
    bool IsEnum,
    string ElementTypeName,
    bool ElementTypeIsPrimitive,
    EnumerableType EnumerableType,
    NullableAnnotation ElementTypeNullableAnnotation,
    bool IsCountable,
    bool IsFixedSize,
    bool IsReferenceType,
    SpecialType SpecialType,
    bool IsPrimitive,
    bool HasNonPrimitiveProperties)
{
#if DEBUG
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "Is for debugging purposes only.")]
    internal ITypeSymbol OriginalTypeSymbol { get; init; } = null!;
#endif
}

internal static class TypeMappingExtensions
{
    internal static TypeMapping ToTypeMapping(this ITypeSymbol typeSymbol, NullableAnnotation nullableAnnotation = NullableAnnotation.None)
    {
        var isArray = false;
        ITypeSymbol? elementType = null;
        var elementTypeName = string.Empty;
        var enumerableType = EnumerableType.None;

        switch (typeSymbol)
        {
            case IArrayTypeSymbol arrayTypeSymbol:
                isArray = true;
                elementType = arrayTypeSymbol.ElementType;
                elementTypeName = elementType.ToFullyQualifiedDisplayString();
                enumerableType = EnumerableType.Array;
                break;

            case INamedTypeSymbol namedTypedSymbol:
                elementType = namedTypedSymbol.TypeArguments.FirstOrDefault();
                elementTypeName = elementType?.ToFullyQualifiedDisplayString() ?? string.Empty;
                enumerableType = namedTypedSymbol.ToEnumerableType();
                break;
        }

        var mapping = new TypeMapping(
            Name: typeSymbol.Name,
            FullName: typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            QualifiedName: typeSymbol.ToDisplayString(),
            IsNullable: typeSymbol.IsReferenceType || typeSymbol.NullableAnnotation is NullableAnnotation.Annotated,
            NullableAnnotation: nullableAnnotation is NullableAnnotation.None ? typeSymbol.NullableAnnotation : nullableAnnotation,
            IsArray: isArray,
            IsEnum: typeSymbol.TypeKind == TypeKind.Enum,
            ElementTypeName: elementTypeName,
            EnumerableType: enumerableType,
            ElementTypeIsPrimitive: elementType?.IsPrimitiveType() ?? false,
            ElementTypeNullableAnnotation: elementType?.NullableAnnotation ?? NullableAnnotation.None,
            SpecialType: typeSymbol.SpecialType,
            IsCountable: enumerableType.IsCountable(),
            IsFixedSize: enumerableType.IsFixedSize(),
            IsReferenceType: typeSymbol.IsReferenceType,
            IsPrimitive: typeSymbol.IsPrimitiveType(),
            HasNonPrimitiveProperties: typeSymbol.HasNonPrimitiveProperties());

#if DEBUG
        return mapping with { OriginalTypeSymbol = typeSymbol };
#else
        return mapping;
#endif
    }

    internal static string EmptySourceCodeString(this TypeMapping type) => type switch
    {
        { EnumerableType: EnumerableType.Array or EnumerableType.ReadOnlyCollection or EnumerableType.ReadOnlyList } =>
            $"global::{KnownTypes.Array}.Empty<{type.ElementTypeName}>()",
        { EnumerableType: EnumerableType.List } => $"new global::{KnownTypes.GenericList}<{type.ElementTypeName}>()",
        { EnumerableType: EnumerableType.Enumerable } => $"global::{KnownTypes.LinqEnumerable}.Empty<{type.ElementTypeName}>()",
        { SpecialType: SpecialType.System_String, NullableAnnotation: not NullableAnnotation.Annotated } => "string.Empty",
        _ => "default"
    };

    private static EnumerableType ToEnumerableType(this ITypeSymbol typeSymbol) => typeSymbol switch
    {
        _ when typeSymbol.IsReadOnlySpanOfT() => EnumerableType.ReadOnlySpan,
        _ when typeSymbol.IsSpanOfT() => EnumerableType.Span,
        _ when typeSymbol.IsReadOnlyMemoryOfT() => EnumerableType.ReadOnlyMemory,
        _ when typeSymbol.IsMemoryOfT() => EnumerableType.Memory,
        _ when typeSymbol.IsImmutableArrayOfT() => EnumerableType.ImmutableArray,
        _ when typeSymbol.IsGenericCollectionOf(SpecialType.System_Collections_Generic_IList_T) => EnumerableType.List,
        _ when typeSymbol.IsGenericCollectionOf(SpecialType.System_Collections_Generic_ICollection_T) => EnumerableType.Collection,
        _ when typeSymbol.IsGenericCollectionOf(SpecialType.System_Collections_Generic_IReadOnlyList_T) => EnumerableType.ReadOnlyList,
        _ when typeSymbol.IsGenericCollectionOf(SpecialType.System_Collections_Generic_IReadOnlyCollection_T) => EnumerableType.ReadOnlyCollection,
        _ when typeSymbol.IsGenericCollectionOf(SpecialType.System_Collections_Generic_IEnumerable_T) => EnumerableType.Enumerable,
        _ => EnumerableType.None
    };
}