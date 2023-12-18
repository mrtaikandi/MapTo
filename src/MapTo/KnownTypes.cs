namespace MapTo;

internal record KnownTypes(
    INamedTypeSymbol MapFromAttributeTypeSymbol,
    INamedTypeSymbol IgnorePropertyAttributeTypeSymbol,
    INamedTypeSymbol MapPropertyAttributeTypeSymbol,
    INamedTypeSymbol MapConstructorAttributeTypeSymbol,
    INamedTypeSymbol PropertyTypeConverterAttributeTypeSymbol,
    INamedTypeSymbol CompilerGeneratedAttributeTypeSymbol,
    INamedTypeSymbol IgnoreEnumMemberAttributeTypeSymbol)
{
    internal const string NotNullIfNotNullAttributeFullName = "System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute";
    internal const string CompilerGeneratedAttributeFullName = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";
    internal const string ArgumentNullException = "System.ArgumentNullException";
    internal const string ArgumentOutOfRangeException = "System.ArgumentOutOfRangeException";

    internal const string Array = "System.Array";
    internal const string GenericIEnumerable = "System.Collections.Generic.IEnumerable";
    internal const string GenericICollection = "System.Collections.Generic.ICollection";
    internal const string ObjectModelCollection = "System.Collections.ObjectModel.Collection";
    internal const string GenericIReadOnlyCollection = "System.Collections.Generic.IReadOnlyCollection";
    internal const string GenericIList = "System.Collections.Generic.IList";
    internal const string GenericIReadOnlyList = "System.Collections.Generic.IReadOnlyList";
    internal const string GenericList = "System.Collections.Generic.List";
    internal const string LinqEnumerable = "System.Linq.Enumerable";
    internal const string LinqIQueryable = "System.Linq.IQueryable";
    internal const string SystemSpan = "System.Span";
    internal const string SystemMemory = "System.Memory";
    internal const string SystemReadOnlySpan = "System.ReadOnlySpan";
    internal const string SystemReadOnlyMemory = "System.ReadOnlyMemory";

    internal static KnownTypes Create(Compilation compilation) => new(
        MapFromAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<MapFromAttribute>(),
        IgnorePropertyAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<IgnorePropertyAttribute>(),
        MapPropertyAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<MapPropertyAttribute>(),
        MapConstructorAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<MapConstructorAttribute>(),
        PropertyTypeConverterAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<PropertyTypeConverterAttribute>(),
        CompilerGeneratedAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow(CompilerGeneratedAttributeFullName),
        IgnoreEnumMemberAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<IgnoreEnumMemberAttribute>());
}