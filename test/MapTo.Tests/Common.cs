using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapTo.Extensions;
using MapTo.Sources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.Tests
{
    internal static class Common
    {
        internal const int Indent1 = 4;
        internal const int Indent2 = Indent1 * 2;
        internal const int Indent3 = Indent1 * 3;
        internal static readonly Location IgnoreLocation = Location.None;

        internal static readonly Dictionary<string, string> DefaultAnalyzerOptions = new()
        {
            [GeneratorExecutionContextExtensions.GetBuildPropertyName(nameof(SourceGenerationOptions.GenerateXmlDocument))] = "false"
        };

        internal static string GetSourceText(SourceGeneratorOptions options = null)
        {
            const string ns = "Test";
            options ??= new SourceGeneratorOptions();
            var hasDifferentSourceNamespace = options.SourceClassNamespace != ns;
            var builder = new StringBuilder();

            builder.AppendLine("//");
            builder.AppendLine("// Test source code.");
            builder.AppendLine("//");
            builder.AppendLine();

            if (options.UseMapToNamespace)
            {
                builder.AppendFormat("using {0};", Constants.RootNamespace).AppendLine();
            }

            builder
                .AppendFormat("using {0};", options.SourceClassNamespace)
                .AppendLine()
                .AppendLine();

            builder
                .AppendFormat("namespace {0}", ns)
                .AppendOpeningBracket();

            if (hasDifferentSourceNamespace && options.UseMapToNamespace)
            {
                builder
                    .PadLeft(Indent1)
                    .AppendFormat("using {0};", options.SourceClassNamespace)
                    .AppendLine()
                    .AppendLine();
            }

            builder
                .PadLeft(Indent1)
                .AppendLine(options.UseMapToNamespace ? "[MapFrom(typeof(Baz))]" : "[MapTo.MapFrom(typeof(Baz))]")
                .PadLeft(Indent1).Append("public partial class Foo")
                .AppendOpeningBracket(Indent1);

            for (var i = 1; i <= options.ClassPropertiesCount; i++)
            {
                builder
                    .PadLeft(Indent2)
                    .AppendLine(i % 2 == 0 ? $"public int Prop{i} {{ get; set; }}" : $"public int Prop{i} {{ get; }}");
            }

            options.PropertyBuilder?.Invoke(builder);

            builder
                .AppendClosingBracket(Indent1, false)
                .AppendClosingBracket()
                .AppendLine()
                .AppendLine();

            builder
                .AppendFormat("namespace {0}", options.SourceClassNamespace)
                .AppendOpeningBracket()
                .PadLeft(Indent1).Append("public class Baz")
                .AppendOpeningBracket(Indent1);

            for (var i = 1; i <= options.SourceClassPropertiesCount; i++)
            {
                builder
                    .PadLeft(Indent2)
                    .AppendLine(i % 2 == 0 ? $"public int Prop{i} {{ get; set; }}" : $"public int Prop{i} {{ get; }}");
            }

            options.SourcePropertyBuilder?.Invoke(builder);

            builder
                .AppendClosingBracket(Indent1, false)
                .AppendClosingBracket();

            return builder.ToString();
        }

        internal static PropertyDeclarationSyntax GetPropertyDeclarationSyntax(SyntaxTree syntaxTree, string targetPropertyName, string targetClass = "Foo")
        {
            return syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Single(c => c.Identifier.ValueText == targetClass)
                .DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Single(p => p.Identifier.ValueText == targetPropertyName);
        }

        internal static IPropertySymbol GetSourcePropertySymbol(string propertyName, Compilation compilation, string targetClass = "Foo")
        {
            var syntaxTree = compilation.SyntaxTrees.First();
            var propSyntax = GetPropertyDeclarationSyntax(syntaxTree, propertyName, targetClass);

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            return semanticModel.GetDeclaredSymbol(propSyntax);
        }

        internal record SourceGeneratorOptions(
            bool UseMapToNamespace = false,
            string SourceClassNamespace = "Test.Models",
            int ClassPropertiesCount = 3,
            int SourceClassPropertiesCount = 3,
            Action<StringBuilder> PropertyBuilder = null,
            Action<StringBuilder> SourcePropertyBuilder = null);
    }
}