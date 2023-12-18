namespace MapTo.Generators.Properties;

internal interface IPropertyMappingGenerator
{
    IPropertyMappingGenerator Next(IPropertyMappingGenerator generator);

    bool Handle(PropertyGeneratorContext context);
}