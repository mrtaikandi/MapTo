namespace MapTo.Tests.Infrastructure;

internal static class CSharpGenerator
{
    private static readonly string[] IgnoredDiagnosticIds = { "MT" };
    private static readonly IIncrementalGenerator[] SourceGenerators = { new MapToGenerator() };

    internal static CompilationResult GetOutputCompilation(
        string source,
        bool assertCompilation = false,
        IDictionary<string, string>? analyzerConfigOptions = null,
        NullableContextOptions nullableContextOptions = NullableContextOptions.Disable,
        LanguageVersion languageVersion = LanguageVersion.CSharp8,
        bool assertOutputCompilation = true) =>
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
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();

        var compilation = CSharpCompilation.Create(
            $"{typeof(CSharpGenerator).Assembly.GetName().Name}.Dynamic",
            sources.Select((s, index) => CSharpSyntaxTree.ParseText(s.Source, path: s.GetPath() ?? $"Test{index:00}.g.cs", options: new CSharpParseOptions(languageVersion))),
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: nullableContextOptions));

        if (assertCompilation)
        {
            // NB: fail tests when the injected program isn't valid _before_ running generators
            compilation.GetDiagnostics().ShouldBeSuccessful();
        }

        var driver = CSharpGeneratorDriver.Create(
            SourceGenerators.Select(GeneratorExtensions.AsSourceGenerator),
            optionsProvider: new TestAnalyzerConfigOptionsProvider(analyzerConfigOptions),
            parseOptions: new CSharpParseOptions(languageVersion));

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generateDiagnostics);

        if (assertOutputCompilation)
        {
            generateDiagnostics.ShouldBeSuccessful(ignoreDiagnosticsIds: IgnoredDiagnosticIds);
            outputCompilation.GetDiagnostics().ShouldBeSuccessful(outputCompilation);
        }

        return new(outputCompilation, generateDiagnostics);
    }
}