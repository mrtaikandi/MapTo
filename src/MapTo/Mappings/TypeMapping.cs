using System.Diagnostics;

namespace MapTo.Mappings;

[DebuggerDisplay($"FQN: {{{nameof(FullName)}}}")]
internal readonly record struct TypeMapping(
    string Name,
    string FullName,
    bool IsArray,
    string ElementTypeFullName)
{
#if DEBUG
    internal ITypeSymbol OriginalTypeSymbol { get; init; }
#endif
}

internal static class TypeMappingExtensions
{
    internal static TypeMapping ToTypeMapping(this ITypeSymbol typeSymbol)
    {
        var isArray = false;
        var elementType = string.Empty;

        if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
        {
            isArray = true;
            elementType = arrayTypeSymbol.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        var mapping = new TypeMapping(
            Name: typeSymbol.Name,
            FullName: typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            IsArray: isArray,
            ElementTypeFullName: elementType);

#if DEBUG
        return mapping with { OriginalTypeSymbol = typeSymbol };
#else
        return mapping;
#endif
    }
}