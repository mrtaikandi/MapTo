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
}