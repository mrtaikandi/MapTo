using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapTo.Extensions;
using MapTo.Sources;
using MapTo.Tests.Extensions;
using MapTo.Tests.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;
using Xunit;
using static MapTo.Extensions.GeneratorExecutionContextExtensions;

namespace MapTo.Tests
{
    public class Tests
    {
        private const int Indent1 = 4;
        private const int Indent2 = Indent1 * 2;
        private const int Indent3 = Indent1 * 3;
        private static readonly Location IgnoreLocation = Location.None;

        private static readonly Dictionary<string, string> DefaultAnalyzerOptions = new()
        {
            [GetBuildPropertyName(nameof(SourceGenerationOptions.GenerateXmlDocument))] = "false"
        };

        private static readonly string ExpectedAttribute = $@"{Constants.GeneratedFilesHeader}
using System;

namespace MapTo
{{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class MapFromAttribute : Attribute
    {{
        public MapFromAttribute(Type sourceType)
        {{
            SourceType = sourceType;
        }}

        public Type SourceType {{ get; }}
    }}
}}";

        private record SourceGeneratorOptions(
            bool UseMapToNamespace = false,
            string SourceClassNamespace = "Test.Models",
            int ClassPropertiesCount = 3,
            int SourceClassPropertiesCount = 3,
            Action<StringBuilder> PropertyBuilder = null,
            Action<StringBuilder> SourcePropertyBuilder = null);

        private static string GetSourceText(SourceGeneratorOptions options = null)
        {
            const string ns = "Test";
            options ??= new SourceGeneratorOptions();
            var hasDifferentSourceNamespace = options.SourceClassNamespace != ns;
            var builder = new StringBuilder();

            builder.AppendLine("//");
            builder.AppendLine("// Test source code.");
            builder.AppendLine("//");
            builder.AppendLine();

            if (options.UseMapToNamespace)
            {
                builder.AppendFormat("using {0};", Constants.RootNamespace).AppendLine();
            }

            builder
                .AppendFormat("using {0};", options.SourceClassNamespace)
                .AppendLine()
                .AppendLine();

            builder
                .AppendFormat("namespace {0}", ns)
                .AppendOpeningBracket();

            if (hasDifferentSourceNamespace && options.UseMapToNamespace)
            {
                builder
                    .PadLeft(Indent1)
                    .AppendFormat("using {0};", options.SourceClassNamespace)
                    .AppendLine()
                    .AppendLine();
            }

            builder
                .PadLeft(Indent1)
                .AppendLine(options.UseMapToNamespace ? "[MapFrom(typeof(Baz))]" : "[MapTo.MapFrom(typeof(Baz))]")
                .PadLeft(Indent1).Append("public partial class Foo")
                .AppendOpeningBracket(Indent1);

            for (var i = 1; i <= options.ClassPropertiesCount; i++)
            {
                builder
                    .PadLeft(Indent2)
                    .AppendLine(i % 2 == 0 ? $"public int Prop{i} {{ get; set; }}" : $"public int Prop{i} {{ get; }}");
            }

            options.PropertyBuilder?.Invoke(builder);

            builder
                .AppendClosingBracket(Indent1, false)
                .AppendClosingBracket()
                .AppendLine()
                .AppendLine();

            builder
                .AppendFormat("namespace {0}", options.SourceClassNamespace)
                .AppendOpeningBracket()
                .PadLeft(Indent1).Append("public class Baz")
                .AppendOpeningBracket(Indent1);

            for (var i = 1; i <= options.SourceClassPropertiesCount; i++)
            {
                builder
                    .PadLeft(Indent2)
                    .AppendLine(i % 2 == 0 ? $"public int Prop{i} {{ get; set; }}" : $"public int Prop{i} {{ get; }}");
            }

            options.SourcePropertyBuilder?.Invoke(builder);

            builder
                .AppendClosingBracket(Indent1, false)
                .AppendClosingBracket();

            return builder.ToString();
        }

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
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class IgnorePropertyAttribute : Attribute {{ }}
}}
".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.ShouldContainSource(IgnorePropertyAttributeSource.AttributeName, expectedAttribute);
        }

        [Fact]
        public void VerifyMapToAttribute()
        {
            // Arrange
            const string source = "";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.ShouldContainSource(MapFromAttributeSource.AttributeName, ExpectedAttribute);
        }

        [Fact]
        public void When_FoundMatchingPropertyNameWithDifferentTypes_Should_ReportError()
        {
            // Arrange
            var source = GetSourceText(new SourceGeneratorOptions(
                true,
                PropertyBuilder: builder =>
                {
                    builder
                        .PadLeft(Indent2).AppendLine("public string Prop4 { get; set; }");
                },
                SourcePropertyBuilder: builder => builder.PadLeft(Indent2).AppendLine("public int Prop4 { get; set; }")));

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            var expectedError = DiagnosticProvider.NoMatchingPropertyTypeFoundError(GetSourcePropertySymbol("Prop4", compilation));

            diagnostics.ShouldBeUnsuccessful(expectedError);
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
                        .PadLeft(Indent2).AppendLine("[IgnoreProperty]")
                        .PadLeft(Indent2).AppendLine("public int Prop4 { get; set; }");
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
        }
