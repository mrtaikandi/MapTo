﻿using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using DiffPlex.Chunkers;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Xunit.Sdk;

namespace MapTo.Tests.Extensions;

internal static partial class ShouldlyExtensions
{
    private static readonly string EndOfLine = Environment.NewLine;

    internal static void ShouldBe(this CSharpSyntaxNode? declarationSyntax, [StringSyntax("csharp")] string expected) =>
        Verify(declarationSyntax?.NormalizeWhitespace(eol: EndOfLine).ToString() ?? string.Empty, expected, "Should be equal but is not.", Assert.Equal, false);

    internal static void ShouldBe(this CSharpSyntaxNode? declarationSyntax, bool ignoreWhitespace, [StringSyntax("csharp")] string expected) =>
        Verify(declarationSyntax?.NormalizeWhitespace(eol: EndOfLine).ToString() ?? string.Empty, expected, "Should be equal but is not.", Assert.Equal, ignoreWhitespace);

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

    internal static void ShouldContain(this MemberDeclarationSyntax? declarationSyntax, [StringSyntax("csharp")] string expected)
    {
        Assert.NotNull(declarationSyntax);
        var content = declarationSyntax.ToString();

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
        Verify(syntax, expectedSource, $"Expected to find the following source in the '{fileName}' file path but found a difference.", Assert.Equal, false);
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
        Assert.Equal(expectedError.GetMessage(), actualDiagnostics.GetMessage());

        var actualMessage = actualDiagnostics.GetMessage();
        if (actualMessage.Contains("''") || actualMessage.Contains("\"\""))
        {
            Assert.Fail($"Expected diagnostic message to be equal but found a difference.{Environment.NewLine}" +
                        $"Expected:{Environment.NewLine}{expectedError.GetMessage()}{Environment.NewLine}" +
                        $"Actual:{Environment.NewLine}{actualMessage}");
        }
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
        var diff = InlineDiffBuilder.Diff(actual, expected, false, false, new LineChunker());
        var builder = new StringBuilder();
        builder.AppendLine();

        var lineNumber = 0;
        var padding = diff.Lines.Count.ToString().Length + 1;
        for (var index = 0; index < diff.Lines.Count; index++)
        {
            var line = diff.Lines[index];
            builder.Append($"{++lineNumber:00}:".PadRight(padding + 1));

            WriteDiff(builder, diff, line, index);
        }

        return builder.ToString();

        static void WriteDiff(StringBuilder builder, DiffPaneModel diff, DiffPiece line, int index)
        {
            switch (line.Type)
            {
                case ChangeType.Deleted:
                    builder.Append("A ", AnsiColor.Error);
                    var subDiff = InlineDiffBuilder.Diff(line.Text, diff.Lines[index + 1].Text, false, false, new WordChunker());

                    foreach (var character in subDiff.Lines)
                    {
                        switch (character.Type)
                        {
                            case ChangeType.Deleted:
                                builder.Append(character.Text, AnsiColor.Error);
                                break;

                            case ChangeType.Inserted:
                                break;

                            default:
                                builder.Append(character.Text);
                                break;
                        }
                    }

                    builder.AppendLine();

                    break;

                case ChangeType.Inserted:
                    builder.Append("E ", AnsiColor.Success).AppendLine(line.Text);
                    break;

                default:
                    builder.Append("  ").AppendLine(line.Text);
                    break;
            }
        }
    }

    private static void Verify(string actual, string expected, string failError, Action<string, string> assertion, bool ignoreWhitespace)
    {
        try
        {
            if (ignoreWhitespace)
            {
                assertion(actual.RemoveAllWhitespace(), expected.RemoveAllWhitespace());
            }
            else
            {
                assertion(actual, expected);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Assert.Fail(
                $"""
                 {failError}

                 {BuildDiff(actual, expected)}

                 ---------------------------
                 Actual:

                 {actual}

                 Expected:

                 {expected}

                 """);
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