using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.CodeAnalysis;

internal static class CompilationExtensions
{
    public static INamedTypeSymbol GetTypeByMetadataNameOrThrow(this Compilation compilation, string fullyQualifiedMetadataName) =>
        compilation.GetTypeByMetadataName(fullyQualifiedMetadataName) ?? throw new TypeLoadException($"Unable to find '{fullyQualifiedMetadataName}' type.");

    public static INamedTypeSymbol GetTypeByMetadataNameOrThrow<T>(this Compilation compilation) =>
        compilation.GetTypeByMetadataNameOrThrow(typeof(T).FullName!);

    public static bool HasCompatibleTypes(this Compilation compilation, ISymbol source, ISymbol destination)
    {
        return source.TryGetTypeSymbol(out var sourceType) && destination.TryGetTypeSymbol(out var destinationType) &&
               (SymbolEqualityComparer.Default.Equals(destinationType, sourceType) || compilation.HasImplicitConversion(sourceType, destinationType));
    }

    public static bool IsGenericEnumerable(this Compilation compilation, ITypeSymbol typeSymbol) =>
        typeSymbol is INamedTypeSymbol { IsGenericType: true } &&
        compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Equals(typeSymbol.OriginalDefinition, SymbolEqualityComparer.Default);

    public static bool IsNonGenericEnumerable(this Compilation compilation, ITypeSymbol typeSymbol) =>
        typeSymbol is INamedTypeSymbol { IsGenericType: false } &&
        !SymbolEqualityComparer.Default.Equals(compilation.GetSpecialType(SpecialType.System_String), typeSymbol) &&
        compilation.HasImplicitConversion(typeSymbol, compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable));

    public static bool IsEnumerableType(this Compilation compilation, ITypeSymbol symbol, out ImmutableArray<ITypeSymbol> typeArguments)
    {
        if (symbol is IArrayTypeSymbol arrayTypeSymbol)
        {
            typeArguments = ImmutableArray.Create(arrayTypeSymbol.ElementType);
            return true;
        }

        if (symbol is not INamedTypeSymbol namedTypeSymbol)
        {
            typeArguments = ImmutableArray<ITypeSymbol>.Empty;
            return false;
        }

        var spanSymbol = compilation.GetTypeByMetadataName(KnownTypes.SystemSpanOfT);
        var readOnlySpanSymbol = compilation.GetTypeByMetadataName(KnownTypes.SystemReadOnlySpanOfT);
        var memorySymbol = compilation.GetTypeByMetadataName(KnownTypes.SystemMemoryOfT);
        var readOnlyMemorySymbol = compilation.GetTypeByMetadataName(KnownTypes.SystemReadOnlyMemoryOfT);
        var immutableArraySymbol = compilation.GetTypeByMetadataName(KnownTypes.SystemCollectionImmutableArrayOfT);

        var originalDefinition = namedTypeSymbol.OriginalDefinition;
        typeArguments = namedTypeSymbol.TypeArguments;

        return (namedTypeSymbol.IsGenericCollectionOf(SpecialType.System_Collections_Generic_IEnumerable_T)
                || SymbolEqualityComparer.Default.Equals(originalDefinition, spanSymbol)
                || SymbolEqualityComparer.Default.Equals(originalDefinition, readOnlySpanSymbol)
                || SymbolEqualityComparer.Default.Equals(originalDefinition, memorySymbol)
                || SymbolEqualityComparer.Default.Equals(originalDefinition, readOnlyMemorySymbol)
                || SymbolEqualityComparer.Default.Equals(originalDefinition, immutableArraySymbol)) &&
               namedTypeSymbol.TypeArguments.Length > 0;
    }

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

    public static IMethodSymbol? GetMethodSymbolByFullyQualifiedName(this Compilation? compilation, ReadOnlySpan<char> methodFullyQualifiedName)
    {
        if (compilation is null || methodFullyQualifiedName.IsEmpty)
        {
            return null;
        }

        var start = 0;
        var end = methodFullyQualifiedName.Length;

        if (methodFullyQualifiedName.StartsWith("nameof".AsSpan()))
        {
            // Extract the value of the nameof expression
            start = 7;
            end = methodFullyQualifiedName.Length - 8;
        }

        var typeMetadataName = methodFullyQualifiedName.Slice(start, methodFullyQualifiedName.LastIndexOf('.') - start);
        var methodName = methodFullyQualifiedName.Slice(typeMetadataName.Length + start + 1, end - typeMetadataName.Length - 1);
        var typeSymbol = compilation.GetTypeByMetadataName(typeMetadataName.ToString());

        return typeSymbol?.GetMembers(methodName.ToString()).OfType<IMethodSymbol>().SingleOrDefault();
    }

    public static IMethodSymbol? GetMethodSymbol(this Compilation compilation, InvocationExpressionSyntax expressionSyntax)
    {
        var identifier = expressionSyntax.ArgumentList.Arguments.FirstOrDefault()?.Expression;
        if (identifier is null)
        {
            return null;
        }

        var semanticModel = compilation.GetSemanticModel(expressionSyntax.SyntaxTree);
        var symbolInfo = semanticModel.GetSymbolInfo(identifier);
        if (symbolInfo.CandidateReason is CandidateReason.MemberGroup)
        {
            return symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
        }

        return null;
    }

    public static ITypeSymbol CreateGenericTypeSymbol(
        this Compilation compilation,
        SpecialType specialType,
        ITypeSymbol elementTypeSymbol,
        NullableAnnotation nullableAnnotation = NullableAnnotation.None,
        NullableAnnotation elementTypeNullableAnnotation = NullableAnnotation.None)
    {
        var type = compilation.GetSpecialType(specialType);
        return type.Construct(elementTypeSymbol.WithNullableAnnotation(elementTypeNullableAnnotation)).WithNullableAnnotation(nullableAnnotation);
    }

    public static ITypeSymbol CreateGenericTypeSymbol(
        this Compilation compilation,
        string fullyQualifiedMetadataName,
        ITypeSymbol elementTypeSymbol,
        NullableAnnotation nullableAnnotation = NullableAnnotation.None,
        NullableAnnotation elementTypeNullableAnnotation = NullableAnnotation.None)
    {
        var type = compilation.GetTypeByMetadataName(fullyQualifiedMetadataName) ?? throw new TypeLoadException($"Unable to find '{fullyQualifiedMetadataName}' type.");
        return type.Construct(elementTypeSymbol.WithNullableAnnotation(elementTypeNullableAnnotation)).WithNullableAnnotation(nullableAnnotation);
    }
}