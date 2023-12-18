using MapTo.Generators.Properties;

namespace MapTo.Generators;

internal sealed class PropertyGenerator
{
    private static readonly Lazy<PropertyGenerator> LazyInstance = new(() => new PropertyGenerator());
    private readonly IPropertyMappingGenerator _root;

    private PropertyGenerator()
    {
        // The order of the property mapping generators is important
        // and should be from most specific to least specific.
        _root = new ArrayPropertyMappingGenerator();
        _root.Next(new GenericEnumerablePropertyMappingGenerator())
            .Next(new TypeConverterPropertyMappingGenerator())
            .Next(new SimplePropertyMappingGenerator());
    }

    internal static PropertyGenerator Instance => LazyInstance.Value;

    internal void Generate(PropertyGeneratorContext context)
    {
        if (!_root.Handle(context))
        {
            throw new InvalidOperationException(
                $"Unable to find a property mapping generator for the {context.PropertyMapping.TypeName}.{context.PropertyMapping.Name}." +
                $"Please open an issue at https://github.com/mrtaikandi/mapto");
        }
    }
}