".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ToString().ShouldContain(expectedResult);
        }

        [Fact]
        public void When_MappingsModifierOptionIsSetToInternal_Should_GenerateThoseMethodsWithInternalAccessModifier()
        {
            // Arrange
            var source = GetSourceText();
            var configOptions = new Dictionary<string, string>
            {
                [GetBuildPropertyName(nameof(SourceGenerationOptions.GeneratedMethodsAccessModifier))] = "Internal",
                [GetBuildPropertyName(nameof(SourceGenerationOptions.GenerateXmlDocument))] = "false"
            };

            var expectedExtension = @"    
    internal static partial class BazToFooExtensions
    {
        internal static Foo ToFoo(this Test.Models.Baz baz)
        {
            return baz == null ? null : new Foo(baz);
        }
    }".Trim();

            var expectedFactory = @"
        internal static Foo From(Test.Models.Baz baz)
        {
            return baz == null ? null : new Foo(baz);
        }".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: configOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();

            var syntaxTree = compilation.SyntaxTrees.Last().ToString();
            syntaxTree.ShouldContain(expectedFactory);
            syntaxTree.ShouldContain(expectedExtension);
        }

        [Fact]
        public void When_MapToAttributeFound_Should_GenerateTheClass()
        {
            // Arrange
            const string source = @"
using MapTo;

namespace Test
{
    [MapFrom(typeof(Baz))]
    public partial class Foo
    {
        public int Prop1 { get; set; }
    }

    public class Baz 
    {
        public int Prop1 { get; set; }
    }
}
";

            const string expectedResult = @"
// <auto-generated />
using System;

namespace Test
{
    partial class Foo
    {
        public Foo(Test.Baz baz)
        {
            if (baz == null) throw new ArgumentNullException(nameof(baz));

            Prop1 = baz.Prop1;
        }
";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ToString().ShouldStartWith(expectedResult.Trim());
        }

        [Fact]
        public void When_MapToAttributeFoundWithoutMatchingProperties_Should_ReportError()
        {
            // Arrange
            const string source = @"
using MapTo;

namespace Test
{
    [MapFrom(typeof(Baz))]
    public partial class Foo { }

    public class Baz { public int Prop1 { get; set; } }
}
";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source);

            // Assert
            var fooType = compilation.GetTypeByMetadataName("Test.Foo");
            fooType.ShouldNotBeNull();

            var bazType = compilation.GetTypeByMetadataName("Test.Baz");
            bazType.ShouldNotBeNull();

            var expectedDiagnostic = DiagnosticProvider.NoMatchingPropertyFoundError(fooType.Locations.Single(), fooType, bazType);
            var error = diagnostics.FirstOrDefault(d => d.Id == expectedDiagnostic.Id);
            error.ShouldNotBeNull();
        }

        [Fact]
        public void When_MapToAttributeWithNamespaceFound_Should_GenerateTheClass()
        {
            // Arrange
            const string source = @"
namespace Test
{
    [MapTo.MapFrom(typeof(Baz))]
    public partial class Foo { public int Prop1 { get; set; } }

    public class Baz { public int Prop1 { get; set; } }
}
";

            const string expectedResult = @"
// <auto-generated />
using System;

namespace Test
{
    partial class Foo
    {
        public Foo(Test.Baz baz)
        {
            if (baz == null) throw new ArgumentNullException(nameof(baz));

            Prop1 = baz.Prop1;
        }
";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ToString().ShouldStartWith(expectedResult.Trim());
        }

        [Fact]
        public void When_NoMapToAttributeFound_Should_GenerateOnlyTheAttribute()
        {
            // Arrange
            const string source = "";
            var expectedTypes = new[] { IgnorePropertyAttributeSource.AttributeName, MapFromAttributeSource.AttributeName, ITypeConverterSource.InterfaceName };

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees
                .Select(s => s.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s.ToString()))
                .All(s => expectedTypes.Any(s.Contains))
                .ShouldBeTrue();
        }

        [Fact]
        public void When_SourceTypeHasDifferentNamespace_Should_NotAddToUsings()
        {
            // Arrange
            var source = GetSourceText(new SourceGeneratorOptions(SourceClassNamespace: "Bazaar"));

            const string expectedResult = @"
// <auto-generated />
using System;

namespace Test
";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ToString().ShouldStartWith(expectedResult.Trim());
        }

        [Fact]
        public void When_SourceTypeHasMatchingProperties_Should_CreateConstructorAndAssignSrcToDest()
        {
            // Arrange
            var source = GetSourceText();

            const string expectedResult = @"
    partial class Foo
    {
        public Foo(Test.Models.Baz baz)
        {
            if (baz == null) throw new ArgumentNullException(nameof(baz));

            Prop1 = baz.Prop1;
            Prop2 = baz.Prop2;
            Prop3 = baz.Prop3;
        }
";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ToString().ShouldContain(expectedResult.Trim());
        }

        [Fact]
        public void When_SourceTypeHasMatchingProperties_Should_CreateFromStaticMethod()
        {
            // Arrange
            var source = GetSourceText();

            const string expectedResult = @"
        public static Foo From(Test.Models.Baz baz)
        {
            return baz == null ? null : new Foo(baz);
        }
";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ToString().ShouldContain(expectedResult.Trim());
        }

        [Fact]
        public void When_SourceTypeHasMatchingProperties_Should_GenerateToExtensionMethodOnSourceType()
        {
            // Arrange
            var source = GetSourceText();

            const string expectedResult = @"
    public static partial class BazToFooExtensions
    {
        public static Foo ToFoo(this Test.Models.Baz baz)
        {
            return baz == null ? null : new Foo(baz);
        }
    }
";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ToString().ShouldContain(expectedResult.Trim());
        }

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

        private static PropertyDeclarationSyntax GetPropertyDeclarationSyntax(SyntaxTree syntaxTree, string targetPropertyName, string targetClass = "Foo")
        {
            return syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Single(c => c.Identifier.ValueText == targetClass)
                .DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Single(p => p.Identifier.ValueText == targetPropertyName);
        }

        private static IPropertySymbol GetSourcePropertySymbol(string propertyName, Compilation compilation, string targetClass = "Foo")
        {
            var syntaxTree = compilation.SyntaxTrees.First();
            var propSyntax = GetPropertyDeclarationSyntax(syntaxTree, propertyName, targetClass);

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            return semanticModel.GetDeclaredSymbol(propSyntax);
        }
    }
}