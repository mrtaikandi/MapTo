namespace MapTo;

internal record KnownTypes(
    INamedTypeSymbol MapFromAttributeTypeSymbol,
    INamedTypeSymbol IgnorePropertyAttributeTypeSymbol,
    INamedTypeSymbol MapPropertyAttributeTypeSymbol,
    INamedTypeSymbol MapConstructorAttributeTypeSymbol,
    INamedTypeSymbol PropertyTypeConverterAttributeTypeSymbol,
    INamedTypeSymbol CompilerGeneratedAttributeTypeSymbol)
{
    internal const string NotNullIfNotNullAttributeFullName = "System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute";
    internal const string CompilerGeneratedAttributeFullName = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";
    internal const string ArgumentNullException = "System.ArgumentNullException";
    internal const string GenericList = "System.Collections.Generic.List";
    internal const string Array = "System.Array";
    internal const string LinqEnumerable = "System.Linq.Enumerable";

    internal static KnownTypes Create(Compilation compilation) => new(
        MapFromAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<MapFromAttribute>(),
        IgnorePropertyAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<IgnorePropertyAttribute>(),
        MapPropertyAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<MapPropertyAttribute>(),
        MapConstructorAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<MapConstructorAttribute>(),
        PropertyTypeConverterAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<PropertyTypeConverterAttribute>(),
        CompilerGeneratedAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow(CompilerGeneratedAttributeFullName));
}