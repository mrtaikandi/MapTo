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
        internal static (Compilation compilation, ImmutableArray<Diagnostic> diagnostics) GetOutputCompilation(string source, bool assertCompilation = false, IDictionary<string, string> analyzerConfigOptions = null, NullableContextOptions nullableContextOptions = NullableContextOptions.Disable) =>
            GetOutputCompilation(new[] { source }, assertCompilation, analyzerConfigOptions, nullableContextOptions);
        
        internal static (Compilation compilation, ImmutableArray<Diagnostic> diagnostics) GetOutputCompilation(IEnumerable<string> sources, bool assertCompilation = false, IDictionary<string, string> analyzerConfigOptions = null, NullableContextOptions nullableContextOptions = NullableContextOptions.Disable)
        {
            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .ToList();
            
            var compilation = CSharpCompilation.Create(
                $"{typeof(CSharpGenerator).Assembly.GetName().Name}.Dynamic",
                sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, path: $"Test{index:00}.g.cs")),
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
            
            generateDiagnostics.ShouldBeSuccessful();
            outputCompilation.GetDiagnostics().ShouldBeSuccessful(outputCompilation);

            return (outputCompilation, generateDiagnostics);
        }
    }
}