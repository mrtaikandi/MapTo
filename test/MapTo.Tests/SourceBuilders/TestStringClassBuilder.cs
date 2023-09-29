using System.Diagnostics.CodeAnalysis;
using MapTo.Generators;

namespace MapTo.Tests.SourceBuilders;

internal record TestStringClassBuilder(string Body) : ITestStringClassBuilder
{
    /// <inheritdoc />
    public void Build(CodeWriter writer, TestSourceBuilderOptions options)
    {
        writer.WriteLines(Body.Split(writer.NewLine));
    }
}

internal static class TestStringClassBuilderExtensions
{
    public static ITestFileBuilder AddClass(this ITestFileBuilder builder, [StringSyntax("csharp")] string body) =>
        builder.AddClass(new TestStringClassBuilder(body));

    public static ITestFileBuilder AddClass(this ITestFileBuilder builder, TestStringClassBuilder classBuilder)
    {
        builder.AddMember(classBuilder);
        return builder;
    }
}