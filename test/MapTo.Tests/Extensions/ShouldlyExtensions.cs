using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace MapTo.Tests.Extensions
{
    internal static class ShouldlyExtensions
    {
        internal static void ShouldContainSource(this IEnumerable<SyntaxTree> syntaxTree, string typeName, string expectedSource, string customMessage = null)
        {
            var syntax = syntaxTree
                .Select(s => s.ToString().Trim())
                .SingleOrDefault(s => s.Contains(typeName));

            syntax.ShouldNotBeNullOrWhiteSpace();
            syntax.ShouldBe(expectedSource, customMessage);
        }
        
        internal static void ShouldBeSuccessful(this IEnumerable<Diagnostic> diagnostics, DiagnosticSeverity severity = DiagnosticSeverity.Warning)
        {
            var actual = diagnostics.Where(d => d.Severity >= severity).Select(c => $"{c.Severity}: {c.Location.GetLineSpan().StartLinePosition} - {c.GetMessage()}").ToArray();
            Assert.False(actual.Any(), $"Failed: {Environment.NewLine}{string.Join(Environment.NewLine, actual.Select(c => $"- {c}"))}");
        }

        internal static void ShouldBeUnsuccessful(this ImmutableArray<Diagnostic> diagnostics, Diagnostic expectedError)
        {
            var actualDiagnostics = diagnostics.SingleOrDefault(d => d.Id == expectedError.Id);
            var compilationDiagnostics = actualDiagnostics == null ? diagnostics : diagnostics.Except(new[] { actualDiagnostics });
            
            compilationDiagnostics.ShouldBeSuccessful();
            
            Assert.NotNull(actualDiagnostics);
            Assert.Equal(expectedError.Id, actualDiagnostics.Id);
            Assert.Equal(expectedError.Descriptor.Id, actualDiagnostics.Descriptor.Id);
            Assert.Equal(expectedError.Descriptor.Description, actualDiagnostics.Descriptor.Description);
            Assert.Equal(expectedError.Descriptor.Title, actualDiagnostics.Descriptor.Title);
            
            if (expectedError.Location != Location.None)
            {
                Assert.Equal(expectedError.Location, actualDiagnostics.Location);
            }
        }
    }
}