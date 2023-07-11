namespace MapTo;

internal record WellKnownTypes(INamedTypeSymbol MapFromAttributeTypeSymbol,
    INamedTypeSymbol IgnorePropertyAttributeTypeSymbol,
    INamedTypeSymbol MapPropertyAttributeTypeSymbol,
    INamedTypeSymbol MapConstructorAttributeTypeSymbol)
{
    internal const string MapToNamespace = "MapTo";
    internal const string MapFromAttributeName = "MapFromAttribute";
    internal const string MapFromAttributeFullyQualifiedName = $"{MapToNamespace}.MapFromAttribute";

    internal const string IgnorePropertyAttributeFullyQualifiedName = $"{MapToNamespace}.IgnorePropertyAttribute";

    internal const string MapPropertyAttributeFullyQualifiedName = $"{MapToNamespace}.MapPropertyAttribute";
    internal const string MapPropertyAttributeSourcePropertyName = "SourcePropertyName";

    internal const string MapConstructorAttributeFullyQualifiedName = $"{MapToNamespace}.MapConstructorAttribute";

    internal const string NotNullIfNotNullAttribute = "System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute";

    internal static WellKnownTypes Create(Compilation compilation) => new(
        compilation.GetTypeByMetadataNameOrThrow(MapFromAttributeFullyQualifiedName),
        compilation.GetTypeByMetadataNameOrThrow(IgnorePropertyAttributeFullyQualifiedName),
        compilation.GetTypeByMetadataNameOrThrow(MapPropertyAttributeFullyQualifiedName),
        compilation.GetTypeByMetadataNameOrThrow(MapConstructorAttributeFullyQualifiedName));
}