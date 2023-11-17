namespace MapTo.Mappings.Handlers;

internal class ImplicitTypeConverterResolver : ITypeConverterResolver
{
    public ResolverResult<TypeConverterMapping> Get(MappingContext context, IPropertySymbol property, SourceProperty sourceProperty)
    {
        if (!property.Type.IsArray() && context.Compilation.HasCompatibleTypes(sourceProperty.TypeSymbol, property))
        {
            return ResolverResult.Success<TypeConverterMapping>();
        }

        if (!context.Compilation.IsNonGenericEnumerable(property.Type) && context.Compilation.IsNonGenericEnumerable(sourceProperty.TypeSymbol))
        {
            return DiagnosticsFactory.PropertyTypeConverterRequiredError(property);
        }

        return ResolverResult.Undetermined<TypeConverterMapping>();
    }
}