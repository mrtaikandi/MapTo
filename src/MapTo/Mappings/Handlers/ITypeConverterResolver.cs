namespace MapTo.Mappings.Handlers;

internal interface ITypeConverterResolver
{
    ResolverResult<TypeConverterMapping> Get(MappingContext context, IPropertySymbol property, SourceProperty sourceProperty);
}