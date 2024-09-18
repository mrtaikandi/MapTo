using System.Diagnostics.CodeAnalysis;

namespace MapTo.Tests;

public class NullHandlingTests
{
    private readonly ITestOutputHelper _output;

    public NullHandlingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void When_NullHandlingIsSetNull_Should_CheckIfSourcePropertyIsNull()
    {
        // Arrange
        var builder = ScenarioBuilder.BuildEmployeeManagerModels(TestSourceBuilderOptions.Create(analyzerConfigOptions: new Dictionary<string, string>
        {
            [nameof(CodeGeneratorOptions.NullHandling)] = NullHandling.SetNull.ToString()
        }));

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var employeeExtensionClass = compilation.GetClassDeclaration("EmployeeToEmployeeViewModelMapToExtensions", "MapTo.Tests.EmployeeViewModel.g.cs");
        employeeExtensionClass.ShouldContain(
            """
            if (employee.Manager is not null)
            {
                target.Manager = global::MapTo.Tests.ManagerToManagerViewModelMapToExtensions.MapToManagerViewModel(employee.Manager);
            }
            """);
    }

    [Fact]
    public void When_NullHandlingIsThrowException_Should_ThrowExceptionIfSourceIsNull()
    {
        // Arrange
        var builder = ScenarioBuilder.BuildEmployeeManagerModels(TestSourceBuilderOptions.Create(analyzerConfigOptions: new Dictionary<string, string>
        {
            [nameof(CodeGeneratorOptions.NullHandling)] = NullHandling.ThrowException.ToString()
        }));

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var employeeExtensionClass = compilation.GetClassDeclaration("EmployeeToEmployeeViewModelMapToExtensions", "MapTo.Tests.EmployeeViewModel.g.cs");
        employeeExtensionClass.ShouldContain(
            "target.Manager = employee.Manager is null ? throw new global::System.ArgumentNullException(nameof(employee.Manager)) : global::MapTo.Tests.ManagerToManagerViewModelMapToExtensions.MapToManagerViewModel(employee.Manager);");
    }

    [Fact]
    public void When_NullHandlingIsAutoAndNullabilityContextIsDisabled_Should_CheckIfSourcePropertyIsNull()
    {
        // Arrange
        var builder = ScenarioBuilder.BuildEmployeeManagerModels(TestSourceBuilderOptions.Create(
            supportNullReferenceTypes: false,
            analyzerConfigOptions: new Dictionary<string, string>
            {
                [nameof(CodeGeneratorOptions.NullHandling)] = NullHandling.Auto.ToString()
            }));

        // Act
        var (compilation, diagnostics) = builder.Compile();
        compilation.Dump(_output);

        // Assert
        diagnostics.ShouldBeSuccessful();

        var employeeExtensionClass = compilation.GetClassDeclaration("EmployeeToEmployeeViewModelMapToExtensions", "MapTo.Tests.EmployeeViewModel.g.cs");
        employeeExtensionClass.ShouldContain(
            """
            if (employee.Manager is not null)
            {
                target.Manager = global::MapTo.Tests.ManagerToManagerViewModelMapToExtensions.MapToManagerViewModel(employee.Manager);
            }
            """);
    }

    [Fact]
    public void When_NullHandlingIsAutoAndSourceIsNotAnnotated_Should_CheckIfSourcePropertyIsNull()
    {
        // Arrange
        var builder = ScenarioBuilder.BuildEmployeeManagerModels(TestSourceBuilderOptions.Create(
            supportNullReferenceTypes: true,
            analyzerConfigOptions: new Dictionary<string, string>
            {
                [nameof(CodeGeneratorOptions.NullHandling)] = NullHandling.Auto.ToString()
            }));

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var employeeExtensionClass = compilation.GetClassDeclaration("EmployeeToEmployeeViewModelMapToExtensions", "MapTo.Tests.EmployeeViewModel.g.cs");
        employeeExtensionClass.ShouldContain(
            """
            if (employee.Manager is not null)
            {
                target.Manager = global::MapTo.Tests.ManagerToManagerViewModelMapToExtensions.MapToManagerViewModel(employee.Manager);
            }
            """);
    }

