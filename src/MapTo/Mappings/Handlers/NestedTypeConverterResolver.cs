namespace MapTo.Mappings.Handlers;

internal class NestedTypeConverterResolver : ITypeConverterResolver
{
    /// <inheritdoc />
    public ResolverResult<TypeConverterMapping> Get(MappingContext context, IPropertySymbol property, SourceProperty sourceProperty)
    {
        var methodPrefix = context.CodeGeneratorOptions.MapMethodPrefix;
        var mapFromAttribute = property.Type.ToMapFromAttributeMapping(context);
        var mappedSourcePropertyType = mapFromAttribute?.SourceType;

        return mappedSourcePropertyType switch
        {
            null => ResolverResult.Undetermined<TypeConverterMapping>(),
            { IsEnum: true } => ResolverResult.Undetermined<TypeConverterMapping>(), // Is handled by EnumTypeConverterResolver
            _ => new TypeConverterMapping(
                Method: new MethodMapping(
                    ContainingType: mappedSourcePropertyType.Value.ToExtensionClassName(property.Type, context),
                    MethodName: mappedSourcePropertyType.Value.IsPrimitive ? $"{methodPrefix}{mappedSourcePropertyType.Value.Name}" : $"{methodPrefix}{property.Type.Name}",
                    ReturnType: property.Type.ToTypeMapping()),
                Explicit: false,
                Parameter: null,
                IsMapToExtensionMethod: true,
                ReferenceHandling: context.CodeGeneratorOptions.ReferenceHandling switch
                {
                    ReferenceHandling.Disabled => false,
                    ReferenceHandling.Enabled => true,
                    ReferenceHandling.Auto when mappedSourcePropertyType.Value.HasNonPrimitiveProperties => true,
                    _ => false
                })
        };
    }
}