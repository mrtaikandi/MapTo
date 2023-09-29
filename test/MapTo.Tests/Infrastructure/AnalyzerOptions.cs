using MapTo.CodeAnalysis;

namespace MapTo.Tests.Infrastructure;

internal class AnalyzerOptions
{
    internal static readonly Dictionary<string, string> DisableXmlDoc = new()
    {
        [AnalyzerConfigOptionsExtensions.GetBuildPropertyName("GenerateXmlDocument")] = "false"
    };
}