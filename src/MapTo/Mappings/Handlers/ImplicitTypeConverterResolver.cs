namespace MapTo.Mappings.Handlers;

internal class ImplicitTypeConverterResolver : ITypeConverterResolver
{
    public ResolverResult<TypeConverterMapping> Get(MappingContext context, IPropertySymbol property, SourceProperty sourceProperty)
    {
        if (!property.Type.IsArray() && context.Compilation.HasCompatibleTypes(sourceProperty.TypeSymbol, property))
        {
            return ResolverResult.Success<TypeConverterMapping>();
        }

        return ResolverResult.Undetermined<TypeConverterMapping>();
    }
}