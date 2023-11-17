namespace MapTo.Mappings.Handlers;

internal class NestedTypeConverterResolver : ITypeConverterResolver
{
    /// <inheritdoc />
    public virtual ResolverResult<TypeConverterMapping> Get(MappingContext context, IPropertySymbol property, SourceProperty sourceProperty)
    {
        var mapFromAttribute = property.Type.GetAttribute(context.KnownTypes.MapFromAttributeTypeSymbol);
        if (mapFromAttribute is null)
        {
            return ResolverResult.Undetermined<TypeConverterMapping>();
        }

        if (mapFromAttribute.ConstructorArguments.First().Value is not INamedTypeSymbol mappedSourcePropertyType)
        {
            return ResolverResult.Undetermined<TypeConverterMapping>();
        }

        var methodPrefix = context.CodeGeneratorOptions.MapMethodPrefix;
        return new TypeConverterMapping(
            mappedSourcePropertyType.ToExtensionClassName(context.CodeGeneratorOptions),
            mappedSourcePropertyType.IsPrimitiveType() ? $"{methodPrefix}{mappedSourcePropertyType.Name}" : $"{methodPrefix}{property.Type.Name}",
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