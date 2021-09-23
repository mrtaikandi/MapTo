using System.Linq;
using MapTo.Extensions;
using MapTo.Sources;
using MapTo.Tests.Extensions;
using MapTo.Tests.Infrastructure;
using Shouldly;
using Xunit;
using static MapTo.Tests.Common;

namespace MapTo.Tests
{
    public class IgnorePropertyAttributeTests
    {
        [Fact]
        public void VerifyIgnorePropertyAttribute()
        {
            // Arrange
            const string source = "";
            var expectedAttribute = $@"
{Constants.GeneratedFilesHeader}
using System;

namespace MapTo
{{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public sealed class IgnorePropertyAttribute : Attribute
    {{
        public Type SourceTypeName {{ get; set; }}
    }}
}}
".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.ShouldContainSource(IgnorePropertyAttributeSource.AttributeName, expectedAttribute);
        }

        [Fact]
        public void When_IgnorePropertyAttributeIsSpecified_Should_NotGenerateMappingsForThatProperty()
        {
            // Arrange
            var source = GetSourceText(new SourceGeneratorOptions(
                true,
                PropertyBuilder: builder =>
                {
                    builder
                        .WriteLine("[IgnoreProperty]")
                        .WriteLine("public int Prop4 { get; set; }");
                },
                SourcePropertyBuilder: builder => builder.WriteLine("public int Prop4 { get; set; }")));

            var expectedResult = @"
    partial class Foo
    {
        public Foo(Test.Models.Baz baz)
            : this(new MappingContext(), baz) { }

        private protected Foo(MappingContext context, Test.Models.Baz baz)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (baz == null) throw new ArgumentNullException(nameof(baz));

            context.Register(baz, this);

            Prop1 = baz.Prop1;
            Prop2 = baz.Prop2;
            Prop3 = baz.Prop3;
        }
".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ShouldContainPartialSource(expectedResult);
        }

        [Fact]
        public void When_TypedIgnorePropertyAttributeIsSpecified_Should_NotGenerateMappingsForThatProperty()
        {
            // Arrange
            var source = GetSourceText(new SourceGeneratorOptions(
                true,
                PropertyBuilder: builder =>
                {
                    builder
                        .WriteLine("[IgnoreProperty(SourceTypeName = typeof(Test.Models.Baz))]")
                        .WriteLine("public int Prop4 { get; set; }")
                        .WriteLine("[IgnoreProperty(SourceTypeName = typeof(Foo))]")
                        .WriteLine("public int Prop5 { get; set; }");
                },
                SourcePropertyBuilder: builder => builder
                                                    .WriteLine("public int Prop4 { get; set; }")
                                                    .WriteLine("public int Prop5 { get; set; }")));

            var expectedResult = @"
    partial class Foo
    {
        public Foo(Test.Models.Baz baz)
            : this(new MappingContext(), baz) { }

        private protected Foo(MappingContext context, Test.Models.Baz baz)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (baz == null) throw new ArgumentNullException(nameof(baz));

            context.Register(baz, this);

            Prop1 = baz.Prop1;
            Prop2 = baz.Prop2;
            Prop3 = baz.Prop3;
            Prop5 = baz.Prop5;
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