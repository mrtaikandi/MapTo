using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace MapTo.Tests.Extensions
{
    internal static class RoslynExtensions
    {
        internal static SyntaxTree GetGeneratedSyntaxTree(this Compilation compilation, string className) => 
            compilation.SyntaxTrees.SingleOrDefault(s => s.FilePath.EndsWith($"{className}.g.cs"));

        internal static string PrintSyntaxTree(this Compilation compilation)
        {
            var builder = new StringBuilder();
            
            return string.Join(
                Environment.NewLine,
                compilation.SyntaxTrees
                    .Reverse()
                    .Select((s, i) =>
                    {
                        builder
                            .Clear()
                            .AppendLine("----------------------------------------")
                            .AppendFormat("File Path: \"{0}\"", s.FilePath).AppendLine()
                            .AppendFormat("Index: \"{0}\"", i).AppendLine()
                            .AppendLine();

                        var lines = s.ToString().Split(Environment.NewLine);
                        var lineNumber = 1;
                        foreach (var line in lines)
                        {
                            builder.AppendFormat("{0:00}: {1}", lineNumber, line).AppendLine();
                            lineNumber++;
                        }

                        return builder.ToString();
                    }));
        }
    }
}