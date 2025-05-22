namespace MapTo.Mappings.Handlers;

internal class EnumerableTypeConverterResolver : ITypeConverterResolver
{
    /// <inheritdoc />
    public ResolverResult<TypeConverterMapping> Get(MappingContext context, IPropertySymbol property, SourceProperty sourceProperty)
    {
        if (property.Type.IsPrimitiveType())
        {
            return ResolverResult.Undetermined<TypeConverterMapping>();
        }

        var typeConverterAttribute = property.GetAttribute(context.KnownTypes.PropertyTypeConverterAttributeTypeSymbol);
        if (typeConverterAttribute is not null)
        {
            return ResolverResult.Undetermined<TypeConverterMapping>();
        }

        if (property.Type is not INamedTypeSymbol enumerableTypeSymbol || enumerableTypeSymbol.TypeArguments.IsEmpty)
        {
            return DiagnosticsFactory.SuitableMappingTypeInNestedPropertyNotFoundError(property, property.Type);
        }

        TypeMapping mappedSourcePropertyType;
        var typeSymbol = enumerableTypeSymbol.TypeArguments.First();
        var mapFromAttribute = typeSymbol.ToMapFromAttributeMapping(context);

        if (mapFromAttribute?.SourceType is not null)
        {
            mappedSourcePropertyType = mapFromAttribute.Value.SourceType;
        }
        else
        {
            var elementType = typeSymbol.GetElementType();
            if (elementType is null || !elementType.IsPrimitiveType())
            {
                return DiagnosticsFactory.SuitableMappingTypeInNestedPropertyNotFoundError(property, property.Type);
            }

            mapFromAttribute = elementType.ToMapFromAttributeMapping(context);
            mappedSourcePropertyType = mapFromAttribute?.SourceType ?? elementType.ToTypeMapping();
        }

        var propertyTypeName = typeSymbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var methodPrefix = context.CodeGeneratorOptions.MapMethodPrefix;

        return new TypeConverterMapping(
            Method: new MethodMapping(
                ContainingType: mappedSourcePropertyType.ToExtensionClassName(typeSymbol, context),
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