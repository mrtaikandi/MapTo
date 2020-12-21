using System;
using System.Collections.Immutable;
using System.Linq;
using MapTo;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace MapToTests
{
    internal static class CSharpGenerator
    {
        internal static string GetGeneratedOutput(this ITestOutputHelper outputHelper, string source)
        {
            var (compilation, diagnostics) = GetOutputCompilation(source);
            diagnostics.ShouldBeSuccessful();
            
            var generatedOutput = compilation.SyntaxTrees.Last().ToString();
            outputHelper.WriteLine(generatedOutput);
            
            return generatedOutput;
        }

        internal static void ShouldBeSuccessful(this ImmutableArray<Diagnostic> diagnostics)
        {
            Assert.False(diagnostics.Any(d => d.Severity >= DiagnosticSeverity.Warning), $"Failed: {Environment.NewLine}{string.Join($"{Environment.NewLine}- ", diagnostics.Select(c => c.GetMessage()))}");
        }

        internal static (Compilation compilation, ImmutableArray<Diagnostic> diagnostics) GetOutputCompilation(string source, bool assertCompilation = false)
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

            ISourceGenerator generator = new MapToGenerator();
            var driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generateDiagnostics);
            
            return (outputCompilation, generateDiagnostics);
        }
    }
}