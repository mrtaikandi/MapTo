using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapTo.Extensions;
using MapTo.Models;
using MapTo.Tests.Infrastructure;
using Microsoft.CodeAnalysis;
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

        private static readonly Dictionary<string, string> DefaultAnalyzerOptions = new()
        {
            [GetBuildPropertyName(nameof(SourceGenerationOptions.GenerateXmlDocument))] = "false"
        };

        private static readonly string ExpectedAttribute = $@"{SourceBuilder.GeneratedFilesHeader}
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

            if (options.UseMapToNamespace)
            {
                builder.AppendFormat("using {0};", SourceBuilder.NamespaceName).AppendLine();
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
                .AppendLine(options.UseMapToNamespace ? "[MapTo.MapFrom(typeof(Baz))]" : "[MapFrom(typeof(Baz))]")
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
{SourceBuilder.GeneratedFilesHeader}
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
            compilation.SyntaxTrees.ShouldContain(c => c.ToString() == expectedAttribute);
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
            compilation.SyntaxTrees.ShouldContain(c => c.ToString() == ExpectedAttribute);
        }

        [Fact]
        public void When_FoundMatchingPropertyNameWithDifferentType_Should_Ignore()
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
            var expectedDiagnostic = Diagnostics.NoMatchingPropertyFoundError(Location.None, "Foo", "Baz");
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
            var (_, diagnostics) = CSharpGenerator.GetOutputCompilation(source);

            // Assert
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

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees
                .Select(s => s.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s.ToString()))
                .All(s => s.Contains(": Attribute"))
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
    }
}