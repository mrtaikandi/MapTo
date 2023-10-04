namespace MapTo.Generators.Handlers;

internal interface IPropertyMappingGenerator
{
    IPropertyMappingGenerator Next(IPropertyMappingGenerator generator);

    bool Handle(PropertyGeneratorContext context);
}