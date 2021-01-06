using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Shouldly;

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
    }
}