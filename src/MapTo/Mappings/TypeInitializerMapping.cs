namespace MapTo.Mappings;

internal readonly record struct TypeInitializerMapping(
    string SourceName,
    string TargetName,
    ConstructorMapping TargetConstructor,
    ImmutableArray<PropertyMapping> TargetProperties,
    CodeGeneratorOptions Options);

internal static class TypeInitializerMappingExtensions
{
    internal static TypeInitializerMapping ToTypeInitializerMapping(this TargetMapping mapping, string? sourceName = null) => new(
        SourceName: sourceName ?? mapping.Source.Name,
        TargetName: mapping.Name,
        TargetConstructor: mapping.Constructor,
        TargetProperties: mapping.Properties,
        Options: mapping.Options);
}