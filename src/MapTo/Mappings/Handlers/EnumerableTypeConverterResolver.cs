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

        if (property.Type is not INamedTypeSymbol enumerableTypeSymbol || enumerableTypeSymbol.TypeArguments.IsEmpty)
        {
            return DiagnosticsFactory.SuitableMappingTypeInNestedPropertyNotFoundError(property, property.Type);
        }

        var typeSymbol = enumerableTypeSymbol.TypeArguments.First();
        var mapFromAttribute = typeSymbol.GetAttribute(context.KnownTypes.MapFromAttributeTypeSymbol);
        if (mapFromAttribute?.ConstructorArguments.First().Value is not INamedTypeSymbol mappedSourcePropertyType)
        {
            return DiagnosticsFactory.SuitableMappingTypeInNestedPropertyNotFoundError(property, property.Type);
        }

        var propertyTypeName = typeSymbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var methodPrefix = context.CodeGeneratorOptions.MapMethodPrefix;

        return new TypeConverterMapping(
            mappedSourcePropertyType.ToExtensionClassName(context.CodeGeneratorOptions),
            mappedSourcePropertyType.IsPrimitiveType() ? $"{methodPrefix}{mappedSourcePropertyType.Name}" : $"{methodPrefix}{propertyTypeName}",
            property.Type.ToTypeMapping(),
            null,
            true,
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