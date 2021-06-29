using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace MapTo.Tests.Extensions
{
    internal static class ShouldlyExtensions
    {
        internal static void ShouldContainSource(this IEnumerable<SyntaxTree> syntaxTree, string typeName, string expectedSource, string? customMessage = null)
        {
            var syntax = syntaxTree
                .Select(s => s.ToString().Trim())
                .SingleOrDefault(s => s.Contains(typeName));

            syntax.ShouldNotBeNullOrWhiteSpace();
            syntax.ShouldBe(expectedSource, customMessage);
        }
        
        internal static void ShouldContainPartialSource(this IEnumerable<SyntaxTree> syntaxTree, string typeName, string expectedSource, string? customMessage = null)
        {
            var syntax = syntaxTree
                .Select(s => s.ToString().Trim())
                .SingleOrDefault(s => s.Contains(typeName));
            
            syntax.ShouldNotBeNullOrWhiteSpace();
            syntax.ShouldContainWithoutWhitespace(expectedSource, customMessage);
        }
        
        internal static void ShouldContainPartialSource(this SyntaxTree syntaxTree, string expectedSource, string? customMessage = null)
        {
            var syntax = syntaxTree.ToString();
            syntax.ShouldNotBeNullOrWhiteSpace();
            syntax.ShouldContainWithoutWhitespace(expectedSource, customMessage);
        }
        
        internal static void ShouldBeSuccessful(this IEnumerable<Diagnostic> diagnostics, Compilation? compilation = null, IEnumerable<string>? ignoreDiagnosticsIds = null)
        {
            var actual = diagnostics
                .Where(d => (ignoreDiagnosticsIds is null || ignoreDiagnosticsIds.All(i => !d.Id.StartsWith(i) )) && (d.Severity == DiagnosticSeverity.Warning || d.Severity == DiagnosticSeverity.Error))
                .Select(c => $"{c.Severity}: {c.Location.GetLineSpan()} - {c.GetMessage()}").ToArray();

            if (!actual.Any())
            {
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("Failed");

            foreach (var d in actual)
            {
                builder.AppendFormat("- {0}", d).AppendLine();
            }

            if (compilation is not null)
            {
                builder.AppendLine("Generated Sources:");
                builder.AppendLine(compilation.PrintSyntaxTree());
            }
            
            Assert.False(true, builder.ToString());
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
                Assert.Equal(expectedError.Location, actualDiagnostics?.Location);
            }
        }
    }
}