using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace MapTo.Tests.Infrastructure
{
    internal static class CSharpGenerator
    {
        internal static void ShouldBeSuccessful(this ImmutableArray<Diagnostic> diagnostics)
        {
            Assert.False(diagnostics.Any(d => d.Severity >= DiagnosticSeverity.Warning), $"Failed: {Environment.NewLine}{string.Join($"{Environment.NewLine}- ", diagnostics.Select(c => c.GetMessage()))}");
        }

        internal static (Compilation compilation, ImmutableArray<Diagnostic> diagnostics) GetOutputCompilation(string source, bool assertCompilation = false, IDictionary<string, string> analyzerConfigOptions = null)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .ToList();

            var compilation = CSharpCompilation.Create("foo", new[] { syntaxTree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            if (assertCompilation)
            {
                // NB: fail tests when the injected program isn't valid _before_ running generators
                var compileDiagnostics = compilation.GetDiagnostics();
                Assert.False(compileDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error), $"Failed: {Environment.NewLine}{string.Join($"{Environment.NewLine}- ", compileDiagnostics.Select(c => c.GetMessage()))}");
            }

            var driver = CSharpGeneratorDriver.Create(
                new[] { new MapToGenerator() },
                optionsProvider: new TestAnalyzerConfigOptionsProvider(analyzerConfigOptions)
            );

            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generateDiagnostics);
            return (outputCompilation, generateDiagnostics);
        }
    }
}