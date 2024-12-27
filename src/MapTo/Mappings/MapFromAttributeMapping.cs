namespace MapTo.Mappings;

internal readonly record struct MapFromAttributeMapping(TypeMapping SourceType);

internal static class MapFromAttributeMappingExtensions
{
    internal static MapFromAttributeMapping? ToMapFromAttributeMapping(this ITypeSymbol? typeSymbol, MappingContext context)
    {
        if (typeSymbol is null)
        {
            return null;
        }

        var mapFromAttribute = GetMapAttribute(typeSymbol, context);
        if (mapFromAttribute is null)
        {
            return null;
        }

        var sourceType = mapFromAttribute.AttributeClass?.TypeArguments.FirstOrDefault()
                         ?? mapFromAttribute.ConstructorArguments.FirstOrDefault().Value as ITypeSymbol;

        return sourceType is null ? null : new MapFromAttributeMapping(sourceType.ToTypeMapping());
    }

    private static AttributeData? GetMapAttribute(ITypeSymbol typeSymbol, MappingContext context)
    {
        var mapFromAttribute = typeSymbol.GetAttribute(context.KnownTypes.MapFromAttributeGenericTypeSymbol)
                               ?? typeSymbol.GetAttribute(context.KnownTypes.MapFromAttributeTypeSymbol);

        if (mapFromAttribute is null)
        {
            var compilation = context.TargetSemanticModel.Compilation;
            mapFromAttribute = compilation.Assembly
                .GetAttributes()
                .FirstOrDefault(
                    a => a.AttributeClass is not null &&
                         SymbolEqualityComparer.Default.Equals(a.AttributeClass.ConstructedFrom, context.KnownTypes.MapAttributeTypeSymbol) &&
                         a.AttributeClass.TypeArguments[1].Equals(typeSymbol, SymbolEqualityComparer.Default));
        }

        return mapFromAttribute;
    }
}