    [Fact]
    public void When_NullHandlingIsAutoAndSourceIsAnnotated_Should_CheckIfSourcePropertyIsNull()
    {
        // Arrange
        var builder = new TestSourceBuilder(
            supportNullReferenceTypes: true,
            analyzerConfigOptions: new Dictionary<string, string>
            {
                [nameof(CodeGeneratorOptions.NullHandling)] = NullHandling.Auto.ToString()
            });

        var sourceFile = builder.AddFile(supportNullableReferenceTypes: true);
        sourceFile.AddClass(Accessibility.Public, "MiddleClass").WithProperty<int>("Key");
        sourceFile.AddClass(Accessibility.Public, "MappedMiddleClass", attributes: "[MapFrom(typeof(MiddleClass))]")
            .WithProperty<int>("Key");

        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty("MiddleClass?", "Prop1", defaultValue: "null!");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty("MappedMiddleClass", "Prop1", defaultValue: "null!");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var extensionClass = compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain(
            """
            if (sourceClass.Prop1 is not null)
            {
                target.Prop1 = global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass(sourceClass.Prop1);
            }
            """);
    }

    [Fact]
    public void When_NullHandlingIsAutoAndSourceIsNotNullable_Should_AssignDirectly()
    {
        // Arrange
        var builder = new TestSourceBuilder(
            supportNullReferenceTypes: true,
            analyzerConfigOptions: new Dictionary<string, string>
            {
                [nameof(CodeGeneratorOptions.NullHandling)] = NullHandling.Auto.ToString()
            });

        var sourceFile = builder.AddFile(supportNullableReferenceTypes: true);
        sourceFile.AddClass(Accessibility.Public, "MiddleClass").WithProperty<int>("Key");
        sourceFile.AddClass(Accessibility.Public, "MappedMiddleClass", attributes: "[MapFrom(typeof(MiddleClass))]")
            .WithProperty<int>("Key");

        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty("MiddleClass", "Prop1", defaultValue: "null!");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty("MappedMiddleClass", "Prop1", defaultValue: "null!");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var extensionClass = compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain("target.Prop1 = global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass(sourceClass.Prop1);");
    }

