namespace MapTo.Mappings;

internal readonly record struct SourceMapping(string Name, NamespaceMapping Namespace)
{
    internal static SourceMapping Create(INamedTypeSymbol sourceTypeSymbol) =>
        new(sourceTypeSymbol.Name, NamespaceMapping.Create(sourceTypeSymbol));
}

internal static class SourceTypeMappingExtensions
{
    internal static string ToFullyQualifiedName(this SourceMapping mapping) =>
        mapping.Namespace.IsGlobalNamespace ? mapping.Name : $"global::{mapping.Namespace}.{mapping.Name}";
}