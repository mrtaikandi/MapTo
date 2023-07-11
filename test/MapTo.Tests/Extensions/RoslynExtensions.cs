using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.Tests.Extensions;

internal static class RoslynExtensions
{
    private const string ErrorConsoleColor = "BD2626";

    internal static ClassDeclarationSyntax? GetClassDeclaration(this Compilation compilation, string className) => compilation.SyntaxTrees
        .SelectMany(s => s.GetRoot().DescendantNodes())
        .OfType<ClassDeclarationSyntax>()
        .SingleOrDefault(c => c.Identifier.Text == className);

    internal static ClassDeclarationSyntax? GetClassDeclaration(this SyntaxTree? syntaxTree, string className) => syntaxTree?
        .GetRoot()
        .DescendantNodes()
        .OfType<ClassDeclarationSyntax>()
        .SingleOrDefault(c => c.Identifier.Text == className);

    internal static SyntaxTree? GetGeneratedFileSyntaxTree(this Compilation compilation, string fileName) =>
        compilation.SyntaxTrees.SingleOrDefault(s => s.FilePath.EndsWith($"{Path.GetFileNameWithoutExtension(fileName)}.g.cs"));

    internal static SyntaxTree GetLastSyntaxTree(this Compilation compilation) =>
        compilation.SyntaxTrees.Last();

    internal static string GetLastSyntaxTreeString(this Compilation compilation) =>
        compilation.GetLastSyntaxTree().ToString();

    internal static string? GetSyntaxTreeContentByFileName(this Compilation compilation, string fileName) =>
        compilation.GetGeneratedFileSyntaxTree(fileName)?.ToString();

    internal static string PrintSyntaxTree(
        this Compilation compilation,
        IReadOnlyCollection<(DiagnosticSeverity Severity, FileLinePositionSpan Location, string Message)>? errors = null)
    {
        var builder = new StringBuilder();
        foreach (var syntaxTree in compilation.SyntaxTrees.Reverse())
        {
            var error = errors?.FirstOrDefault(e => e.Location.Path == syntaxTree.FilePath) ?? default;

            builder
                .AppendLine("----------------------------------------")
                .Append("File Path: \"")
                .AppendErrorIf(error != default, syntaxTree.FilePath)
                .AppendLine("\"")
                .AppendLine();

            var lines = syntaxTree.ToString().Split(Environment.NewLine);
            var index = 0;
            var padding = lines.Length.ToString().Length;

            foreach (var line in lines)
            {
                var lineNumber = $"{++index:00}: ".PadLeft(padding);
                if (error != default && error.Location.StartLinePosition.Line == index - 1)
                {
                    builder.AppendError(lineNumber);
                    builder.AppendErrorLine(line, error.Location);
                }
                else
                {
                    builder.Append(lineNumber);
                    builder.AppendLine(line);
                }
            }
        }

        return builder.ToString();
    }

    internal static string ToDisplayString(this Location location)
    {
        var pos = location.GetLineSpan();
        return $"({pos.Path}@{pos.StartLinePosition.Line + 1}:{pos.StartLinePosition.Character + 1})";
    }
}