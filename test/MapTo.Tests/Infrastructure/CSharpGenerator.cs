using System.Reflection;

namespace MapTo.Tests.Infrastructure;

internal static class CSharpGenerator
{
    private static readonly string[] IgnoredDiagnosticIds = ["MT"];
    private static readonly IIncrementalGenerator[] SourceGenerators = [new MapToGenerator()];

    private static readonly string[] SdkAssemblies =
    [
        "Microsoft.CSharp",
        "netstandard",
        "System.Collections",
        "System.Collections.Concurrent",
        "System.Collections.Immutable",
        "System.Collections.NonGeneric",
        "System.ComponentModel",
        "System.ComponentModel.Primitives",
        "System.ComponentModel.TypeConverter",
        "System.Console",
        "System.Diagnostics.Debug",
        "System.Diagnostics.Process",
        "System.Diagnostics.TraceSource",
        "System.Diagnostics.Tracing",
        "System.Globalization",
        "System.IO",
        "System.IO.FileSystem",
        "System.Linq",
        "System.Linq.Expressions",
        "System.Linq.Queryable",
        "System.Memory",
        "System.Net.Primitives",
        "System.Net.Sockets",
        "System.ObjectModel",
        "System.Private.CoreLib",
        "System.Private.Uri",
        "System.Reflection",
        "System.Reflection.Emit",
        "System.Reflection.Emit.ILGeneration",
        "System.Reflection.Emit.Lightweight",
        "System.Reflection.Extensions",
        "System.Reflection.Metadata",
        "System.Reflection.Primitives",
        "System.Runtime",
        "System.Runtime.Extensions",
        "System.Runtime.InteropServices",
        "System.Runtime.Intrinsics",
        "System.Runtime.Loader",
        "System.Runtime.Numerics",
        "System.Runtime.Serialization.Formatters",
        "System.Text.Encoding.Extensions",
        "System.Text.RegularExpressions",
        "System.Threading",
        "System.Threading.Tasks",
        "System.Threading.Thread",
        "System.Threading.ThreadPool"
    ];

    private static readonly string[] MapToAssemblies = ["MapTo", "MapTo.Abstractions"];

    internal static CompilationResult GetOutputCompilation(
        string source,
        bool assertCompilation = false,
        IDictionary<string, string>? analyzerConfigOptions = null,
        NullableContextOptions nullableContextOptions = NullableContextOptions.Disable,
        LanguageVersion languageVersion = LanguageVersion.CSharp8) =>
        GetOutputCompilation(
            new[] { new TestSource(string.Empty, source, languageVersion, true) },
            assertCompilation,
            analyzerConfigOptions,
            nullableContextOptions,
            languageVersion,
            assertCompilation);

    internal static CompilationResult GetOutputCompilation(
        IEnumerable<TestSource> sources,
        bool assertCompilation = false,
        IDictionary<string, string>? analyzerConfigOptions = null,
        NullableContextOptions nullableContextOptions = NullableContextOptions.Disable,
        LanguageVersion languageVersion = LanguageVersion.CSharp8,
        bool assertOutputCompilation = true)
    {
        var sdkDir = Path.GetDirectoryName(typeof(object).Assembly.Location) ?? throw new InvalidOperationException("Could not find SDK directory");
        var workingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException("Could not find working directory");
        var references = SdkAssemblies.Select(s => Path.Combine(sdkDir, $"{s}.dll"))
            .Union(MapToAssemblies.Select(s => Path.Combine(workingDir, $"{s}.dll")))
            .Select(s => MetadataReference.CreateFromFile(s));

        var compilation = CSharpCompilation.Create(
            $"{typeof(CSharpGenerator).Assembly.GetName().Name}.Dynamic",
            sources.Select((s, index) => CSharpSyntaxTree.ParseText(s.Source, path: s.GetPath() ?? $"Test{index:00}.g.cs", options: new CSharpParseOptions(languageVersion))),
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: nullableContextOptions));

        if (assertCompilation)
        {
            // NB: fail tests when the injected program isn't valid _before_ running generators
            compilation.ShouldBeSuccessful();
        }

        var driver = CSharpGeneratorDriver.Create(
            SourceGenerators.Select(GeneratorExtensions.AsSourceGenerator),
            optionsProvider: new TestAnalyzerConfigOptionsProvider(analyzerConfigOptions),
            parseOptions: new CSharpParseOptions(languageVersion));

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generateDiagnostics);

        if (assertOutputCompilation)
        {
            var compilationDiagnostics = outputCompilation.GetDiagnostics();
            var suppressedErrors = compilationDiagnostics.All(c => c.Severity is not DiagnosticSeverity.Error) ? IgnoredDiagnosticIds : [];

            generateDiagnostics.Union(compilationDiagnostics)
                .Where(d => suppressedErrors.All(i => !d.Id.StartsWith(i)))
                .ShouldBeSuccessful(outputCompilation);
        }

        return new(outputCompilation, generateDiagnostics);
    }
}