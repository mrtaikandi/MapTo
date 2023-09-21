using MapTo.Configuration;

namespace MapTo.Mappings;

internal readonly record struct TargetMapping(
    AccessModifier Modifier,
    string Name,
    NamespaceMapping Namespace,
    bool IsPartial,
    SourceMapping Source,
    ConstructorMapping Constructor,
    ImmutableArray<PropertyMapping> Properties,
    Location Location,
    ImmutableArray<string> UsingDirectives,
    CodeGeneratorOptions Options);

internal static class TargetMappingFactory
{
    public static TargetMapping Create(MappingContext context)
    {
        var properties = PropertyMappingFactory.Create(context);
        var (targetTypeSyntax, targetTypeSymbol, _, sourceTypeSymbol, _, codeGeneratorOptions, _) = context;

        var mapping = new TargetMapping(
            Modifier: targetTypeSyntax.GetAccessModifier(),
            Name: targetTypeSyntax.Identifier.Text,
            Namespace: NamespaceMapping.Create(targetTypeSymbol),
            IsPartial: targetTypeSyntax.IsPartial(),
            Source: SourceMapping.Create(sourceTypeSymbol),
            Constructor: ConstructorMappingFactory.Create(context, properties),
            Properties: properties,
            Location: targetTypeSyntax.Identifier.GetLocation(),
            UsingDirectives: properties.SelectMany(p => p.UsingDirectives).Distinct().ToImmutableArray(),
            Options: codeGeneratorOptions with
            {
                ReferenceHandling = context.UseReferenceHandling(properties)
            });

        return mapping.IsValid(context) ? mapping : default;
    }

    internal static string GetReturnType(this TargetMapping mapping) =>
        mapping.UseFullyQualifiedName() ? mapping.ToFullyQualifiedName() : mapping.Name;

    internal static string GetSourceType(this TargetMapping mapping) =>
        mapping.UseFullyQualifiedName() ? mapping.Source.ToFullyQualifiedName() : mapping.Source.Name;

    private static string ToFullyQualifiedName(this TargetMapping mapping) =>
        mapping.Namespace.IsGlobalNamespace ? mapping.Name : $"global::{mapping.Namespace}.{mapping.Name}";

    private static bool UseFullyQualifiedName(this TargetMapping mapping) =>
        mapping.Namespace != mapping.Source.Namespace;

    private static ReferenceHandling UseReferenceHandling(this MappingContext context, ImmutableArray<PropertyMapping> properties) =>
        context.CodeGeneratorOptions.ReferenceHandling == ReferenceHandling.Auto &&
        (properties.Any(p => SymbolEqualityComparer.Default.Equals(p.Type, context.TargetTypeSymbol)) || context.TargetTypeSymbol.HasNonPrimitiveProperties())
            ? ReferenceHandling.Enabled
            : context.CodeGeneratorOptions.ReferenceHandling;

    private static bool IsValid(this TargetMapping mapping, MappingContext context)
    {
        if (mapping is { IsPartial: false, Constructor.IsGenerated: true })
        {
            context.ReportDiagnostic(DiagnosticsFactory.MissingPartialKeywordOnTargetClassError(mapping.Location, mapping.Name));
            return false;
        }

        return true;
    }
}