namespace MapTo.CodeAnalysis;

internal static class SymbolExtensions
{
    public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol type, bool includeBaseTypeMembers = true)
    {
        return includeBaseTypeMembers
            ? type.GetBaseTypesAndThis().SelectMany(t => t.GetMembers())
            : type.GetMembers();
    }

    public static AttributeData? GetAttribute(this ISymbol symbol, ITypeSymbol attributeSymbol) =>
        symbol.GetAttributes(attributeSymbol).FirstOrDefault();

    public static AttributeData? GetAttribute<T>(this ISymbol symbol)
        where T : Attribute => symbol.GetAttribute(typeof(T).FullName!);

    public static AttributeData? GetAttribute(this ISymbol symbol, string fullyQualifiedAttributeName) =>
        symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == fullyQualifiedAttributeName);

    public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, ITypeSymbol attributeSymbol) =>
        symbol.GetAttributes().Where(a => a.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) == true);

    public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
    {
        var current = type;
        while (current != null)
        {
            yield return current;

            current = current.BaseType;
        }
    }

    public static ITypeSymbol? GetTypeSymbol(this ISymbol symbol) => symbol.TryGetTypeSymbol(out var typeSymbol) ? typeSymbol : null;

    public static bool HasAttribute(this ISymbol? symbol, ITypeSymbol attributeSymbol) =>
        symbol?.GetAttributes().Any(a => a.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) == true) == true;

    public static bool IsPrimitiveType(this ITypeSymbol type, bool includeNullable)
    {
        if (type.IsPrimitiveType())
        {
            return true;
        }

        if (includeNullable && type is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } namedTypeSymbol)
        {
            var underlyingType = namedTypeSymbol.TypeArguments[0];
            return underlyingType.IsPrimitiveType();
        }

        return false;
    }

    public static bool IsPrimitiveType(this ITypeSymbol type) => type.SpecialType is
        SpecialType.System_String or
        SpecialType.System_Boolean or
        SpecialType.System_SByte or
        SpecialType.System_Int16 or
        SpecialType.System_Int32 or
        SpecialType.System_Int64 or
        SpecialType.System_Byte or
        SpecialType.System_UInt16 or
        SpecialType.System_UInt32 or
        SpecialType.System_UInt64 or
        SpecialType.System_Single or
        SpecialType.System_Double or
        SpecialType.System_Char or
        SpecialType.System_Object;

    public static bool IsArrayOf(this ITypeSymbol type, SpecialType elementType) =>
        type is IArrayTypeSymbol arrayTypeSymbol && arrayTypeSymbol.ElementType.SpecialType == elementType;

    public static bool IsArray(this ITypeSymbol type) => type is IArrayTypeSymbol;

    public static bool TryGetTypeSymbol(this ISymbol symbol, [NotNullWhen(true)] out ITypeSymbol? typeSymbol)
    {
        switch (symbol)
        {
            case IPropertySymbol propertySymbol:
                typeSymbol = propertySymbol.Type;
                return true;

            case IParameterSymbol parameterSymbol:
                typeSymbol = parameterSymbol.Type;
                return true;

            case ITypeSymbol type:
                typeSymbol = type;
                return true;

            default:
                typeSymbol = null;
                return false;
        }
    }

    public static INamedTypeSymbol GetTypeNamedSymbol(this IPropertySymbol symbol)
    {
        var namedTypeSymbol = symbol.Type switch
        {
            IArrayTypeSymbol arrayTypeSymbol => arrayTypeSymbol.ElementType,
            _ => symbol.Type
        };

        return namedTypeSymbol as INamedTypeSymbol ?? throw new InvalidOperationException($"Unable to get type symbol for property '{symbol.Name}'.");
    }

    public static bool HasNonPrimitiveProperties(this ITypeSymbol typeSymbol) =>
        !typeSymbol.IsPrimitiveType() && typeSymbol.GetAllMembers().OfType<IPropertySymbol>().Any(p => !p.Type.IsPrimitiveType(true));

    public static Location? GetLocation(this ISymbol? symbol) =>
        symbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation();

    public static bool IsGenericCollectionOf(this ITypeSymbol typeSymbol, SpecialType type)
    {
        if (typeSymbol is not INamedTypeSymbol { IsGenericType: true } namedTypeSymbol)
        {
            return false;
        }

        if (namedTypeSymbol.SpecialType == type || namedTypeSymbol.ConstructedFrom.SpecialType == type)
        {
            return true;
        }

        var genericType = namedTypeSymbol.ConstructedFrom;
        return genericType.Interfaces.Any(i => i.IsGenericType && i.ConstructedFrom.SpecialType == type);
    }

    public static bool IsSpanOfT(this ITypeSymbol typeSymbol) =>
        typeSymbol is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol &&
        namedTypeSymbol.ContainingNamespace.ToDisplayString() == "System" &&
        namedTypeSymbol.MetadataName == "Span`1";

    public static bool IsReadOnlySpanOfT(this ITypeSymbol typeSymbol) =>
        typeSymbol is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol &&
        namedTypeSymbol.ContainingNamespace.ToDisplayString() == "System" &&
        namedTypeSymbol.MetadataName == "ReadOnlySpan`1";

    public static bool IsMemoryOfT(this ITypeSymbol typeSymbol) =>
        typeSymbol is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol &&
        namedTypeSymbol.ContainingNamespace.ToDisplayString() == "System" &&
        namedTypeSymbol.MetadataName == "Memory`1";

    public static bool IsReadOnlyMemoryOfT(this ITypeSymbol typeSymbol) =>
        typeSymbol is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol &&
        namedTypeSymbol.ContainingNamespace.ToDisplayString() == "System" &&
        namedTypeSymbol.MetadataName == "ReadOnlyMemory`1";

    public static bool IsImmutableArrayOfT(this ITypeSymbol typeSymbol) =>
        typeSymbol is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol &&
        namedTypeSymbol.ContainingNamespace.ToDisplayString() == "System.Collections.Immutable" &&
        namedTypeSymbol.MetadataName == "ImmutableArray`1";

    public static bool IsQueryableOfT(this ITypeSymbol typeSymbol) =>
        typeSymbol is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol &&
        namedTypeSymbol.ContainingNamespace.ToDisplayString() == "System.Linq" &&
        namedTypeSymbol.MetadataName == "IQueryable`1";

    public static string ToFullyQualifiedDisplayString(this ITypeSymbol typeSymbol) =>
        $"{typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}{(typeSymbol.NullableAnnotation is NullableAnnotation.Annotated ? "?" : string.Empty)}";

    public static T? GetSymbolOrBestCandidate<T>(this SymbolInfo symbolInfo)
        where T : class, ISymbol => symbolInfo switch
    {
        { Symbol: { } symbol } => symbol as T,
        { CandidateReason: CandidateReason.MemberGroup, CandidateSymbols: { Length: 1 } candidateSymbols } => candidateSymbols[0] as T,
        _ => null
    };
}