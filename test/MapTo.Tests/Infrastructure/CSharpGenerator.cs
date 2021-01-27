using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MapTo.Tests.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace MapTo.Tests.Infrastructure
{
    internal static class CSharpGenerator
    {
        internal static (Compilation compilation, ImmutableArray<Diagnostic> diagnostics) GetOutputCompilation(string source, bool assertCompilation = false, IDictionary<string, string> analyzerConfigOptions = null, NullableContextOptions nullableContextOptions = NullableContextOptions.Disable)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .ToList();

            var compilation = CSharpCompilation.Create(
                $"{typeof(CSharpGenerator).Assembly.GetName().Name}.Dynamic",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: nullableContextOptions));

            if (assertCompilation)
            {
                // NB: fail tests when the injected program isn't valid _before_ running generators
                compilation.GetDiagnostics().ShouldBeSuccessful();
            }
            
            var driver = CSharpGeneratorDriver.Create(
                new[] { new MapToGenerator() },
                optionsProvider: new TestAnalyzerConfigOptionsProvider(analyzerConfigOptions)
            );

            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generateDiagnostics);

            var diagnostics = outputCompilation.GetDiagnostics()
                .Where(d => d.Severity >= DiagnosticSeverity.Warning)
                .Select(c => $"{c.Severity}: {c.Location.GetLineSpan().StartLinePosition} - {c.GetMessage()} [in \"{c.Location.SourceTree?.FilePath}\"]").ToArray();

            if (diagnostics.Any())
            {
                Assert.False(diagnostics.Any(), $@"Failed: 
{string.Join(Environment.NewLine, diagnostics.Select(c => $"- {c}"))}

Generated Sources:
{string.Join(Environment.NewLine, outputCompilation.SyntaxTrees.Reverse().Select(s => $"----------------------------------------{Environment.NewLine}File Path: \"{s.FilePath}\"{Environment.NewLine}{s}"))}
");
            }

            return (outputCompilation, generateDiagnostics);
        }
    }
}