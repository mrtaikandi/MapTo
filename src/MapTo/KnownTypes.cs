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

    internal static KnownTypes Create(Compilation compilation) => new(
        MapFromAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<MapFromAttribute>(),
        IgnorePropertyAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<IgnorePropertyAttribute>(),
        MapPropertyAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<MapPropertyAttribute>(),
        MapConstructorAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<MapConstructorAttribute>(),
        PropertyTypeConverterAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow<PropertyTypeConverterAttribute>(),
        CompilerGeneratedAttributeTypeSymbol: compilation.GetTypeByMetadataNameOrThrow(CompilerGeneratedAttributeFullName));
}