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

        var mapFromAttribute = arrayNamedTypeSymbol.ToMapFromAttributeMapping(context.KnownTypes);
        var mappedSourcePropertyType = mapFromAttribute is null ? arrayNamedTypeSymbol : mapFromAttribute.Value.SourceType;

        return new TypeConverterMapping(
            ContainingType: mappedSourcePropertyType.ToExtensionClassName(context),
            MethodName: mappedSourcePropertyType.IsPrimitiveType() ? $"{methodPrefix}{mappedSourcePropertyType.Name}" : $"{methodPrefix}{propertyTypeName}",
            Parameter: null,
            Type: property.Type.ToTypeMapping(),
            Explicit: false,
            IsMapToExtensionMethod: mapFromAttribute is not null,
            UsingDirectives: ImmutableArray.Create("global::System.Linq"),
            ReferenceHandling: context.CodeGeneratorOptions.ReferenceHandling switch
            {
                ReferenceHandling.Disabled => false,
                ReferenceHandling.Enabled => true,
                ReferenceHandling.Auto when mappedSourcePropertyType.HasNonPrimitiveProperties() => true,
                _ => false
            });
    }
}