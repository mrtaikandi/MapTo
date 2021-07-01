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
        SyntaxTokenList Modifiers,
        string Type,
        string TypeIdentifierName,
        string SourceNamespace,
        string SourceTypeIdentifierName,
        string SourceTypeFullName,
        ImmutableArray<MappedProperty> MappedProperties,
        bool HasMappedBaseClass,
        ImmutableArray<string> Usings,
        bool GenerateSecondaryConstructor
    )
    {
        public string SourceType => SourceTypeIdentifierName == TypeIdentifierName ? SourceTypeFullName : SourceTypeIdentifierName;
    }

    internal record SourceGenerationOptions(
        AccessModifier ConstructorAccessModifier,
        AccessModifier GeneratedMethodsAccessModifier,
        bool GenerateXmlDocument,
        bool SupportNullableReferenceTypes,
        bool SupportNullableStaticAnalysis,
        LanguageVersion LanguageVersion)
    {
        internal static SourceGenerationOptions From(GeneratorExecutionContext context)
        {
            var compilation = context.Compilation as CSharpCompilation;
            var supportNullableReferenceTypes = false;
            var supportNullableStaticAnalysis = false;

            if (compilation is not  null)
            {
                supportNullableStaticAnalysis = compilation.LanguageVersion >= LanguageVersion.CSharp8;
                supportNullableReferenceTypes = compilation.Options.NullableContextOptions == NullableContextOptions.Warnings || 
                                                compilation.Options.NullableContextOptions == NullableContextOptions.Enable;
            }
            
            return new(
                context.GetBuildGlobalOption(nameof(ConstructorAccessModifier), AccessModifier.Public),
                context.GetBuildGlobalOption(nameof(GeneratedMethodsAccessModifier), AccessModifier.Public),
                context.GetBuildGlobalOption(nameof(GenerateXmlDocument), true),
                supportNullableReferenceTypes,
                supportNullableStaticAnalysis,
                compilation?.LanguageVersion ?? LanguageVersion.Default
            );
        }

        public string NullableReferenceSyntax => SupportNullableReferenceTypes ? "?" : string.Empty;
    }
}