    [Theory]
    [MemberData(nameof(CollectionNullHandlingAutoTestData))]
    [MemberData(nameof(CollectionNullHandlingSetNullTestData))]
    [MemberData(nameof(CollectionNullHandlingThrowExceptionTestData))]
    [MemberData(nameof(CollectionNullHandlingSetEmptyCollectionTestData))]
    public void VerifyCollectionNullHandling(
        int key,
        bool supportNullableTypes,
        bool copyPrimitiveArrays,
        NullHandling nullHandling,
        string sourceProperty,
        string targetProperty,
        string expected)
    {
        // Arrange
        key.ShouldBeGreaterThan(0);
        var builder = new TestSourceBuilder(
            supportNullReferenceTypes: supportNullableTypes,
            analyzerConfigOptions: new Dictionary<string, string>
            {
                [nameof(CodeGeneratorOptions.NullHandling)] = nullHandling.ToString(),
                [nameof(CodeGeneratorOptions.CopyPrimitiveArrays)] = copyPrimitiveArrays.ToString()
            });

        var sourceFile = builder.AddFile(supportNullableReferenceTypes: supportNullableTypes, usings: new[] { "System.Collections", "System.Collections.Generic" });
        sourceFile.AddClass(Accessibility.Public, "MiddleClass").WithProperty<int>("Key");
        sourceFile.AddClass(Accessibility.Public, "MappedMiddleClass", attributes: "[MapFrom(typeof(MiddleClass))]")
            .WithProperty<int>("Key");

        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty(sourceProperty, "Prop1", defaultValue: "null!");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty(targetProperty, "Prop1", defaultValue: "null!");

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        compilation.Dump(_output);
        compilation.ShouldBeSuccessful();
        diagnostics.ShouldBeSuccessful();

        var extensionClass = compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain(expected);
    }

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Test data")]
    public static IEnumerable<object[]> CollectionNullHandlingThrowExceptionTestData()
    {
        yield return new object[]
        {
            1, false, false, NullHandling.ThrowException, "List<int>", "List<int>",
            "target.Prop1 = sourceClass.Prop1 ?? throw new global::System.ArgumentNullException(nameof(sourceClass.Prop1));"
        };

        yield return new object[]
        {
            2, false, false, NullHandling.ThrowException, "string[]", "string[]",
            "target.Prop1 = sourceClass.Prop1 ?? throw new global::System.ArgumentNullException(nameof(sourceClass.Prop1));"
        };

        yield return new object[]
        {
            3, false, false, NullHandling.ThrowException, "MiddleClass[]", "MiddleClass[]",
            "target.Prop1 = sourceClass.Prop1 ?? throw new global::System.ArgumentNullException(nameof(sourceClass.Prop1));"
        };

        yield return new object[]
        {
            4, false, false, NullHandling.ThrowException, "List<MiddleClass>", "List<MappedMiddleClass>",
            "target.Prop1 = sourceClass.Prop1?.Select<global::MapTo.Tests.MiddleClass, global::MapTo.Tests.MappedMiddleClass>(global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass).ToList() ?? throw new global::System.ArgumentNullException(nameof(sourceClass.Prop1));"
        };

        yield return new object[]
        {
            5, false, true, NullHandling.ThrowException, "List<int>", "List<int>",
            "target.Prop1 = sourceClass.Prop1 ?? throw new global::System.ArgumentNullException(nameof(sourceClass.Prop1));"
        };

        yield return new object[]
        {
            6, false, true, NullHandling.ThrowException, "string[]", "string[]",
            "target.Prop1 = sourceClass.Prop1 is null ? throw new global::System.ArgumentNullException(nameof(sourceClass.Prop1)) : MapToStringArray(sourceClass.Prop1);"
        };

        yield return new object[]
        {
            7, false, true, NullHandling.ThrowException, "MiddleClass[]", "MiddleClass[]",
            "target.Prop1 = sourceClass.Prop1 is null ? throw new global::System.ArgumentNullException(nameof(sourceClass.Prop1)) : MapToMiddleClassArray(sourceClass.Prop1);"
        };

        yield return new object[]
        {
            8, true, false, NullHandling.ThrowException, "int[]", "int[]",
            "target.Prop1 = sourceClass.Prop1 ?? throw new global::System.ArgumentNullException(nameof(sourceClass.Prop1));"
        };

        yield return new object[]
        {
            9, true, false, NullHandling.ThrowException, "int[]", "int[]?",
            "target.Prop1 = sourceClass.Prop1 ?? throw new global::System.ArgumentNullException(nameof(sourceClass.Prop1));"
        };

        yield return new object[]
        {
            10, true, false, NullHandling.ThrowException, "int[]?", "int[]",
            "target.Prop1 = sourceClass.Prop1 ?? throw new global::System.ArgumentNullException(nameof(sourceClass.Prop1));"
        };

        yield return new object[]
        {
            11, true, false, NullHandling.ThrowException, "string[]", "string?[]",
            "target.Prop1 = sourceClass.Prop1 ?? throw new global::System.ArgumentNullException(nameof(sourceClass.Prop1));"
        };

        yield return new object[]
        {
            12, true, true, NullHandling.ThrowException, "int[]", "int[]",
            "target.Prop1 = sourceClass.Prop1 is null ? throw new global::System.ArgumentNullException(nameof(sourceClass.Prop1)) : MapToInt32Array(sourceClass.Prop1);"
        };

        yield return new object[]
        {
            13, true, true, NullHandling.ThrowException, "int[]?", "int[]",
            "target.Prop1 = sourceClass.Prop1 is null ? throw new global::System.ArgumentNullException(nameof(sourceClass.Prop1)) : MapToInt32Array(sourceClass.Prop1);"
        };

        yield return new object[]
        {
            14, true, true, NullHandling.ThrowException, "int[]", "int[]?",
            "target.Prop1 = sourceClass.Prop1 is null ? throw new global::System.ArgumentNullException(nameof(sourceClass.Prop1)) : MapToInt32Array(sourceClass.Prop1);"
        };

        yield return new object[]
        {
            15, true, true, NullHandling.ThrowException, "string[]", "string?[]",
            "target.Prop1 = sourceClass.Prop1 is null ? throw new global::System.ArgumentNullException(nameof(sourceClass.Prop1)) : MapToStringArray(sourceClass.Prop1);"
        };

        yield return new object[]
        {
            16, true, true, NullHandling.ThrowException, "List<MiddleClass>", "List<MappedMiddleClass>?",
            "target.Prop1 = sourceClass.Prop1?.Select<global::MapTo.Tests.MiddleClass, global::MapTo.Tests.MappedMiddleClass>(global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass).ToList() ?? throw new global::System.ArgumentNullException(nameof(sourceClass.Prop1));"
        };

        yield return new object[]
        {
            17, true, true, NullHandling.ThrowException, "List<MiddleClass>?", "List<MappedMiddleClass>",
            "target.Prop1 = sourceClass.Prop1?.Select<global::MapTo.Tests.MiddleClass, global::MapTo.Tests.MappedMiddleClass>(global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass).ToList() ?? throw new global::System.ArgumentNullException(nameof(sourceClass.Prop1));"
        };

        yield return new object[]
        {
            18, true, true, NullHandling.ThrowException, "List<MiddleClass>?", "List<MappedMiddleClass>?",
            "target.Prop1 = sourceClass.Prop1?.Select<global::MapTo.Tests.MiddleClass, global::MapTo.Tests.MappedMiddleClass>(global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass).ToList() ?? throw new global::System.ArgumentNullException(nameof(sourceClass.Prop1));"
        };

        yield return new object[]
        {
            19, true, true, NullHandling.ThrowException, "List<MiddleClass?>", "List<MappedMiddleClass?>",
            "target.Prop1 = sourceClass.Prop1?.Select<global::MapTo.Tests.MiddleClass?, global::MapTo.Tests.MappedMiddleClass?>(global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass).ToList() ?? throw new global::System.ArgumentNullException(nameof(sourceClass.Prop1));"
        };
    }

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Test data")]
    public static IEnumerable<object[]> CollectionNullHandlingSetEmptyCollectionTestData()
    {
        yield return new object[]
        {
            20, false, false, NullHandling.SetEmptyCollection, "List<int>", "List<int>",
            "target.Prop1 = sourceClass.Prop1 ?? new global::System.Collections.Generic.List<int>();"
        };

        yield return new object[]
        {
            21, false, false, NullHandling.SetEmptyCollection, "string[]", "string[]",
            "target.Prop1 = sourceClass.Prop1 ?? global::System.Array.Empty<string>();"
        };

        yield return new object[]
        {
            22, false, false, NullHandling.SetEmptyCollection, "MiddleClass[]", "MiddleClass[]",
            "target.Prop1 = sourceClass.Prop1 ?? global::System.Array.Empty<global::MapTo.Tests.MiddleClass>();"
        };

        yield return new object[]
        {
            23, false, false, NullHandling.SetEmptyCollection, "List<MiddleClass>", "IEnumerable<MappedMiddleClass>",
            "target.Prop1 = sourceClass.Prop1?.Select<global::MapTo.Tests.MiddleClass, global::MapTo.Tests.MappedMiddleClass>(global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass).ToArray() ?? global::System.Linq.Enumerable.Empty<global::MapTo.Tests.MappedMiddleClass>();"
        };

        yield return new object[]
        {
            24, false, false, NullHandling.SetEmptyCollection, "IList<string>", "IList<string>",
            "target.Prop1 = sourceClass.Prop1 ?? new global::System.Collections.Generic.List<string>();"
        };

        yield return new object[]
        {
            25, false, true, NullHandling.SetEmptyCollection, "List<int>", "List<int>",
            "target.Prop1 = sourceClass.Prop1 ?? new global::System.Collections.Generic.List<int>();"
        };

        yield return new object[]
        {
            26, false, true, NullHandling.SetEmptyCollection, "string[]", "string[]",
            "target.Prop1 = sourceClass.Prop1 is null ? global::System.Array.Empty<string>() : MapToStringArray(sourceClass.Prop1);"
        };

        yield return new object[]
        {
            27, false, true, NullHandling.SetEmptyCollection, "MiddleClass[]", "MiddleClass[]",
            "target.Prop1 = sourceClass.Prop1 is null ? global::System.Array.Empty<global::MapTo.Tests.MiddleClass>() : MapToMiddleClassArray(sourceClass.Prop1);"
        };

        yield return new object[]
        {
            28, false, true, NullHandling.SetEmptyCollection, "List<MiddleClass>", "IEnumerable<MappedMiddleClass>",
            "target.Prop1 = sourceClass.Prop1?.Select<global::MapTo.Tests.MiddleClass, global::MapTo.Tests.MappedMiddleClass>(global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass).ToArray() ?? global::System.Linq.Enumerable.Empty<global::MapTo.Tests.MappedMiddleClass>();"
        };

        yield return new object[]
        {
            29, false, true, NullHandling.SetEmptyCollection, "IList<string>", "IList<string>",
            "target.Prop1 = sourceClass.Prop1 ?? new global::System.Collections.Generic.List<string>();"
        };

        yield return new object[]
        {
            30, true, false, NullHandling.SetEmptyCollection, "int[]", "int[]",
            "target.Prop1 = sourceClass.Prop1 ?? global::System.Array.Empty<int>();"
        };

        yield return new object[]
        {
            31, true, false, NullHandling.SetEmptyCollection, "int[]", "int[]?",
            "target.Prop1 = sourceClass.Prop1 ?? global::System.Array.Empty<int>();"
        };

        yield return new object[]
        {
            32, true, false, NullHandling.SetEmptyCollection, "string[]", "string?[]",
            "target.Prop1 = sourceClass.Prop1 ?? global::System.Array.Empty<string?>();"
        };

        yield return new object[]
        {
            33, true, true, NullHandling.SetEmptyCollection, "int[]", "int[]",
            "target.Prop1 = sourceClass.Prop1 is null ? global::System.Array.Empty<int>() : MapToInt32Array(sourceClass.Prop1);"
        };

        yield return new object[]
        {
            34, true, true, NullHandling.SetEmptyCollection, "string[]", "string?[]",
            "target.Prop1 = sourceClass.Prop1 is null ? global::System.Array.Empty<string?>() : MapToStringArray(sourceClass.Prop1);"
        };
    }

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Test data")]
    public static IEnumerable<object[]> CollectionNullHandlingSetNullTestData()
    {
        yield return new object[]
        {
            35, false, false, NullHandling.SetNull, "List<int>", "List<int>",
            "target.Prop1 = sourceClass.Prop1;"
        };

        yield return new object[]
        {
            36, false, false, NullHandling.SetNull, "string[]", "string[]",
            "target.Prop1 = sourceClass.Prop1;"
        };

        yield return new object[]
        {
            37, false, false, NullHandling.SetNull, "MiddleClass[]", "MiddleClass[]",
            "target.Prop1 = sourceClass.Prop1;"
        };

        yield return new object[]
        {
            38, false, false, NullHandling.SetNull, "List<MiddleClass>", "IEnumerable<MappedMiddleClass>",
            """
            if (sourceClass.Prop1 is not null)
            {
                target.Prop1 = sourceClass.Prop1.Select<global::MapTo.Tests.MiddleClass, global::MapTo.Tests.MappedMiddleClass>(global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass).ToArray();
            }
            """
        };

        yield return new object[]
        {
            39, false, false, NullHandling.SetNull, "IList<string>", "IList<string>",
            "target.Prop1 = sourceClass.Prop1;"
        };

        yield return new object[]
        {
            40, false, true, NullHandling.SetNull, "List<int>", "List<int>",
            "target.Prop1 = sourceClass.Prop1;"
        };

        yield return new object[]
        {
            41, false, true, NullHandling.SetNull, "string[]", "string[]",
            """
            if (sourceClass.Prop1 is not null)
            {
                target.Prop1 = MapToStringArray(sourceClass.Prop1);
            }
            """
        };

        yield return new object[]
        {
            42, false, true, NullHandling.SetNull, "MiddleClass[]", "MiddleClass[]",
            """
            if (sourceClass.Prop1 is not null)
            {
                target.Prop1 = MapToMiddleClassArray(sourceClass.Prop1);
            }
            """
        };

        yield return new object[]
        {
            43, false, true, NullHandling.SetNull, "List<MiddleClass>", "IEnumerable<MappedMiddleClass>",
            """
            if (sourceClass.Prop1 is not null)
            {
                target.Prop1 = sourceClass.Prop1.Select<global::MapTo.Tests.MiddleClass, global::MapTo.Tests.MappedMiddleClass>(global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass).ToArray();
            }
            """
        };

        yield return new object[]
        {
            44, false, true, NullHandling.SetNull, "IList<string>", "IList<string>",
            "target.Prop1 = sourceClass.Prop1;"
        };

        yield return new object[]
        {
            45, true, false, NullHandling.SetNull, "int[]", "int[]?",
            "target.Prop1 = sourceClass.Prop1;"
        };

        yield return new object[]
        {
            46, true, false, NullHandling.SetNull, "MiddleClass[]?", "MiddleClass[]",
            "target.Prop1 = sourceClass.Prop1;"
        };

        yield return new object[]
        {
            47, true, false, NullHandling.SetNull, "List<MiddleClass>", "IEnumerable<MappedMiddleClass?>",
            """
            if (sourceClass.Prop1 is not null)
            {
                target.Prop1 = sourceClass.Prop1.Select<global::MapTo.Tests.MiddleClass, global::MapTo.Tests.MappedMiddleClass?>(global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass).ToArray();
            }
            """
        };

        yield return new object[]
        {
            48, true, false, NullHandling.SetNull, "IList<string>", "IList<string>",
            "target.Prop1 = sourceClass.Prop1;"
        };

        yield return new object[]
        {
            49, true, true, NullHandling.SetNull, "List<int>", "List<int>",
            "target.Prop1 = sourceClass.Prop1;"
        };

        yield return new object[]
        {
            50, true, true, NullHandling.SetNull, "string[]", "string[]",
            """
            if (sourceClass.Prop1 is not null)
            {
                target.Prop1 = MapToStringArray(sourceClass.Prop1);
            }
            """
        };

        yield return new object[]
        {
            51, true, true, NullHandling.SetNull, "MiddleClass[]", "MiddleClass[]",
            """
            if (sourceClass.Prop1 is not null)
            {
                target.Prop1 = MapToMiddleClassArray(sourceClass.Prop1);
            }
            """
        };

        yield return new object[]
        {
            52, true, true, NullHandling.SetNull, "List<MiddleClass?>", "IEnumerable<MappedMiddleClass?>",
            """
            if (sourceClass.Prop1 is not null)
            {
                target.Prop1 = sourceClass.Prop1.Select<global::MapTo.Tests.MiddleClass?, global::MapTo.Tests.MappedMiddleClass?>(global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass).ToArray();
            }
            """
        };

        yield return new object[]
        {
            53, true, true, NullHandling.SetNull, "IList<string>?", "IList<string>?",
            "target.Prop1 = sourceClass.Prop1;"
        };
    }

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Test data")]
    public static IEnumerable<object[]> CollectionNullHandlingAutoTestData()
    {
        yield return new object[]
        {
            54, true, false, NullHandling.Auto, "int[]?", "int[]?",
            "target.Prop1 = sourceClass.Prop1;"
        };

        yield return new object[]
        {
            55, true, false, NullHandling.Auto, "int[]?", "int[]",
            "target.Prop1 = sourceClass.Prop1 ?? global::System.Array.Empty<int>();"
        };

        yield return new object[]
        {
            56, true, false, NullHandling.Auto, "int[]", "int[]?",
            "target.Prop1 = sourceClass.Prop1;"
        };

        yield return new object[]
        {
            57, true, false, NullHandling.Auto, "int[]", "int[]",
            "target.Prop1 = sourceClass.Prop1;"
        };

        yield return new object[]
        {
            58, true, false, NullHandling.Auto, "List<MiddleClass>", "IEnumerable<MiddleClass>",
            "target.Prop1 = sourceClass.Prop1;"
        };

        yield return new object[]
        {
            59, true, false, NullHandling.Auto, "List<MiddleClass>", "IEnumerable<MiddleClass>?",
            "target.Prop1 = sourceClass.Prop1"
        };

        yield return new object[]
        {
            60, true, false, NullHandling.Auto, "List<MiddleClass>?", "IEnumerable<MiddleClass>",
            "target.Prop1 = sourceClass.Prop1 ?? global::System.Linq.Enumerable.Empty<global::MapTo.Tests.MiddleClass>();"
        };

        yield return new object[]
        {
            61, true, false, NullHandling.Auto, "List<MiddleClass>?", "IEnumerable<MiddleClass>?",
            "target.Prop1 = sourceClass.Prop1"
        };

        yield return new object[]
        {
            62, true, false, NullHandling.Auto, "List<MiddleClass>", "IEnumerable<MappedMiddleClass>",
            "target.Prop1 = sourceClass.Prop1.Select<global::MapTo.Tests.MiddleClass, global::MapTo.Tests.MappedMiddleClass>(global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass).ToArray()"
        };

        yield return new object[]
        {
            67, true, false, NullHandling.Auto, "List<MiddleClass>", "IEnumerable<MappedMiddleClass>?",
            "target.Prop1 = sourceClass.Prop1.Select<global::MapTo.Tests.MiddleClass, global::MapTo.Tests.MappedMiddleClass>(global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass).ToArray()"
        };

        yield return new object[]
        {
            68, true, false, NullHandling.Auto, "List<MiddleClass>?", "IEnumerable<MappedMiddleClass>",
            "target.Prop1 = sourceClass.Prop1?.Select<global::MapTo.Tests.MiddleClass, global::MapTo.Tests.MappedMiddleClass>(global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass).ToArray() ?? global::System.Linq.Enumerable.Empty<global::MapTo.Tests.MappedMiddleClass>();"
        };

        yield return new object[]
        {
            69, true, false, NullHandling.Auto, "List<MiddleClass>?", "IEnumerable<MappedMiddleClass>?",
            "target.Prop1 = sourceClass.Prop1?.Select<global::MapTo.Tests.MiddleClass, global::MapTo.Tests.MappedMiddleClass>(global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass).ToArray()"
        };

        yield return new object[]
        {
            70, true, true, NullHandling.Auto, "int[]?", "int[]?",
            """
            if (sourceClass.Prop1 is not null)
            {
                target.Prop1 = MapToInt32Array(sourceClass.Prop1);
            }
            """
        };

        yield return new object[]
        {
            71, true, true, NullHandling.Auto, "int[]?", "int[]",
            "target.Prop1 = sourceClass.Prop1 is null ? global::System.Array.Empty<int>() : MapToInt32Array(sourceClass.Prop1);"
        };

        yield return new object[]
        {
            72, true, true, NullHandling.Auto, "int[]", "int[]?",
            "target.Prop1 = MapToInt32Array(sourceClass.Prop1);"
        };

        yield return new object[]
        {
            73, true, true, NullHandling.Auto, "int[]", "int[]",
            "target.Prop1 = MapToInt32Array(sourceClass.Prop1);"
        };

        yield return new object[]
        {
            74, true, true, NullHandling.Auto, "List<MiddleClass>", "IEnumerable<MiddleClass>",
            "target.Prop1 = sourceClass.Prop1;"
        };

        yield return new object[]
        {
            75, true, true, NullHandling.Auto, "List<MiddleClass>", "IEnumerable<MiddleClass>?",
            "target.Prop1 = sourceClass.Prop1"
        };

        yield return new object[]
        {
            76, true, true, NullHandling.Auto, "List<MiddleClass>?", "IEnumerable<MiddleClass>",
            "target.Prop1 = sourceClass.Prop1 ?? global::System.Linq.Enumerable.Empty<global::MapTo.Tests.MiddleClass>();"
        };

        yield return new object[]
        {
            77, true, true, NullHandling.Auto, "List<MiddleClass>?", "IEnumerable<MiddleClass>?",
            "target.Prop1 = sourceClass.Prop1"
        };

        yield return new object[]
        {
            78, true, true, NullHandling.Auto, "List<MiddleClass>", "IEnumerable<MappedMiddleClass>",
            "target.Prop1 = sourceClass.Prop1.Select<global::MapTo.Tests.MiddleClass, global::MapTo.Tests.MappedMiddleClass>(global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass).ToArray();"
        };

        yield return new object[]
        {
            79, true, true, NullHandling.Auto, "List<MiddleClass>", "IEnumerable<MappedMiddleClass>?",
            "target.Prop1 = sourceClass.Prop1.Select<global::MapTo.Tests.MiddleClass, global::MapTo.Tests.MappedMiddleClass>(global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass).ToArray();"
        };

        yield return new object[]
        {
            80, true, true, NullHandling.Auto, "List<MiddleClass>?", "IEnumerable<MappedMiddleClass>",
            "target.Prop1 = sourceClass.Prop1?.Select<global::MapTo.Tests.MiddleClass, global::MapTo.Tests.MappedMiddleClass>(global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass).ToArray() ?? global::System.Linq.Enumerable.Empty<global::MapTo.Tests.MappedMiddleClass>();"
        };

        yield return new object[]
        {
            81, true, true, NullHandling.Auto, "List<MiddleClass>?", "IEnumerable<MappedMiddleClass>?",
            "target.Prop1 = sourceClass.Prop1?.Select<global::MapTo.Tests.MiddleClass, global::MapTo.Tests.MappedMiddleClass>(global::MapTo.Tests.MiddleClassToMappedMiddleClassMapToExtensions.MapToMappedMiddleClass).ToArray();"
        };
    }
}