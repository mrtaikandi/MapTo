using System.Collections.Immutable;
using MapTo.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MapTo
{
    internal record SourceCode(string Text, string HintName);

    internal record MappedProperty(
        string Name,
        string Type,
        string? TypeConverter,
        ImmutableArray<string> TypeConverterParameters,
        string SourcePropertyName,
        string? MappedSourcePropertyTypeName,
        string? EnumerableTypeArgument)
    {
        public bool IsEnumerable => EnumerableTypeArgument is not null;
    }

    internal record MappingModel (
        SourceGenerationOptions Options,
        string? Namespace,
        SyntaxTokenList ClassModifiers,
        string ClassName,
        string SourceNamespace,
        string SourceClassName,
        string SourceClassFullName,
        ImmutableArray<MappedProperty> MappedProperties,
        bool HasMappedBaseClass,
        ImmutableArray<string> Usings
    );

    internal record SourceGenerationOptions(
        AccessModifier ConstructorAccessModifier,
        AccessModifier GeneratedMethodsAccessModifier,
        bool GenerateXmlDocument,
        bool SupportNullableReferenceTypes)
    {
        internal static SourceGenerationOptions From(GeneratorExecutionContext context)
        {
            var compilationOptions = (context.Compilation as CSharpCompilation)?.Options;

            return new(
                context.GetBuildGlobalOption(nameof(ConstructorAccessModifier), AccessModifier.Public),
                context.GetBuildGlobalOption(nameof(GeneratedMethodsAccessModifier), AccessModifier.Public),
                context.GetBuildGlobalOption(nameof(GenerateXmlDocument), true),
                compilationOptions is not null && (compilationOptions.NullableContextOptions == NullableContextOptions.Warnings || compilationOptions.NullableContextOptions == NullableContextOptions.Enable)
            );
        }

        public string NullableReferenceSyntax => SupportNullableReferenceTypes ? "?" : string.Empty;
    }
}