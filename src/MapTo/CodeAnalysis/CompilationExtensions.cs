namespace MapTo.CodeAnalysis;

internal static class CompilationExtensions
{
    public static INamedTypeSymbol GetTypeByMetadataNameOrThrow(this Compilation compilation, string fullyQualifiedMetadataName) =>
        compilation.GetTypeByMetadataName(fullyQualifiedMetadataName) ?? throw new TypeLoadException($"Unable to find '{fullyQualifiedMetadataName}' type.");

    public static bool HasCompatibleTypes(this Compilation compilation, ISymbol source, ISymbol destination)
    {
        return source.TryGetTypeSymbol(out var sourceType) && destination.TryGetTypeSymbol(out var destinationType) &&
               (SymbolEqualityComparer.Default.Equals(destinationType, sourceType) || compilation.HasImplicitConversion(sourceType, destinationType));
    }

    public static bool IsGenericEnumerable(this Compilation compilation, ITypeSymbol typeSymbol) =>
        typeSymbol is INamedTypeSymbol { IsGenericType: true } &&
        compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Equals(typeSymbol.OriginalDefinition, SymbolEqualityComparer.Default);

    public static bool TypeByMetadataNameExists(this Compilation? compilation, string typeMetadataName) => GetTypesByMetadataName(compilation, typeMetadataName).Any();

    public static IEnumerable<INamedTypeSymbol> GetTypesByMetadataName(this Compilation? compilation, string typeMetadataName)
    {
        if (compilation is null)
        {
            return Enumerable.Empty<INamedTypeSymbol>();
        }

        return compilation.References
            .Select(compilation.GetAssemblyOrModuleSymbol)
            .OfType<IAssemblySymbol>()
            .Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName(typeMetadataName))
            .Where(t => t != null)!;
    }
}