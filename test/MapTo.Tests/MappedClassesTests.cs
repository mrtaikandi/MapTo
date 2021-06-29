using System.Linq;
using MapTo.Tests.Extensions;
using MapTo.Tests.Infrastructure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using static MapTo.Tests.Common;

namespace MapTo.Tests
{
    public class MappedClassesTests
    {
        private readonly ITestOutputHelper _output;

        public MappedClassesTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void VerifyMappedClassSource()
        {
            // Arrange
            var sources = new[] { MainSourceClass, NestedSourceClass, MainDestinationClass, NestedDestinationClass };

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(sources, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            _output.WriteLine(compilation.PrintSyntaxTree());
        }

        [Fact]
        public void When_SecondaryConstructorExists_Should_NotGenerateOne()
        {
            // Arrange
            var source = @"
using MapTo;
namespace Test.Data.Models
{
    public class SourceClass { public string Prop1 { get; set; } }

    [MapFrom(typeof(SourceClass))]
    public partial class DestinationClass
    {
        public DestinationClass(SourceClass source) : this(new MappingContext(), source) { }
        public string Prop1 { get; set; }
    }
}
".Trim();
            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation
                .GetGeneratedSyntaxTree("DestinationClass")
                .ShouldNotBeNull()
                .GetRoot()
                .DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .Count()
                .ShouldBe(1);
        }

        [Fact]
        public void When_SecondaryConstructorExistsButDoNotReferencePrivateConstructor_Should_ReportError()
        {
            // Arrange
            var source = @"
using MapTo;
namespace Test.Data.Models
{
    public class SourceClass { public string Prop1 { get; set; } }

    [MapFrom(typeof(SourceClass))]
    public partial class DestinationClass
    {
        public DestinationClass(SourceClass source) { }
        public string Prop1 { get; set; }
    }
}
".Trim();
            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            var constructorSyntax = compilation.SyntaxTrees
                .First()
                .GetRoot()
                .DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .Single();

            diagnostics.ShouldNotBeSuccessful(DiagnosticsFactory.MissingConstructorArgument(constructorSyntax));
        }

        [Fact]
        public void When_PropertyNameIsTheSameAsClassName_Should_MapAccordingly()
        {
            // Arrange
            var source = @"
namespace Sale
{
    public class Sale { public Sale Prop1 { get; set; } }
}

namespace SaleModel
{
    using MapTo;
    using Sale;

    [MapFrom(typeof(Sale))]
    public partial class SaleModel
    {
        [MapProperty(SourcePropertyName = nameof(global::Sale.Sale.Prop1))]
        public Sale Sale { get; set; }
    }
}
".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
        }

        private static string NestedSourceClass => @"
namespace Test.Data.Models
{
    public class Profile
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string FullName => $""{FirstName} {LastName}"";
    }
}
".Trim();

        private static string MainSourceClass => @"
using System;

namespace Test.Data.Models
{
    public class User
    {
        public int Id { get; set; }

        public DateTimeOffset RegisteredAt { get; set; }

        public Profile Profile { get; set; }
    }
}
".Trim();

        private static string NestedDestinationClass => @"
using MapTo;
using Test.Data.Models;

namespace Test.ViewModels
{
    [MapFrom(typeof(Profile))]
    public partial class ProfileViewModel
    {
        public string FirstName { get; }

        public string LastName { get; }
    }
}
".Trim();


        private static string MainDestinationClass => @"
using System;
using MapTo;
using Test.Data.Models;

namespace Test.ViewModels
{
    [MapFrom(typeof(User))]
    public partial class UserViewModel
    {
        [MapProperty(SourcePropertyName = nameof(User.Id))]
        [MapTypeConverter(typeof(IdConverter))]
        public string Key { get; }

        public DateTimeOffset RegisteredAt { get; set; }
        
        // [IgnoreProperty]
        public ProfileViewModel Profile { get; set; }

        private class IdConverter : ITypeConverter<int, string>
        {
            public string Convert(int source, object[] converterParameters) => $""{source:X}"";
        }
    }
}
".Trim();
    }
}