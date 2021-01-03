using MapTo.Extensions;
using Microsoft.CodeAnalysis;

namespace MapTo.Models
{
    internal record SourceGenerationOptions(
        AccessModifier ConstructorAccessModifier, 
        AccessModifier MappingsAccessModifier,
        bool GenerateXmlDocument)
    {
        internal static SourceGenerationOptions From(GeneratorExecutionContext context) => new(
            context.GetBuildGlobalOption<AccessModifier>(nameof(ConstructorAccessModifier)),
            context.GetBuildGlobalOption<AccessModifier>(nameof(MappingsAccessModifier)),
            context.GetBuildGlobalOption(nameof(GenerateXmlDocument), defaultValue: true)
        );
    }
}