using System;
using System.Linq;
using MapTo.Sources;
using MapTo.Tests.Extensions;
using MapTo.Tests.Infrastructure;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;
using static MapTo.Tests.Common;

namespace MapTo.Tests
{
    public class MapPropertyTests
    {
        [Theory]
        [InlineData(NullableContextOptions.Disable)]
        [InlineData(NullableContextOptions.Enable)]
        public void VerifyMapPropertyAttribute(NullableContextOptions nullableContextOptions)
        {
            // Arrange
            const string source = "";
            var nullableSyntax = nullableContextOptions == NullableContextOptions.Enable ? "?" : string.Empty;
            var expectedInterface = $@"
{Constants.GeneratedFilesHeader}
{(nullableContextOptions == NullableContextOptions.Enable ? $"#nullable enable{Environment.NewLine}" : string.Empty)}
using System;

namespace MapTo
{{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class MapPropertyAttribute : Attribute
    {{
        public string{nullableSyntax} SourcePropertyName {{ get; set; }}
    }}
}}
".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions, nullableContextOptions: nullableContextOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.ShouldContainSource(MapPropertyAttributeSource.AttributeName, expectedInterface);
        }

        [Fact]
        public void When_MapPropertyFound_Should_UseItToMapToSourceProperty()
        {
            // Arrange
            var source = GetSourceText(new SourceGeneratorOptions(
                true,
                PropertyBuilder: builder =>
                {
                    builder
                        .WriteLine("[MapProperty(SourcePropertyName = nameof(Baz.Prop3))]")
                        .WriteLine("public int Prop4 { get; set; }");
                },
                SourcePropertyBuilder: builder => builder.WriteLine("public int Prop4 { get; set; }")));

            var expectedResult = @"
    partial class Foo
    {
        public Foo(Baz baz)
        {
            if (baz == null) throw new ArgumentNullException(nameof(baz));

            Prop1 = baz.Prop1;
            Prop2 = baz.Prop2;
            Prop3 = baz.Prop3;
            Prop4 = baz.Prop3;
        }
".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ShouldContainPartialSource(expectedResult);
        }
    }
}