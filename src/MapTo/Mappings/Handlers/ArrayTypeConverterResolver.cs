namespace MapTo.Mappings.Handlers;

internal class ArrayTypeConverterResolver : ITypeConverterResolver
{
    /// <inheritdoc />
    public ResolverResult<TypeConverterMapping> Get(MappingContext context, IPropertySymbol property, SourceProperty sourceProperty)
    {
        if (property.Type.IsPrimitiveType() || property.Type is not IArrayTypeSymbol { ElementType: INamedTypeSymbol arrayNamedTypeSymbol })
        {
            return ResolverResult.Undetermined<TypeConverterMapping>();
        }

        var typeConverterAttribute = property.GetAttribute(context.KnownTypes.PropertyTypeConverterAttributeTypeSymbol);
        if (typeConverterAttribute is not null)
        {
            return ResolverResult.Undetermined<TypeConverterMapping>();
        }

        var methodPrefix = context.CodeGeneratorOptions.MapMethodPrefix;
        var propertyTypeName = arrayNamedTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        TypeMapping mappedSourcePropertyType;
        var mapFromAttribute = arrayNamedTypeSymbol.ToMapFromAttributeMapping(context);
        if (mapFromAttribute?.SourceType is not null)
        {
            mappedSourcePropertyType = mapFromAttribute.Value.SourceType;
        }
        else
        {
            var elementType = sourceProperty.TypeSymbol.GetElementType();
            if (elementType is null)
            {
                return ResolverResult.Undetermined<TypeConverterMapping>();
            }

            mapFromAttribute = elementType.ToMapFromAttributeMapping(context);
            mappedSourcePropertyType = mapFromAttribute?.SourceType ?? elementType.ToTypeMapping();
        }

        return new TypeConverterMapping(
            Method: new MethodMapping(
                ContainingType: mappedSourcePropertyType.ToExtensionClassName(context),
                MethodName: mappedSourcePropertyType.IsPrimitive ? $"{methodPrefix}{mappedSourcePropertyType.Name}" : $"{methodPrefix}{propertyTypeName}",
                ReturnType: property.Type.ToTypeMapping()),
            Explicit: false,
            IsMapToExtensionMethod: mapFromAttribute is not null,
            ReferenceHandling: context.CodeGeneratorOptions.ReferenceHandling switch
            {
                ReferenceHandling.Disabled => false,
                ReferenceHandling.Enabled => true,
                ReferenceHandling.Auto when mappedSourcePropertyType.HasNonPrimitiveProperties => true,
                _ => false
            });
    }
}