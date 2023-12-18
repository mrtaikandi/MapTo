namespace MapTo.Generators.Properties;

internal abstract class PropertyMappingGenerator : IPropertyMappingGenerator
{
    private IPropertyMappingGenerator? _next;

    public IPropertyMappingGenerator Next(IPropertyMappingGenerator generator)
    {
        _next = generator;
        return generator;
    }

    public bool Handle(PropertyGeneratorContext context) =>
        HandleCore(context) || _next?.Handle(context) == true;

    protected abstract bool HandleCore(PropertyGeneratorContext context);
}