using System.Text;

namespace MapTo.Tests.Extensions;

internal static class RoslynExtensions
{
    private const string ErrorConsoleColor = "BD2626";

    internal static ClassDeclarationSyntax? GetClassDeclaration(this Compilation compilation, string className, string? filePath = null)
    {
        try
        {
            return compilation.SyntaxTrees
                .Where(s => filePath is null || s.FilePath.EndsWith(filePath))
                .SelectMany(s => s.GetRoot().DescendantNodes())
                .OfType<ClassDeclarationSyntax>()
                .SingleOrDefault(c => c.Identifier.Text == className);
        }
        catch (InvalidOperationException ex)
        {
            var files = compilation.SyntaxTrees.Select(s => s.FilePath).ToArray();
            throw new InvalidOperationException(
                $"Multiple classes with the name '{className}' found. Files:{Environment.NewLine}{string.Join($",{Environment.NewLine}", files)}",
                ex);
        }
    }

    internal static ClassDeclarationSyntax? GetClassDeclaration(this SyntaxTree? syntaxTree, string className) => syntaxTree?
        .GetRoot()
        .DescendantNodes()
        .OfType<ClassDeclarationSyntax>()
        .SingleOrDefault(c => c.Identifier.Text == className);

    internal static SyntaxTree? GetGeneratedFileSyntaxTree(this Compilation compilation, string fileName)
    {
        if (!fileName.EndsWith("g.cs"))
        {
            fileName += ".g.cs";
        }

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

        return syntaxTrees.Single();
    }

    internal static SyntaxTree GetLastSyntaxTree(this Compilation compilation) =>
        compilation.SyntaxTrees.Last();

    internal static string GetLastSyntaxTreeString(this Compilation compilation) =>
        compilation.GetLastSyntaxTree().ToString();

    internal static string? GetSyntaxTreeContentByFileName(this Compilation compilation, string fileName) =>
        compilation.GetGeneratedFileSyntaxTree(fileName)?.ToString();

    internal static string PrintSyntaxTree(
        this Compilation compilation,
        IReadOnlyCollection<Diagnostic>? errors = null,
        bool excludeSuccessful = false)
    {
        var builder = new StringBuilder();
        foreach (var syntaxTree in compilation.SyntaxTrees.Reverse())
        {
            var error = errors?.FirstOrDefault(e => e.Location.GetLineSpan().Path == syntaxTree.FilePath) ?? default;
            if (excludeSuccessful && error == default)
            {
                continue;
            }

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
                var position = error?.Location.GetLineSpan();

                if (position?.StartLinePosition.Line == index - 1)
                {
                    builder.AppendError(lineNumber);
                    builder.AppendErrorLine(line, position.Value);
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