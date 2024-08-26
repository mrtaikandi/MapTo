namespace MapTo;

internal record KnownTypes(
    INamedTypeSymbol MapFromAttributeTypeSymbol,
    INamedTypeSymbol GenericMapFromAttributeTypeSymbol,
    INamedTypeSymbol IgnorePropertyAttributeTypeSymbol,
    INamedTypeSymbol MapPropertyAttributeTypeSymbol,
    INamedTypeSymbol MapConstructorAttributeTypeSymbol,
    INamedTypeSymbol PropertyTypeConverterAttributeTypeSymbol,
    INamedTypeSymbol CompilerGeneratedAttributeTypeSymbol,
    INamedTypeSymbol IgnoreEnumMemberAttributeTypeSymbol)
{
    internal const string FriendlyMapFromAttributeName = "MapFrom";
    internal const string NotNullIfNotNullAttributeFullName = "System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute";
    internal const string CompilerGeneratedAttributeFullName = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";
    internal const string ArgumentNullException = "System.ArgumentNullException";
    internal const string ArgumentOutOfRangeException = "System.ArgumentOutOfRangeException";
    internal const string InvalidOperationException = "System.InvalidOperationException";

    internal const string Array = "System.Array";
    internal const string GenericList = "System.Collections.Generic.List";
    internal const string LinqEnumerable = "System.Linq.Enumerable";
    internal const string LinqToArray = "System.Linq.Enumerable.ToArray";
    internal const string LinqToList = "System.Linq.Enumerable.ToList";

    internal const string SystemSpanOfT = "System.Span`1";
    internal const string SystemReadOnlySpanOfT = "System.ReadOnlySpan`1";
    internal const string SystemMemoryOfT = "System.Memory`1";
    internal const string SystemReadOnlyMemoryOfT = "System.ReadOnlyMemory`1";
    internal const string SystemCollectionsGenericListOfT = "System.Collections.Generic.List`1";
    internal const string SystemLinqIQueryableOfT = "System.Linq.IQueryable`1";
    internal const string SystemCollectionImmutableArray = "System.Collections.Immutable.ImmutableArray";
    internal const string SystemCollectionImmutableArrayOfT = "System.Collections.Immutable.ImmutableArray`1";

    internal static KnownTypes Create(Compilation compilation) => new(
        MapFromAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<MapFromAttribute>(),
        GenericMapFromAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow(typeof(MapFromAttribute<>).FullName!),
        IgnorePropertyAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<IgnorePropertyAttribute>(),
        MapPropertyAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<MapPropertyAttribute>(),
        MapConstructorAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<MapConstructorAttribute>(),
        PropertyTypeConverterAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<PropertyTypeConverterAttribute>(),
        CompilerGeneratedAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow(CompilerGeneratedAttributeFullName),
        IgnoreEnumMemberAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<IgnoreEnumMemberAttribute>());
}