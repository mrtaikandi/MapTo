using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using DiffPlex.DiffBuilder;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.Tests.Extensions;

internal static partial class ShouldlyExtensions
{
    internal static void ShouldBe(this ClassDeclarationSyntax? classDeclaration, [StringSyntax("csharp")] string expected) =>
        Verify(classDeclaration?.ToString(), expected, "Should be equal but is not.", Assert.Equal);

    internal static void ShouldBeSuccessful(this IEnumerable<Diagnostic> diagnostics, Compilation? compilation = null, IEnumerable<string>? ignoreDiagnosticsIds = null)
    {
        var errors = diagnostics
            .Where(d => (ignoreDiagnosticsIds is null || ignoreDiagnosticsIds.All(i => !d.Id.StartsWith(i))) &&
                        d.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error)
            .Select(c => (c.Severity, Location: c.Location.GetLineSpan(), Message: c.GetMessage()))
            .ToArray();

        if (!errors.Any())
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("Failed");

        foreach (var (severity, location, message) in errors)
        {
            builder.Append($"- {severity}: {location} - {message}").AppendLine();
        }

        if (compilation is not null)
        {
            builder.AppendLine("Generated Sources:");
            builder.AppendLine(compilation.PrintSyntaxTree(errors));
        }

        Assert.Fail(builder.ToString());
    }

    internal static void ShouldContain(this ClassDeclarationSyntax? classDeclaration, [StringSyntax("csharp")] string expected)
    {
        Assert.NotNull(classDeclaration);
        var content = classDeclaration.ToString();

#pragma warning disable SA1118
        Verify(
            (a, e) => a.Contains(e),
            content.RemoveAllWhitespace(),
            expected.RemoveAllWhitespace(),
            $"{Environment.NewLine}Not found:{Environment.NewLine}{Environment.NewLine}{expected}{Environment.NewLine}" +
            $"{Environment.NewLine}In value:{Environment.NewLine}{Environment.NewLine}{content}");
#pragma warning restore SA1118
    }

    internal static void ShouldContainSource(this IEnumerable<SyntaxTree> syntaxTree, string typeName, string expectedSource, string? customMessage = null)
    {
        var syntax = syntaxTree
            .Select(s => s.ToString().Trim())
            .SingleOrDefault(s => s.Contains(typeName));

        syntax.ShouldNotBeNullOrWhiteSpace();
        syntax.ShouldBe(expectedSource, customMessage);
    }

    internal static void ShouldNotBeSuccessful(this ImmutableArray<Diagnostic> diagnostics, Diagnostic expectedError)
    {
        var actualDiagnostics = diagnostics.SingleOrDefault(d => d.Id == expectedError.Id);
        var compilationDiagnostics = actualDiagnostics == null ? diagnostics : diagnostics.Except(new[] { actualDiagnostics });

        compilationDiagnostics.ShouldBeSuccessful();

        Assert.NotNull(actualDiagnostics);
        Assert.Equal(expectedError.Id, actualDiagnostics?.Id);
        Assert.Equal(expectedError.Descriptor.Id, actualDiagnostics?.Descriptor.Id);
        Assert.Equal(expectedError.Descriptor.Description, actualDiagnostics?.Descriptor.Description);
        Assert.Equal(expectedError.Descriptor.Title, actualDiagnostics?.Descriptor.Title);

        if (expectedError.Location != Location.None)
        {
            Assert.Equal(expectedError.Location.ToDisplayString(), actualDiagnostics?.Location.ToDisplayString());
        }
    }

    internal static void ShouldNotContain(this ClassDeclarationSyntax? classDeclaration, [StringSyntax("csharp")] string expected)
    {
        classDeclaration.ShouldNotBeNull();
        classDeclaration.ToString().ShouldNotContain(expected);
    }

    private static string BuildDiff(string? actual, string? expected)
    {
        var diff = InlineDiffBuilder.Diff(actual, expected, false);
        var builder = new StringBuilder();
        builder.AppendLine();

        var lineNumber = 0;
        var padding = diff.Lines.Count.ToString().Length + 1;
        foreach (var line in diff.Lines)
        {
            builder.Append($"{++lineNumber:00}:".PadRight(padding + 1));
            builder.AppendLine(line);
        }

        return builder.ToString();
    }

    private static void Verify<TActual, TExpected>(TActual actual, TExpected expected, string failError, Action<TActual, TExpected> assertion)
    {
        try
        {
            assertion(actual, expected);
        }
        catch (Exception)
        {
            var diff = BuildDiff(actual as string ?? actual?.ToString(), expected as string ?? expected?.ToString());
            Assert.Fail($"{failError}{Environment.NewLine}{diff}");
        }
    }

    private static void Verify<TActual, TExpected>(Func<TActual, TExpected, bool> assertion, TActual actual, TExpected expected, string errorMessage)
    {
        if (!assertion(actual, expected))
        {
            Assert.Fail(errorMessage);
        }
    }

    [GeneratedRegex("\\s+")]
    private static partial Regex MatchWhitespaceRegex();

    private static string RemoveAllWhitespace(this string value) => MatchWhitespaceRegex().Replace(value, string.Empty);
}