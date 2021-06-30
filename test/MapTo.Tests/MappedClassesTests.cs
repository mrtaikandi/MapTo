using System.Collections.Generic;
using System.Linq;
using MapTo.Tests.Extensions;
using MapTo.Tests.Infrastructure;
using Microsoft.CodeAnalysis.CSharp;
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

        [Theory]
        [MemberData(nameof(SecondaryConstructorCheckData))]
        public void When_SecondaryConstructorExists_Should_NotGenerateOne(string source, LanguageVersion languageVersion)
        {
            // Arrange
            source = source.Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions, languageVersion: languageVersion);

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

        public static IEnumerable<object[]> SecondaryConstructorCheckData => new List<object[]>
        {
            new object[]
            {
                @"
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
",
                LanguageVersion.CSharp7_3
            },
            new object[]
            {
                @"
using MapTo;
namespace Test.Data.Models
{
    public record SourceClass(string Prop1);

    [MapFrom(typeof(SourceClass))]
    public partial record DestinationClass(string Prop1)
    {
        public DestinationClass(SourceClass source) : this(new MappingContext(), source) { }
    }
}
",
                LanguageVersion.CSharp9
            }
        };

        [Theory]
        [MemberData(nameof(SecondaryCtorWithoutPrivateCtorData))]
        public void When_SecondaryConstructorExistsButDoNotReferencePrivateConstructor_Should_ReportError(string source, LanguageVersion languageVersion)
        {
            // Arrange
            source = source.Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions, languageVersion: languageVersion);

            // Assert
            var constructorSyntax = compilation.SyntaxTrees
                .First()
                .GetRoot()
                .DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .Single();

            diagnostics.ShouldNotBeSuccessful(DiagnosticsFactory.MissingConstructorArgument(constructorSyntax));
        }

        public static IEnumerable<object[]> SecondaryCtorWithoutPrivateCtorData => new List<object[]>
        {
            new object[]
            {
                @"
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
",
                LanguageVersion.CSharp7_3
            },
            new object[]
            {
                @"
using MapTo;
namespace Test.Data.Models
{
    public record SourceClass(string Prop1);

    [MapFrom(typeof(SourceClass))]
    public partial record DestinationClass(string Prop1)
    {
        public DestinationClass(SourceClass source) : this(""invalid"") { }
    }
}
",
                LanguageVersion.CSharp9
            }
        };

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
            var (_, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
        }

        [Theory]
        [MemberData(nameof(VerifyMappedTypesData))]
        public void VerifyMappedTypes(string[] sources, LanguageVersion languageVersion)
        {
            // Arrange
            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(sources, analyzerConfigOptions: DefaultAnalyzerOptions, languageVersion: languageVersion);

            // Assert
            diagnostics.ShouldBeSuccessful();
            _output.WriteLine(compilation.PrintSyntaxTree());
        }

        public static IEnumerable<object[]> VerifyMappedTypesData => new List<object[]>
        {
            new object[] { new[] { MainSourceClass, NestedSourceClass, MainDestinationClass, NestedDestinationClass }, LanguageVersion.CSharp7_3 },
            new object[] { new[] { MainSourceRecord, NestedSourceRecord, MainDestinationRecord, NestedDestinationRecord }, LanguageVersion.CSharp9 }
        };

        [Fact]
        public void VerifySelfReferencingRecords()
        {
            // Arrange
            var source = @"
namespace Tests.Data.Models
{
    using System.Collections.Generic;
    
    public record Employee(int Id, string EmployeeCode, Manager Manager);

    public record Manager(int Id, string EmployeeCode, Manager Manager, int Level, List<Employee> Employees) : Employee(Id, EmployeeCode, Manager);
}

namespace Tests.Data.ViewModels
{
    using System.Collections.Generic;
    using Tests.Data.Models;
    using MapTo;
    
    [MapFrom(typeof(Employee))]
    public partial record EmployeeViewModel(int Id, string EmployeeCode, ManagerViewModel Manager);

    [MapFrom(typeof(Manager))]
    public partial record ManagerViewModel(int Id, string EmployeeCode, ManagerViewModel Manager, int Level, List<EmployeeViewModel> Employees) : EmployeeViewModel(Id, EmployeeCode, Manager);
}
".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions, languageVersion: LanguageVersion.CSharp9);

            // Assert
            diagnostics.ShouldBeSuccessful();
            _output.WriteLine(compilation.PrintSyntaxTree());
        }

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

        private static string MainSourceRecord => BuildSourceRecord("public record User(int Id, DateTimeOffset RegisteredAt, Profile Profile);");
        
        private static string MainDestinationRecord => BuildDestinationRecord(@"
[MapFrom(typeof(User))] 
public partial record UserViewModel(
    [MapProperty(SourcePropertyName = nameof(User.Id))] 
    [MapTypeConverter(typeof(UserViewModel.IdConverter))] 
    string Key, 
    DateTimeOffset RegisteredAt)
{
    private class IdConverter : ITypeConverter<int, string>
    {
        public string Convert(int source, object[] converterParameters) => $""{source:X}"";
    }
}");
        
        private static string NestedSourceRecord => BuildSourceRecord("public record Profile(string FirstName, string LastName) { public string FullName => $\"{FirstName} {LastName}\"; }");
        
        private static string NestedDestinationRecord => BuildDestinationRecord("[MapFrom(typeof(Profile))] public partial record ProfileViewModel(string FirstName, string LastName);");
        
        private static string BuildSourceRecord(string record)
        {
            return $@"
using System;

namespace RecordTest.Data.Models
{{
    {record}        
}}
".Trim();
        }
        
        private static string BuildDestinationRecord(string record)
        {
            return $@"
using System;
using MapTo;
using RecordTest.Data.Models;

namespace RecordTest.ViewModels
{{
    {record}        
}}
".Trim();
        }
    }
}