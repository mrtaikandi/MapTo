using MapTo.Extensions;

namespace MapTo.Mappings;

internal readonly record struct PropertyMapping(
    string Name,
    INamedTypeSymbol Type,
    string SourceName,
    INamedTypeSymbol SourceType,
    PropertyInitializationMode InitializationMode,
    string ParameterName)
{
    public string TypeName => Type.ToDisplayString();

    public static PropertyMapping? Create(MappingContext context, IPropertySymbol property, INamedTypeSymbol sourceTypeSymbol)
    {
        var mapPropertyAttributeTypeSymbol = context.WellKnownTypes.MapPropertyAttributeTypeSymbol;
        var sourcePropertyName = property.GetSourcePropertyName(mapPropertyAttributeTypeSymbol);
        var sourceProperty = sourceTypeSymbol.FindProperty(sourcePropertyName);

        if (sourceProperty is null)
        {
            return null;
        }

        return new(
            property.Name,
            (INamedTypeSymbol)property.Type,
            sourceProperty.Name,
            (INamedTypeSymbol)sourceProperty.Type,
            property.SetMethod is null ? PropertyInitializationMode.Constructor : PropertyInitializationMode.ObjectInitializer,
            property.Name.ToParameterNameCasing());
    }
}

internal static class PropertyMappingExtensions
{
    internal static IPropertySymbol? FindProperty(this ITypeSymbol typeSymbol, string propertyName) => typeSymbol
        .GetAllMembers()
        .OfType<IPropertySymbol>()
        .SingleOrDefault(p => p.Name == propertyName);

    internal static string GetSourcePropertyName(this ISymbol targetProperty, ITypeSymbol propertyAttributeTypeSymbol) => targetProperty
        .GetAttribute(propertyAttributeTypeSymbol)
        ?.NamedArguments
        .SingleOrDefault(a => a.Key == WellKnownTypes.MapPropertyAttributeSourcePropertyName)
        .Value.Value as string ?? targetProperty.Name;

    internal static bool IsCompatibleWith(this PropertyMapping mapping, IParameterSymbol parameter, SemanticModel semanticModel) =>
        mapping.ParameterName == parameter.Name && semanticModel.Compilation.HasCompatibleTypes(parameter.Type, mapping.Type);
}