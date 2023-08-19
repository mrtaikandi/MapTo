using Microsoft.CodeAnalysis.Diagnostics;

namespace MapTo.Configuration;

/// <summary>
/// Represents the analyzer configuration options for the MapTo generator.
/// </summary>
/// <param name="ConstructorAccessModifier">The access modifier for the generated constructor.</param>
/// <param name="GeneratedMethodsAccessModifier">The access modifier for the generated methods.</param>
/// <param name="GenerateXmlDocument">Indicates whether to generate an XML documentation file for the generated code.</param>
/// <param name="MapMethodPrefix">The generated mapping extension method suffix.</param>
/// <param name="MapExtensionClassSuffix">The generated mapping extension class suffix.</param>
internal readonly record struct CodeGeneratorOptions(
    AccessModifier ConstructorAccessModifier = AccessModifier.Public,
    AccessModifier GeneratedMethodsAccessModifier = AccessModifier.Public,
    bool GenerateXmlDocument = true,
    string MapMethodPrefix = "MapTo",
    string MapExtensionClassSuffix = "MapToExtensions")
{
    /// <summary>
    /// The prefix of the property name in the .editorconfig file.
    /// </summary>
    internal const string GlobalBuildOptionsPropertyNamePrefix = "MapTo";

    /// <summary>
    /// Creates a new instance of the <see cref="CodeGeneratorOptions" /> class from the specified analyzer configuration provider.
    /// </summary>
    /// <param name="provider">The <see cref="AnalyzerConfigOptionsProvider" /> to use.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken" /> to use.</param>
    /// <returns>A new instance of the <see cref="CodeGeneratorOptions" /> class.</returns>
    public static CodeGeneratorOptions Create(AnalyzerConfigOptionsProvider provider, CancellationToken cancellationToken) => new(
        provider.GlobalOptions.GetOption(nameof(ConstructorAccessModifier), AccessModifier.Public),
        provider.GlobalOptions.GetOption(nameof(GeneratedMethodsAccessModifier), AccessModifier.Public),
        provider.GlobalOptions.GetOption(nameof(GenerateXmlDocument), true),
        provider.GlobalOptions.GetOption(nameof(MapMethodPrefix), "MapTo"));
}