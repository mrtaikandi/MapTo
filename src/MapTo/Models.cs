using System.Collections.Immutable;
using MapTo.Extensions;
using Microsoft.CodeAnalysis;

namespace MapTo
{
    internal record SourceGenerationOptions(
        AccessModifier ConstructorAccessModifier,
        AccessModifier GeneratedMethodsAccessModifier,
        bool GenerateXmlDocument)
    {
        internal static SourceGenerationOptions From(GeneratorExecutionContext context) => new(
            context.GetBuildGlobalOption<AccessModifier>(nameof(ConstructorAccessModifier)),
            context.GetBuildGlobalOption<AccessModifier>(nameof(GeneratedMethodsAccessModifier)),
            context.GetBuildGlobalOption(nameof(GenerateXmlDocument), true)
        );
    }

    internal record MappedProperty(string Name, string? ConverterFullyQualifiedName);

    internal record MappingModel (
        SourceGenerationOptions Options,
        string? Namespace,
        SyntaxTokenList ClassModifiers,
        string ClassName,
        string SourceNamespace,
        string SourceClassName,
        string SourceClassFullName,
        ImmutableArray<MappedProperty> MappedProperties
    );
}