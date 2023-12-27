namespace MapTo.Mappings;

internal readonly record struct NamespaceMapping(string Value, bool IsGlobalNamespace)
{
    public static implicit operator string(NamespaceMapping mapping) => mapping.Value;

    public static NamespaceMapping Create(INamedTypeSymbol symbol) => new(
        symbol.ContainingNamespace.ToDisplayString(),
        symbol.ContainingNamespace.IsGlobalNamespace);

    /// <inheritdoc />
    public override string ToString() => Value;
}