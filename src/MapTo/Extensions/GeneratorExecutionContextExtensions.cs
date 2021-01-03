using System;
using Microsoft.CodeAnalysis;

namespace MapTo.Extensions
{
    internal static class GeneratorExecutionContextExtensions
    {
        private const string PropertyNameSuffix = "MapTo_";

        internal static T GetBuildGlobalOption<T>(this GeneratorExecutionContext context, string propertyName, T defaultValue = default!) where T: notnull
        {
            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(GetBuildPropertyName(propertyName), out var optionValue))
            {
                return defaultValue;
            }

            var type = typeof(T);

            if (!type.IsEnum)
            {
                return (T)Convert.ChangeType(optionValue, type);
            }

            try
            {
                return (T)Enum.Parse(type, optionValue, true);
            }
            catch (Exception)
            {
                context.ReportDiagnostic(Diagnostics.ConfigurationParseError($"'{optionValue}' is not a valid value for {PropertyNameSuffix}{propertyName} property."));
                return defaultValue;
            }
        }

        internal static string GetBuildPropertyName(string propertyName) => $"build_property.{PropertyNameSuffix}{propertyName}";
    }
}