using Microsoft.CodeAnalysis.Diagnostics;
using static MapTo.Configuration.CodeGeneratorOptions;

namespace MapTo.CodeAnalysis;

internal static class AnalyzerConfigOptionsExtensions
{
    internal static string GetBuildPropertyName(string propertyName) => $"build_property.{GlobalBuildOptionsPropertyNamePrefix}_{propertyName}";

    internal static T GetOption<T>(this AnalyzerConfigOptions options, string propertyName, T defaultValue = default!)
    {
        if (!options.TryGetValue(GetBuildPropertyName(propertyName), out var value) || string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        var type = typeof(T);
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (!type.IsEnum)
        {
            return (T)Convert.ChangeType(value, type);
        }

        if (type.GetCustomAttributes(typeof(FlagsAttribute), false).Length == 0)
        {
            return (T)Enum.Parse(type, value, true);
        }

        var values = value.Split(['|'], StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim())
            .Select(v => Enum.Parse(type, v, true))
            .Cast<int>()
            .Aggregate(0, (current, next) => current | next);

        return (T)Enum.ToObject(type, values);
    }
}