using System.Linq;
using MapTo.Extensions;
using MapTo.Sources;
using MapTo.Tests.Extensions;
using MapTo.Tests.Infrastructure;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;
using static MapTo.Extensions.GeneratorExecutionContextExtensions;
using static MapTo.Tests.Common;

namespace MapTo.Tests
{
    public class MapTypeConverterTests
    {
        [Fact]
        public void VerifyTypeConverterInterface()
        {
            // Arrange
            const string source = "";
            var expectedInterface = $@"
{Constants.GeneratedFilesHeader}

namespace MapTo
{{
    public interface ITypeConverter<in TSource, out TDestination>
    {{
        TDestination Convert(TSource source, object[] converterParameters);
    }}
}}
".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.ShouldContainSource(ITypeConverterSource.InterfaceName, expectedInterface);
        }
        
        [Fact]
        public void VerifyTypeConverterInterfaceWithNullableOptionOn()
        {
            // Arrange
            const string source = "";
            var expectedInterface = $@"
{Constants.GeneratedFilesHeader}
#nullable enable

namespace MapTo
{{
    public interface ITypeConverter<in TSource, out TDestination>
    {{
        TDestination Convert(TSource source, object[]? converterParameters);
    }}
}}
".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions, nullableContextOptions: NullableContextOptions.Enable);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.ShouldContainSource(ITypeConverterSource.InterfaceName, expectedInterface);
        }
        
        [Fact]
        public void VerifyMapTypeConverterAttribute()
        {
            // Arrange
            const string source = "";
            var expectedInterface = $@"
{Constants.GeneratedFilesHeader}

using System;

namespace MapTo
{{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class MapTypeConverterAttribute : Attribute
    {{
        public MapTypeConverterAttribute(Type converter, object[] converterParameters = null)
        {{
            Converter = converter;
            ConverterParameters = converterParameters;
        }}

        public Type Converter {{ get; }}

        public object[] ConverterParameters {{ get; }}
    }}
}}
".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.ShouldContainSource(MapTypeConverterAttributeSource.AttributeName, expectedInterface);
        }
        
        [Fact]
        public void VerifyMapTypeConverterAttributeWithNullableOptionOn()
        {
            // Arrange
            const string source = "";
            var expectedInterface = $@"
{Constants.GeneratedFilesHeader}
#nullable enable

using System;

namespace MapTo
{{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class MapTypeConverterAttribute : Attribute
    {{
        public MapTypeConverterAttribute(Type converter, object[]? converterParameters = null)
        {{
            Converter = converter;
            ConverterParameters = converterParameters;
        }}

        public Type Converter {{ get; }}

        public object[]? ConverterParameters {{ get; }}
    }}
}}
".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions, nullableContextOptions: NullableContextOptions.Enable);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.ShouldContainSource(MapTypeConverterAttributeSource.AttributeName, expectedInterface);
        }

        [Fact]
        public void When_FoundMatchingPropertyNameWithDifferentImplicitlyConvertibleType_Should_GenerateTheProperty()
        {
            // Arrange
            var source = GetSourceText(new SourceGeneratorOptions(
                true,
                PropertyBuilder: builder =>
                {
                    builder
                        .PadLeft(Indent2).AppendLine("public long Prop4 { get; set; }");
                },
                SourcePropertyBuilder: builder => builder.PadLeft(Indent2).AppendLine("public int Prop4 { get; set; }")));

            var expectedResult = @"
    partial class Foo
    {
        public Foo(Test.Models.Baz baz)
        {
            if (baz == null) throw new ArgumentNullException(nameof(baz));

            Prop1 = baz.Prop1;
            Prop2 = baz.Prop2;
            Prop3 = baz.Prop3;
            Prop4 = baz.Prop4;
        }
".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ToString().ShouldContain(expectedResult);
        }

        [Fact]
        public void When_FoundMatchingPropertyNameWithIncorrectConverterType_ShouldReportError()
        {
            // Arrange
            var source = GetSourceText(new SourceGeneratorOptions(
                true,
                PropertyBuilder: builder =>
                {
                    builder
                        .PadLeft(Indent2).AppendLine("[IgnoreProperty]")
                        .PadLeft(Indent2).AppendLine("public long IgnoreMe { get; set; }")
                        .PadLeft(Indent2).AppendLine("[MapTypeConverter(typeof(Prop4Converter))]")
                        .PadLeft(Indent2).AppendLine("public long Prop4 { get; set; }");
                },
                SourcePropertyBuilder: builder => builder.PadLeft(Indent2).AppendLine("public string Prop4 { get; set; }")));

            source += @"
namespace Test
{
    using MapTo;

    public class Prop4Converter: ITypeConverter<string, int>
    {
        public int Convert(string source, object[] converterParameters) => int.Parse(source);
    }
}
";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            var expectedError = DiagnosticProvider.InvalidTypeConverterGenericTypesError(GetSourcePropertySymbol("Prop4", compilation), GetSourcePropertySymbol("Prop4", compilation, "Baz"));
            diagnostics.ShouldBeUnsuccessful(expectedError);
        }

        [Fact]
        public void When_FoundMatchingPropertyNameWithConverterType_ShouldUseTheConverterToAssignProperties()
        {
            // Arrange
            var source = GetSourceText(new SourceGeneratorOptions(
                true,
                PropertyBuilder: builder =>
                {
                    builder
                        .PadLeft(Indent2).AppendLine("[MapTypeConverter(typeof(Prop4Converter))]")
                        .PadLeft(Indent2).AppendLine("public long Prop4 { get; set; }");
                },
                SourcePropertyBuilder: builder => builder.PadLeft(Indent2).AppendLine("public string Prop4 { get; set; }")));

            source += @"
namespace Test
{
    using MapTo;

    public class Prop4Converter: ITypeConverter<string, long>
    {
        public long Convert(string source, object[] converterParameters) => long.Parse(source);
    }
}
";

            const string expectedSyntax = "Prop4 = new Test.Prop4Converter().Convert(baz.Prop4, null);";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ToString().ShouldContain(expectedSyntax);
        }

        [Fact]
        public void When_FoundMatchingPropertyNameWithConverterType_ShouldUseTheConverterAndItsParametersToAssignProperties()
        {
            // Arrange
            var source = GetSourceText(new SourceGeneratorOptions(
                true,
                PropertyBuilder: builder =>
                {
                    builder
                        .PadLeft(Indent2).AppendLine("[MapTypeConverter(typeof(Prop4Converter), new object[]{\"G\", 'C', 10})]")
                        .PadLeft(Indent2).AppendLine("public string Prop4 { get; set; }");
                },
                SourcePropertyBuilder: builder => builder.PadLeft(Indent2).AppendLine("public long Prop4 { get; set; }")));

            source += @"
namespace Test
{
    using MapTo;

    public class Prop4Converter: ITypeConverter<long, string>
    {
        public string Convert(long source, object[] converterParameters) => source.ToString(converterParameters[0] as string);
    }
}
";

            const string expectedSyntax = "Prop4 = new Test.Prop4Converter().Convert(baz.Prop4, new object[] { \"G\", 'C', 10 });";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ToString().ShouldContain(expectedSyntax);
        }
    }
}