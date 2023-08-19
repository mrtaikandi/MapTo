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
    ImmutableArray<string> UsingDirectives);

internal static class TargetMappingFactory
{
    public static TargetMapping Create(MappingContext context)
    {
        var (targetTypeSyntax, targetTypeSymbol, _, sourceTypeSymbol, _, _, _) = context;

        var properties = context.GetMappedProperties();

        var mapping = new TargetMapping(
            Modifier: targetTypeSyntax.GetAccessModifier(),
            Name: targetTypeSyntax.Identifier.Text,
            Namespace: NamespaceMapping.Create(targetTypeSymbol),
            IsPartial: targetTypeSyntax.IsPartial(),
            Source: SourceMapping.Create(sourceTypeSymbol),
            Constructor: ConstructorMapping.Create(context, properties),
            Properties: properties,
            Location: targetTypeSyntax.Identifier.GetLocation(),
            UsingDirectives: properties.SelectMany(p => p.UsingDirectives).Distinct().ToImmutableArray());

        return mapping.IsValid(context) ? mapping : default;
    }

    internal static string GetReturnType(this TargetMapping mapping) =>
        mapping.UseFullyQualifiedName() ? mapping.ToFullyQualifiedName() : mapping.Name;

    internal static string GetSourceType(this TargetMapping mapping) =>
        mapping.UseFullyQualifiedName() ? mapping.Source.ToFullyQualifiedName() : mapping.Source.Name;

    private static ImmutableArray<PropertyMapping> GetMappedProperties(this MappingContext context)
    {
        var (_, targetTypeSymbol, _, sourceTypeSymbol, wellKnownTypes, _, _) = context;
        var isInheritFromMappedBaseClass = context.IsTargetTypeInheritFromMappedBaseClass();

        return targetTypeSymbol
            .GetAllMembers(!isInheritFromMappedBaseClass)
            .OfType<IPropertySymbol>()
            .Where(p => !p.HasAttribute(wellKnownTypes.IgnorePropertyAttributeTypeSymbol))
            .Select(p => PropertyMappingFactory.Create(context, p, sourceTypeSymbol))
            .Where(p => p is not null)
            .Select(p => p!.Value)
            .ToImmutableArray();
    }

    private static string ToFullyQualifiedName(this TargetMapping mapping) =>
        mapping.Namespace.IsGlobalNamespace ? mapping.Name : $"global::{mapping.Namespace}.{mapping.Name}";

    private static bool UseFullyQualifiedName(this TargetMapping mapping) =>
        mapping.Namespace != mapping.Source.Namespace;

    private static bool IsTargetTypeInheritFromMappedBaseClass(this MappingContext context)
    {
        var (typeSyntax, _, semanticModel, _, wellKnownTypes, _, _) = context;
        return typeSyntax.BaseList is not null && typeSyntax.BaseList.Types
            .Select(t => semanticModel.GetTypeInfo(t.Type).Type)
            .Any(t => t?.GetAttribute(wellKnownTypes.MapFromAttributeTypeSymbol) is not null);
    }

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