using System;
using System.Text;
using MapTo.Sources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace MapTo.Extensions
{
    internal static class GeneratorExecutionContextExtensions
    {
        private const string PropertyNameSuffix = "MapTo_";

        internal static T GetBuildGlobalOption<T>(this GeneratorExecutionContext context, string propertyName, T defaultValue = default!) where T : notnull
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
                context.ReportDiagnostic(DiagnosticProvider.ConfigurationParseError($"'{optionValue}' is not a valid value for {PropertyNameSuffix}{propertyName} property."));
                return defaultValue;
            }
        }

        internal static string GetBuildPropertyName(string propertyName) => $"build_property.{PropertyNameSuffix}{propertyName}";

        internal static Compilation AddSource(this Compilation compilation, ref GeneratorExecutionContext context, SourceCode sourceCode)
        {
            var sourceText = SourceText.From(sourceCode.Text, Encoding.UTF8);
            context.AddSource(sourceCode.HintName, sourceText);

            // NB: https://github.com/dotnet/roslyn/issues/49753
            // To be replaced after above issue is resolved.
            var options = (CSharpParseOptions)((CSharpCompilation)compilation).SyntaxTrees[0].Options;
            return compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(sourceText, options));
        }
    }
}