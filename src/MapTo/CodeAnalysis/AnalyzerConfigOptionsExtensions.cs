using Microsoft.CodeAnalysis.Diagnostics;
using static MapTo.Configuration.CodeGeneratorOptions;

namespace MapTo.CodeAnalysis;

internal static class AnalyzerConfigOptionsExtensions
{
    internal static string GetBuildPropertyName(string propertyName) => $"build_property.{GlobalBuildOptionsPropertyNamePrefix}_{propertyName}";

    internal static T GetOption<T>(this AnalyzerConfigOptions options, string propertyName, T defaultValue = default!)
        where T : notnull
    {
        if (!options.TryGetValue(GetBuildPropertyName(propertyName), out var value) || string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        var type = typeof(T);
        return type.IsEnum
            ? (T)Enum.Parse(type, value, true)
            : (T)Convert.ChangeType(value, type);
    }
}