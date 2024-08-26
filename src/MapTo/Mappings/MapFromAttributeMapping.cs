namespace MapTo.Mappings;

internal readonly record struct MapFromAttributeMapping(ITypeSymbol SourceType);

internal static class MapFromAttributeMappingExtensions
{
    internal static MapFromAttributeMapping? ToMapFromAttributeMapping(this ITypeSymbol? typeSymbol, KnownTypes knownTypes)
    {
        if (typeSymbol is null)
        {
            return null;
        }

        var mapFromAttribute = typeSymbol.GetAttribute(knownTypes.MapFromAttributeGenericTypeSymbol)
                               ?? typeSymbol.GetAttribute(knownTypes.MapFromAttributeTypeSymbol);

        if (mapFromAttribute is null)
        {
            return null;
        }

        var sourceType = mapFromAttribute.AttributeClass?.TypeArguments.FirstOrDefault()
                         ?? mapFromAttribute.ConstructorArguments.FirstOrDefault().Value as ITypeSymbol;

        return sourceType is null ? null : new MapFromAttributeMapping(sourceType);
    }
}