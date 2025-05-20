namespace MapTo.Tests;

public class MapTests(ITestOutputHelper output)
{
    [Fact]
    public void When_MapAttributeIsUsed_Should_MapSourceAndTarget()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddEmptyFile().WithBody(
            """
            using Source = Test.SourceNamespace;
            using Target = AnotherTest.TargetNamespace;

            [assembly: MapTo.Map<Source.SourceClass, Target.TargetClass>()]
            """);

        builder.AddEmptyFile().WithBody(
            """
            namespace Test.SourceNamespace;

            public sealed class SourceClass
            {
                public required string Name { get; init; }
                public required ulong Id { get; init; }
            }
            """);

        builder.AddEmptyFile().WithBody(
            """
            namespace AnotherTest.TargetNamespace;

            public record TargetClass
            {
                public required string Name { get; init; }
                public required ulong Id { get; init; }
            }
            """);

        // Act
        var (compilation, diagnostics) = builder.Compile();
        compilation.Dump(output);

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation
            .GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
            .ShouldContain("public static global::AnotherTest.TargetNamespace.TargetClass[]? MapToTargetClasses(this global::Test.SourceNamespace.SourceClass[]? source)");
    }

    [Fact]
    public void When_MapAttributeIsUsedAndTargetHasMultipleConstructors_Should_MapSourceAndTarget()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddEmptyFile().WithBody(
            """
            using Source = Test.SourceNamespace;
            using Target = AnotherTest.TargetNamespace;

            [assembly: MapTo.Map<Source.SourceClass, Target.TargetClass>()]
            """);

        builder.AddEmptyFile().WithBody(
            """
            namespace Test.SourceNamespace;

            public sealed class SourceClass
            {
                public required string Name { get; init; }
                public required ulong Id { get; init; }
            }
            """);

        builder.AddEmptyFile().WithBody(
            """
            namespace AnotherTest.TargetNamespace;

            public sealed partial class TargetClass
            {
                public TargetClass() { }

                public TargetClass(TargetClass other) { }

                public required string Name { get; init; }
                public required ulong Id { get; init; }
            }
            """);

        // Act
        var (compilation, diagnostics) = builder.Compile();
        compilation.Dump(output);

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation
            .GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
            .ShouldContain("public static global::AnotherTest.TargetNamespace.TargetClass[]? MapToTargetClasses(this global::Test.SourceNamespace.SourceClass[]? source)");
    }

    [Fact]
    public void When_NestedMappedPropertyIsUsed_Should_UseTheMapMethodOfTheNestedType()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddEmptyFile().WithBody(
            """
            [assembly: MapTo.Map<SourceNamespace.Class1, TargetNamespace.Class1>]
            [assembly: MapTo.Map<SourceNamespace.Class2, TargetNamespace.Class2>]
            """);

        builder.AddEmptyFile().WithBody(
            """
            using System.Collections.Generic;

            namespace SourceNamespace;

            public sealed class Class1
            {
                public required string Name { get; init; }
                public required Class2[] Items { get; init; }
            }

            public sealed class Class2
            {
                public required ulong Id { get; init; }
                public required string Name { get; init; }
            }
            """);

        builder.AddEmptyFile().WithBody(
            """
            namespace TargetNamespace;

            public sealed class Class1
            {
                public required string Name { get; init; }
                public required Class2[] Items { get; init; }
            }

            public record Class2
            {
                public required ulong Id { get; init; }
                public required string Name { get; init; }
            }
            """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation
            .GetClassDeclaration("Class1ToClass1MapToExtensions")
            .ShouldContain(
                """
                public static global::TargetNamespace.Class1? MapToClass1(this global::SourceNamespace.Class1? class1)
                {
                    if (class1 is null)
                    {
                        return null;
                    }

                    return new Class1
                    {
                        Name = class1.Name,
                        Items = MapToClass2Array(class1.Items)
                    };
                }
                """);
    }

    [Fact]
    public void When_MappingDifferentSourcesToSameTarget_Should_GenerateSeparateFiles()
    {
        // Arrange
        var builder = new TestSourceBuilder(
            supportNullReferenceTypes: true,
            analyzerConfigOptions: new Dictionary<string, string>
            {
                [nameof(CodeGeneratorOptions.GenerateFullExtensionClassNames)] = "true"
            });

        builder.AddEmptyFile().WithBody(
            """
            using MapTo;
            using System;

            namespace Contracts1;

            [Map<Contract1, Models.Variable>]
            public sealed record Contract1(Guid Id, string Name, string Value, string? Description, bool IsSensitive, int Version);
            """);

        builder.AddEmptyFile().WithBody(
            """
            using MapTo;
            using System;

            namespace Contracts2;

            [Map<Contract1, Models.Variable>]
            public sealed record Contract1(Guid Id, string Name, string Value, string? Description, bool IsSensitive, int Version);
            """);

        builder.AddEmptyFile().WithBody(
            """
            using System;

            namespace Models;

            public sealed class Variable
            {
                public string? Description { get; set; }
                public Guid Id { get; init; } = Guid.NewGuid();
                public bool IsSensitive { get; set; }
                public required string Name { get; set; }
                public required string Value { get; set; }
                public int Version { get; init; } = 1;
            }
            """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        compilation.GetClassDeclaration("Contracts2Contract1ToModelsVariableMapToExtensions").ShouldNotBeNull();
        compilation.GetClassDeclaration("Contracts1Contract1ToModelsVariableMapToExtensions").ShouldNotBeNull();
    }
}