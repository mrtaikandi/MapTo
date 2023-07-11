using MapTo.CodeAnalysis;
using MapTo.Configuration;

namespace MapTo.Tests.Infrastructure;

internal class AnalyzerOptions
{
    internal static readonly Dictionary<string, string> DisableXmlDoc = new()
    {
        [AnalyzerConfigOptionsExtensions.GetBuildPropertyName(nameof(CodeGeneratorOptions.GenerateXmlDocument))] = "false"
    };
}