using MapTo.CodeAnalysis;

namespace MapTo.Tests.SourceBuilders;

internal class TestSourceBuilderOptions
{
    private TestSourceBuilderOptions() { }

    public IDictionary<string, string> AnalyzerConfigOptions { get; } = new Dictionary<string, string>();

    public bool FileScopedNamespace { get; init; }

    public bool GenerateXmlDoc { get; init; }

    public LanguageVersion LanguageVersion { get; init; }

    public bool SupportNullableStaticAnalysis { get; init; }

    public bool SupportNullReferenceTypes { get; init; }

    public static TestSourceBuilderOptions Create(
        LanguageVersion languageVersion = LanguageVersion.CSharp11,
        bool fileScopedNamespace = true,
        bool supportNullReferenceTypes = true,
        bool generateXmlDoc = false,
        IDictionary<string, string>? analyzerConfigOptions = null)
    {
        var options = new TestSourceBuilderOptions
        {
            FileScopedNamespace = languageVersion >= LanguageVersion.CSharp10 && fileScopedNamespace,
            LanguageVersion = languageVersion,
            SupportNullReferenceTypes = supportNullReferenceTypes && languageVersion >= LanguageVersion.CSharp8,
            GenerateXmlDoc = generateXmlDoc
        };

        if (analyzerConfigOptions is not null)
        {
            foreach (var (key, value) in analyzerConfigOptions)
            {
                options.AddAnalyzerConfigOption(key, value);
            }
        }

        return options;
    }

    public void AddAnalyzerConfigOption(string key, string value) =>
        AnalyzerConfigOptions.Add(AnalyzerConfigOptionsExtensions.GetBuildPropertyName(key), value);
}