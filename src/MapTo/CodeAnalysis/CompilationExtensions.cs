namespace MapTo.CodeAnalysis;

internal static class CompilationExtensions
{
    public static INamedTypeSymbol GetTypeByMetadataNameOrThrow(this Compilation compilation, string fullyQualifiedMetadataName) =>
        compilation.GetTypeByMetadataName(fullyQualifiedMetadataName) ?? throw new TypeLoadException($"Unable to find '{fullyQualifiedMetadataName}' type.");

    public static bool HasCompatibleTypes(this Compilation compilation, ISymbol source, ISymbol destination) =>
        source.TryGetTypeSymbol(out var sourceType) && destination.TryGetTypeSymbol(out var destinationType) &&
        (SymbolEqualityComparer.Default.Equals(destinationType, sourceType) || compilation.HasImplicitConversion(sourceType, destinationType));

    public static bool IsGenericEnumerable(this Compilation compilation, ITypeSymbol typeSymbol) =>
        typeSymbol is INamedTypeSymbol { IsGenericType: true } &&
        compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Equals(typeSymbol.OriginalDefinition, SymbolEqualityComparer.Default);
}