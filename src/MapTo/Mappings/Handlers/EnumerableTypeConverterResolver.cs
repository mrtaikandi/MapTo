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
            ContainingType: mappedSourcePropertyType.ToExtensionClassName(context.CodeGeneratorOptions),
            MethodName: mappedSourcePropertyType.IsPrimitiveType() ? $"{methodPrefix}{mappedSourcePropertyType.Name}" : $"{methodPrefix}{propertyTypeName}",
            Type: property.Type.ToTypeMapping(),
            Explicit: false,
            Parameter: null,
            IsMapToExtensionMethod: true,
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