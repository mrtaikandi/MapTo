using System;
using System.Collections.Generic;
using System.Linq;
using MapTo.Sources;
using MapTo.Tests.Extensions;
using MapTo.Tests.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
            var languageVersion = nullableContextOptions == NullableContextOptions.Enable ? LanguageVersion.CSharp8 : LanguageVersion.CSharp7_3;
            var expectedInterface = $@"
{Constants.GeneratedFilesHeader}
{(nullableContextOptions == NullableContextOptions.Enable ? $"#nullable enable{Environment.NewLine}" : string.Empty)}
using System;

namespace MapTo
{{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
    public sealed class MapPropertyAttribute : Attribute
    {{
        public string{nullableSyntax} SourcePropertyName {{ get; set; }}
        public Type{nullableSyntax} SourceTypeName {{ get; set; }}
    }}
}}
".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions, nullableContextOptions: nullableContextOptions, languageVersion: languageVersion);

            
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
            Prop4 = baz.Prop3;
        }
".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ShouldContainPartialSource(expectedResult);
        }

        [Fact]
        public void When_MapPropertyFound_Should_UseItToTypedMapToSourceProperty()
        {
            // Arrange
            var source = GetSourceText(new SourceGeneratorOptions(
                true,
                PropertyBuilder: builder =>
                {
                    builder
                        .WriteLine("[MapProperty(SourcePropertyName = nameof(Baz.Prop3))]")
                        .WriteLine("public int Prop4 { get; set; }")
                        .WriteLine("[MapProperty(SourcePropertyName = nameof(Baz.Prop4), SourceTypeName = typeof(Test.Models.Baz))]")
                        .WriteLine("public int Prop5 { get; set; }")
                        .WriteLine("[MapProperty(SourcePropertyName = nameof(Baz.Prop4), SourceTypeName = typeof(Foo))]")
                        .WriteLine("public int Prop6 { get; set; }");
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
            Prop4 = baz.Prop3;
            Prop5 = baz.Prop4;
        }
".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ShouldContainPartialSource(expectedResult);
        }

        [Theory]
        [MemberData(nameof(MapPropertyWithImplicitConversionFoundData))]
        public void When_MapPropertyWithImplicitConversionFound_Should_UseItToMapToSourceProperty(string source, string expectedResult, LanguageVersion languageVersion)
        {
            // Arrange
            source = source.Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions, languageVersion: languageVersion);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ShouldContainPartialSource(expectedResult);
        }

        public static IEnumerable<object[]> MapPropertyWithImplicitConversionFoundData => new List<object[]>
        {
            new object[]
            {
                @"
namespace Test
{
    using System.Collections.Generic;

    public class InnerClass { public int Prop1 { get; set; } }
    public class OuterClass
    {
        public int Id { get; set; }
        public List<InnerClass> InnerProp { get; set; }
    }
}

namespace Test.Models
{
    using MapTo;
    using System.Collections.Generic;

    [MapFrom(typeof(Test.InnerClass))]
    public partial class InnerClass { public int Prop1 { get; set; } }

    [MapFrom(typeof(Test.OuterClass))]
    public partial class OuterClass
    {
        public int Id { get; set; }
        public IReadOnlyList<InnerClass> InnerProp { get; set; }
    }
}
",
                @"
        private protected OuterClass(MappingContext context, Test.OuterClass outerClass)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (outerClass == null) throw new ArgumentNullException(nameof(outerClass));

            context.Register(outerClass, this);

            Id = outerClass.Id;
            InnerProp = outerClass.InnerProp.Select(context.MapFromWithContext<Test.InnerClass, InnerClass>).ToList();
        }
",
                LanguageVersion.CSharp7_3
            },
            new object[]
            {
                @"
namespace Test 
{
    using System;
    using System.Collections.Generic;

    public class InnerClass
    {      
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class OuterClass
    {
        public int Id { get; set; }
        public List<InnerClass> InnerClasses { get; set; }
        public DateTime? SomeDate { get; set; }
    }  
}

namespace Test.Models
{
    using MapTo;
    using System;
    using System.Collections.Generic;

    [MapFrom(typeof(Test.InnerClass))]
    public partial record InnerClass(int Id, string Name);

    [MapFrom(typeof(Test.OuterClass))]
    public partial record OuterClass(int Id, DateTime? SomeDate, IReadOnlyList<InnerClass> InnerClasses);
}
",
                @"
        private protected OuterClass(MappingContext context, Test.OuterClass outerClass)
            : this(Id: outerClass.Id, SomeDate: outerClass.SomeDate, InnerClasses: outerClass.InnerClasses.Select(context.MapFromWithContext<Test.InnerClass, InnerClass>).ToList())
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (outerClass == null) throw new ArgumentNullException(nameof(outerClass));

            context.Register(outerClass, this);
        }
",
                LanguageVersion.CSharp9
            }
        };
    }
}