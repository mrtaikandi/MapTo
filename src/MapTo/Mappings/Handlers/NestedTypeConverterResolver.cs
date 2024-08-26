namespace MapTo.Mappings.Handlers;

internal class NestedTypeConverterResolver : ITypeConverterResolver
{
    /// <inheritdoc />
    public ResolverResult<TypeConverterMapping> Get(MappingContext context, IPropertySymbol property, SourceProperty sourceProperty)
    {
        var methodPrefix = context.CodeGeneratorOptions.MapMethodPrefix;
        var mapFromAttribute = property.Type.GetAttribute(context.KnownTypes.MapFromAttributeTypeSymbol);
        var mappedSourcePropertyType = mapFromAttribute?.ConstructorArguments.First().Value as INamedTypeSymbol;

        return mappedSourcePropertyType switch
        {
            null => ResolverResult.Undetermined<TypeConverterMapping>(),
            { TypeKind: TypeKind.Enum } => ResolverResult.Undetermined<TypeConverterMapping>(), // Is handled by EnumTypeConverterResolver
            _ => new TypeConverterMapping(
                ContainingType: mappedSourcePropertyType.ToExtensionClassName(context),
                MethodName: mappedSourcePropertyType.IsPrimitiveType() ? $"{methodPrefix}{mappedSourcePropertyType.Name}" : $"{methodPrefix}{property.Type.Name}",
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
                })
        };
    }
}