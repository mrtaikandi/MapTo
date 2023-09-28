using System.Diagnostics;

namespace MapTo.Mappings;

[DebuggerDisplay($"FQN: {{{nameof(FullName)}}}")]
internal readonly record struct TypeMapping(string Name, string FullName);

internal static class TypeMappingExtensions
{
    internal static TypeMapping ToTypeMapping(this ITypeSymbol typeSymbol) => new(
        Name: typeSymbol.Name,
        FullName: typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
}