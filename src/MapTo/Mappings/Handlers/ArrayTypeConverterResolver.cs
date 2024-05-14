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

        var mapFromAttribute = arrayNamedTypeSymbol.GetAttribute(context.KnownTypes.MapFromAttributeTypeSymbol);
        var propertyTypeName = arrayNamedTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var mappedSourcePropertyType = mapFromAttribute?.ConstructorArguments.First().Value as INamedTypeSymbol ?? arrayNamedTypeSymbol;
        var methodPrefix = context.CodeGeneratorOptions.MapMethodPrefix;

        return new TypeConverterMapping(
            ContainingType: mappedSourcePropertyType.ToExtensionClassName(context.CodeGeneratorOptions),
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