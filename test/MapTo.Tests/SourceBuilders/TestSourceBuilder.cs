﻿using System.Diagnostics.CodeAnalysis;
using MapTo.Generators;

namespace MapTo.Tests.SourceBuilders;

internal class TestSourceBuilder : ITestSourceBuilder
{
    private readonly List<ITestFileBuilder> _files = new();

    internal TestSourceBuilder(bool supportNullReferenceTypes, IDictionary<string, string>? analyzerConfigOptions)
        : this(TestSourceBuilderOptions.Create(supportNullReferenceTypes: supportNullReferenceTypes, analyzerConfigOptions: analyzerConfigOptions)) { }

    internal TestSourceBuilder(TestSourceBuilderOptions? options = null)
    {
        Options = options ?? TestSourceBuilderOptions.Create();
        Sources = ImmutableArray<TestSource>.Empty;
    }

    public int Count => _files.Count;

    public TestSourceBuilderOptions Options { get; }

    public ImmutableArray<TestSource> Sources { get; private set; }

    public void AddFile(ITestFileBuilder file) => _files.Add(file);

    /// <inheritdoc />
    public ImmutableArray<TestSource> Build()
    {
        var sources = new List<TestSource>();

        foreach (var file in _files)
        {
            var writer = new CodeWriter();
            file.Build(writer, Options);
            sources.Add(new TestSource(file.Name, writer.ToString(), Options.LanguageVersion, file.AutoGenerated));
        }

        return Sources = [..sources];
    }
}

[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Wrongly reported by StyleCop.")]
internal static class TestSourceBuilderExtensions
{
    public static string? GetPath(this TestSource source)
    {
        if (string.IsNullOrWhiteSpace(source.FileName))
        {
            return null;
        }

        var fileName = Path.GetFileName(source.FileName);
        return Path.HasExtension(fileName) ? fileName : $"{Path.GetFileNameWithoutExtension(fileName)}.g.cs";
    }

    public static TestSource GetSource(this ITestSourceBuilder builder, ITestFileBuilder fileBuilder) =>
        builder.Sources.GetSource(fileBuilder.Name);

    public static TestSource GetSource(this ImmutableArray<TestSource> sources, string fileName) =>
        sources.Single(s => s.FileName == fileName);

    public static string GetSourceContent(this ITestSourceBuilder builder, ITestFileBuilder fileBuilder) =>
        builder.GetSource(fileBuilder).Source;
}