namespace MapTo.Mappings;

internal readonly record struct MethodMapping(
    string ContainingType,
    string MethodName,
    ImmutableArray<string> Parameter,
    bool ReturnsVoid)
{
    public string MethodFullName => $"{ContainingType}.{MethodName}";

    internal static MethodMapping Create(IMethodSymbol methodSymbol) => new(
        ContainingType: methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
        MethodName: methodSymbol.Name,
        Parameter: methodSymbol.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).ToImmutableArray(),
        ReturnsVoid: methodSymbol.ReturnsVoid);
}