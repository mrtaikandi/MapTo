namespace MapTo;

internal record KnownTypes(
    INamedTypeSymbol MapFromAttributeTypeSymbol,
    INamedTypeSymbol IgnorePropertyAttributeTypeSymbol,
    INamedTypeSymbol MapPropertyAttributeTypeSymbol,
    INamedTypeSymbol MapConstructorAttributeTypeSymbol,
    INamedTypeSymbol PropertyTypeConverterAttributeTypeSymbol,
    INamedTypeSymbol CompilerGeneratedAttributeTypeSymbol)
{
    internal const string MapToNamespace = "MapTo";

    internal const string MapFromAttributeName = "MapFromAttribute";
    internal const string MapFromAttributeFullyQualifiedName = $"{MapToNamespace}.MapFromAttribute";
    internal const string MapFromReferenceHandlingPropertyName = "ReferenceHandling";
    internal const string MapFromCopyPrimitiveArraysPropertyName = "CopyPrimitiveArrays";

    internal const string IgnorePropertyAttributeName = "IgnorePropertyAttribute";
    internal const string IgnorePropertyAttributeFullyQualifiedName = $"{MapToNamespace}.{IgnorePropertyAttributeName}";

    internal const string MapPropertyAttributeName = "MapPropertyAttribute";
    internal const string MapPropertyAttributeFullyQualifiedName = $"{MapToNamespace}.{MapPropertyAttributeName}";
    internal const string MapPropertyAttributeSourcePropertyName = "SourcePropertyName";

    internal const string MapConstructorAttributeName = "MapConstructorAttribute";
    internal const string MapConstructorAttributeFullyQualifiedName = $"{MapToNamespace}.{MapConstructorAttributeName}";

    internal const string NotNullIfNotNullAttribute = "System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute";

    internal const string PropertyTypeConverterAttributeName = "PropertyTypeConverterAttribute";
    internal const string PropertyTypeConverterAttributeFullyQualifiedName = $"{MapToNamespace}.{PropertyTypeConverterAttributeName}";
    internal const string PropertyTypeConverterAttributeAdditionalParameters = "Parameters";

    internal static KnownTypes Create(Compilation compilation) => new(
        compilation.GetTypeByMetadataNameOrThrow(MapFromAttributeFullyQualifiedName),
        compilation.GetTypeByMetadataNameOrThrow(IgnorePropertyAttributeFullyQualifiedName),
        compilation.GetTypeByMetadataNameOrThrow(MapPropertyAttributeFullyQualifiedName),
        compilation.GetTypeByMetadataNameOrThrow(MapConstructorAttributeFullyQualifiedName),
        compilation.GetTypeByMetadataNameOrThrow(PropertyTypeConverterAttributeFullyQualifiedName),
        compilation.GetTypeByMetadataNameOrThrow("System.Runtime.CompilerServices.CompilerGeneratedAttribute"));
}