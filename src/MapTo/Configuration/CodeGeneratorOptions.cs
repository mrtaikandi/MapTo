using Microsoft.CodeAnalysis.Diagnostics;

namespace MapTo.Configuration;

/// <summary>
/// Represents the analyzer configuration options for the MapTo generator.
/// </summary>
/// <param name="GeneratedMethodsAccessModifier">The access modifier for the generated methods.</param>
/// <param name="MapMethodPrefix">The generated mapping extension method suffix.</param>
/// <param name="MapExtensionClassSuffix">The generated mapping extension class suffix.</param>
/// <param name="UseReferenceHandling">Indicates whether to use reference handling.</param>
internal readonly record struct CodeGeneratorOptions(
    AccessModifier GeneratedMethodsAccessModifier = AccessModifier.Public,
    string MapMethodPrefix = "MapTo",
    string MapExtensionClassSuffix = "MapToExtensions",
    bool? UseReferenceHandling = null)
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
        GeneratedMethodsAccessModifier: provider.GlobalOptions.GetOption(nameof(GeneratedMethodsAccessModifier), AccessModifier.Public),
        MapMethodPrefix: provider.GlobalOptions.GetOption(nameof(MapMethodPrefix), "MapTo"),
        MapExtensionClassSuffix: provider.GlobalOptions.GetOption(nameof(MapExtensionClassSuffix), "MapToExtensions"),
        UseReferenceHandling: provider.GlobalOptions.GetOption<bool?>(nameof(UseReferenceHandling)));
}