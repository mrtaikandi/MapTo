using System;
using System.Collections.Generic;
using System.Linq;
using MapTo.Extensions;
using MapTo.Sources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;

namespace MapTo.Tests
{
    internal static class Common
    {
        internal const int Indent1 = 4;
        internal const int Indent2 = Indent1 * 2;
        internal const int Indent3 = Indent1 * 3;
        internal static readonly Location IgnoreLocation = Location.None;

        internal static readonly Dictionary<string, string> DefaultAnalyzerOptions = new()
        {
            [GeneratorExecutionContextExtensions.GetBuildPropertyName(nameof(SourceGenerationOptions.GenerateXmlDocument))] = "false"
        };

        internal static string GetSourceText(SourceGeneratorOptions? options = null)
        {
            const string ns = "Test";
            options ??= new SourceGeneratorOptions();
            var hasDifferentSourceNamespace = options.SourceClassNamespace != ns;
            var builder = new SourceBuilder();

            builder.WriteLine("//");
            builder.WriteLine("// Test source code.");
            builder.WriteLine("//");
            builder.WriteLine();

            options.Usings?.ForEach(s => builder.WriteLine($"using {s};"));

            if (options.UseMapToNamespace)
            {
                builder.WriteLine($"using {Constants.RootNamespace};");
            }

            builder
                .WriteLine($"using {options.SourceClassNamespace};")
                .WriteLine()
                .WriteLine();

            builder
                .WriteLine($"namespace {ns}")
                .WriteOpeningBracket();

            if (hasDifferentSourceNamespace && options.UseMapToNamespace)
            {
                builder
                    .WriteLine($"using {options.SourceClassNamespace};")
                    .WriteLine()
                    .WriteLine();
            }

            builder
                .WriteLine(options.UseMapToNamespace ? "[MapFrom(typeof(Baz))]" : "[MapTo.MapFrom(typeof(Baz))]")
                .WriteLine("public partial class Foo")
                .WriteOpeningBracket();

            for (var i = 1; i <= options.ClassPropertiesCount; i++)
            {
                builder.WriteLine(i % 2 == 0 ? $"public int Prop{i} {{ get; set; }}" : $"public int Prop{i} {{ get; }}");
            }

            options.PropertyBuilder?.Invoke(builder);

            builder
                .WriteClosingBracket()
                .WriteClosingBracket()
                .WriteLine()
                .WriteLine();

            builder
                .WriteLine($"namespace {options.SourceClassNamespace}")
                .WriteOpeningBracket()
                .WriteLine("public class Baz")
                .WriteOpeningBracket();

            for (var i = 1; i <= options.SourceClassPropertiesCount; i++)
            {
                builder.WriteLine(i % 2 == 0 ? $"public int Prop{i} {{ get; set; }}" : $"public int Prop{i} {{ get; }}");
            }

            options.SourcePropertyBuilder?.Invoke(builder);

            builder
                .WriteClosingBracket()
                .WriteClosingBracket();

            return builder.ToString();
        }

        internal static string[] GetEmployeeManagerSourceText(
            Func<string>? employeeClassSource = null,
            Func<string>? managerClassSource = null,
            Func<string>? employeeViewModelSource = null,
            Func<string>? managerViewModelSource = null,
            bool useDifferentViewModelNamespace = false)
        {
            return new[]
            {
                employeeClassSource?.Invoke() ?? DefaultEmployeeClassSource(),
                managerClassSource?.Invoke() ?? DefaultManagerClassSource(),
                employeeViewModelSource?.Invoke() ??
                DefaultEmployeeViewModelSource(useDifferentViewModelNamespace),
                managerViewModelSource?.Invoke() ?? DefaultManagerViewModelSource(useDifferentViewModelNamespace)
            };

            static string DefaultEmployeeClassSource() =>
                @"
using System;
using System.Collections.Generic;
using System.Text;

namespace Test.Data.Models
{
    public class Employee
    {
        public int Id { get; set; }

        public string EmployeeCode { get; set; }

        public Manager Manager { get; set; }
    }
}".Trim();

            static string DefaultManagerClassSource() =>
                @"using System;
using System.Collections.Generic;
using System.Text;

namespace Test.Data.Models
{
    public class Manager: Employee
    {
        public int Level { get; set; }

        public IEnumerable<Employee> Employees { get; set; } = Array.Empty<Employee>();
    }
}
".Trim();

            static string DefaultEmployeeViewModelSource(bool useDifferentNamespace) => useDifferentNamespace
                ? @"
using MapTo;
using Test.Data.Models;
using Test.ViewModels2;

namespace Test.ViewModels
{
    [MapFrom(typeof(Employee))]
    public partial class EmployeeViewModel
    {
        public int Id { get; set; }

        public string EmployeeCode { get; set; }

        public ManagerViewModel Manager { get; set; }
    }
}
".Trim()
                : @"
using MapTo;
using Test.Data.Models;

namespace Test.ViewModels
{
    [MapFrom(typeof(Employee))]
    public partial class EmployeeViewModel
    {
        public int Id { get; set; }

        public string EmployeeCode { get; set; }

        public ManagerViewModel Manager { get; set; }
    }
}
".Trim();

            static string DefaultManagerViewModelSource(bool useDifferentNamespace) => useDifferentNamespace
                ? @"
using System;
using System.Collections.Generic;
using MapTo;
using Test.Data.Models;
using Test.ViewModels;

namespace Test.ViewModels2
{
    [MapFrom(typeof(Manager))]
    public partial class ManagerViewModel : EmployeeViewModel
    {
        public int Level { get; set; }

        public IEnumerable<EmployeeViewModel> Employees { get; set; } = Array.Empty<EmployeeViewModel>();
    }
}
".Trim()
                : @"
using System;
using System.Collections.Generic;
using MapTo;
using Test.Data.Models;

namespace Test.ViewModels
{
    [MapFrom(typeof(Manager))]
    public partial class ManagerViewModel : EmployeeViewModel
    {
        public int Level { get; set; }

        public IEnumerable<EmployeeViewModel> Employees { get; set; } = Array.Empty<EmployeeViewModel>();
    }
}".Trim();
        }

        internal static PropertyDeclarationSyntax GetPropertyDeclarationSyntax(SyntaxTree syntaxTree, string targetPropertyName, string targetClass = "Foo")
        {
            return syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Single(c => c.Identifier.ValueText == targetClass)
                .DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Single(p => p.Identifier.ValueText == targetPropertyName);
        }

        internal static IPropertySymbol GetSourcePropertySymbol(string propertyName, Compilation compilation, string targetClass = "Foo")
        {
            var syntaxTree = compilation.SyntaxTrees.First();
            var propSyntax = GetPropertyDeclarationSyntax(syntaxTree, propertyName, targetClass);

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            return semanticModel.GetDeclaredSymbol(propSyntax).ShouldNotBeNull();
        }

        internal record SourceGeneratorOptions(
            bool UseMapToNamespace = false,
            string SourceClassNamespace = "Test.Models",
            int ClassPropertiesCount = 3,
            int SourceClassPropertiesCount = 3,
            Action<SourceBuilder>? PropertyBuilder = null,
            Action<SourceBuilder>? SourcePropertyBuilder = null,
            IEnumerable<string>? Usings = null);
    }
}