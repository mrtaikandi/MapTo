using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using DiffPlex.DiffBuilder;
using Xunit.Sdk;

namespace MapTo.Tests.Extensions;

internal static partial class ShouldlyExtensions
{
    internal static void ShouldBe(this ClassDeclarationSyntax? classDeclaration, [StringSyntax("csharp")] string expected) =>
        Verify(classDeclaration?.ToString(), expected, "Should be equal but is not.", Assert.Equal);

    internal static void ShouldBeSuccessful(this Compilation compilation, IEnumerable<string>? ignoreDiagnosticsIds = null) =>
        compilation.GetDiagnostics().ShouldBeSuccessful(compilation, ignoreDiagnosticsIds);

    internal static void ShouldBeSuccessful(this IEnumerable<Diagnostic> diagnostics, Compilation? compilation = null, IEnumerable<string>? ignoreDiagnosticsIds = null)
    {
        var errors = diagnostics
            .Where(d => (ignoreDiagnosticsIds is null || ignoreDiagnosticsIds.All(i => !d.Id.StartsWith(i))) &&
                        d.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error)
            .ToArray();

        if (!errors.Any())
        {
            return;
        }

        static string GetLocation(Diagnostic e)
        {
            var l = e.Location.GetLineSpan();
            return $"{l.Path}: ({l.StartLinePosition.Line + 1}, {l.StartLinePosition.Character}) - ({l.EndLinePosition.Line + 1}, {l.EndLinePosition.Character})";
        }

        var builder = new StringBuilder();
        builder.AppendLine();
        builder.AppendLine();

        foreach (var error in errors)
        {
            switch (error.Severity)
            {
                case DiagnosticSeverity.Info:
                    builder.Append("- ❕ ").Append(GetLocation(error));
                    break;

                case DiagnosticSeverity.Warning:
                    builder.Append("- ⚠️ ").Append(GetLocation(error), AnsiColor.Warning);
                    break;

                default:
                    builder.Append("- ❌ ").Append(GetLocation(error), AnsiColor.Error);
                    break;
            }

            builder.Append($" - {error.Id}: {error.GetMessage()}").AppendLine();
        }

        if (compilation is not null)
        {
            builder.AppendLine("Generated Sources:");
            builder.AppendLine(compilation.PrintSyntaxTree(errors, excludeSuccessful: true));
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

    internal static void ShouldContainSourceFile(this Compilation compilation, string fileName, [StringSyntax("csharp")] string expectedSource)
    {
        var syntaxTrees = compilation.SyntaxTrees.Where(s => s.FilePath.EndsWith(fileName)).ToList();
        if (syntaxTrees.Count != 1)
        {
            var builder = new StringBuilder();
            builder
                .AppendErrorLine($"Expected to find exactly one syntax tree with the '{fileName}' file path but found {syntaxTrees.Count}.", 51, 51 + fileName.Length)
                .AppendLine("Syntax Trees:")
                .AppendLines(compilation.SyntaxTrees.Select(s => $"- {s.FilePath}"));

            Assert.Fail(builder.ToString());
        }

        var syntax = syntaxTrees.Single().ToString().Trim();
        Verify(syntax, expectedSource, $"Expected to find the following source in the '{fileName}' file path but found a difference.", Assert.Equal);
    }

    internal static void ShouldNotBeSuccessful(this ImmutableArray<Diagnostic> diagnostics, Diagnostic expectedError, IEnumerable<string>? ignoreDiagnosticsIds = null)
    {
        var actualDiagnostics = diagnostics.SingleOrDefault(d => d.Id == expectedError.Id);
        var compilationDiagnostics = actualDiagnostics == null ? diagnostics : diagnostics.Except(new[] { actualDiagnostics });

        compilationDiagnostics.ShouldBeSuccessful(ignoreDiagnosticsIds: ignoreDiagnosticsIds);

        if (actualDiagnostics is null)
        {
            throw new XunitException($"Expected to find a diagnostic with the id '{expectedError.Id}' but found none. Expected diagnostic:" +
                                     $"{Environment.NewLine}{Environment.NewLine}" +
                                     $"[{expectedError.Severity}] {expectedError.Location.GetLineSpan()} - {expectedError.Id}: {expectedError.GetMessage()}" +
                                     $"{Environment.NewLine}");
        }

        Assert.Equal(expectedError.Id, actualDiagnostics.Id);
        Assert.Equal(expectedError.Descriptor.Id, actualDiagnostics.Descriptor.Id);
        Assert.Equal(expectedError.Descriptor.MessageFormat, actualDiagnostics.Descriptor.MessageFormat);
        Assert.Equal(expectedError.Descriptor.Title, actualDiagnostics.Descriptor.Title);
        Assert.Equal(expectedError.Location.ToDisplayString(), actualDiagnostics.Location.ToDisplayString());
    }

    internal static void ShouldNotBeSuccessful(this ImmutableArray<Diagnostic> diagnostics, string diagnosticId, string? diagnosticMessage = null)
    {
        var actualDiagnostics = diagnostics.SingleOrDefault(d => d.Id == diagnosticId);
        if (actualDiagnostics is null)
        {
            throw new XunitException($"Expected to find a diagnostic with the id '{diagnosticId}' but found none.");
        }

        Assert.Equal(diagnosticId, actualDiagnostics.Id);

        if (diagnosticMessage is not null)
        {
            Assert.Equal(diagnosticMessage, actualDiagnostics.GetMessage());
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