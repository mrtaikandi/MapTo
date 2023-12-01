using Microsoft.CodeAnalysis.Diagnostics;

namespace MapTo.Configuration;

/// <summary>
/// Represents the analyzer configuration options for the MapTo generator.
/// </summary>
/// <param name="MapMethodPrefix">The generated mapping extension method suffix.</param>
/// <param name="MapExtensionClassSuffix">The generated mapping extension class suffix.</param>
/// <param name="ReferenceHandling">Indicates whether to use reference handling.</param>
/// <param name="CopyPrimitiveArrays">Indicates whether to copy object and primitive type arrays into a new array.</param>
/// <param name="NullHandling">Indicates how to handle null properties.</param>
/// <param name="EnumMappingStrategy">Indicates the strategy to use when mapping enum values.</param>
/// <param name="StrictEnumMapping">Indicates how strict the enum mapping should be.</param>
internal readonly record struct CodeGeneratorOptions(
    string MapMethodPrefix = "MapTo",
    string MapExtensionClassSuffix = "MapToExtensions",
    ReferenceHandling ReferenceHandling = ReferenceHandling.Disabled,
    bool CopyPrimitiveArrays = false,
    NullHandling NullHandling = NullHandling.Auto,
    EnumMappingStrategy EnumMappingStrategy = EnumMappingStrategy.ByValue,
    StrictEnumMapping StrictEnumMapping = StrictEnumMapping.Off)
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
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "Reviewed.")]
    public static CodeGeneratorOptions Create(AnalyzerConfigOptionsProvider provider, CancellationToken cancellationToken)
    {
        return new CodeGeneratorOptions(
            MapMethodPrefix: provider.GlobalOptions.GetOption(nameof(MapMethodPrefix), "MapTo"),
            MapExtensionClassSuffix: provider.GlobalOptions.GetOption(nameof(MapExtensionClassSuffix), "MapToExtensions"),
            ReferenceHandling: provider.GlobalOptions.GetOption<ReferenceHandling>(nameof(ReferenceHandling)),
            CopyPrimitiveArrays: provider.GlobalOptions.GetOption(nameof(CopyPrimitiveArrays), false),
            NullHandling: provider.GlobalOptions.GetOption<NullHandling>(nameof(NullHandling)),
            EnumMappingStrategy: provider.GlobalOptions.GetOption(nameof(EnumMappingStrategy), EnumMappingStrategy.ByValue),
            StrictEnumMapping: provider.GlobalOptions.GetOption(nameof(StrictEnumMapping), StrictEnumMapping.Off));
    }
}