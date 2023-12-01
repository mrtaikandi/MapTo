using MapTo.Diagnostics;

namespace MapTo.Tests;

public class MapEnumsTests
{
    private readonly ITestOutputHelper _output;

    public MapEnumsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void When_TargetClassEnumStrategyIsByName_Should_GenerateEnumMappingMethod()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleClassWithTwoEnumProperties(enumMappingStrategy: EnumMappingStrategy.ByName);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var targetClass = compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldNotBeNull();
        targetClass.ShouldContain(
            """
            target.Prop1 = MapToSourceEnum(sourceClass.Prop1);
            target.Prop2 = MapToSourceEnum(sourceClass.Prop2);
            """);

        targetClass.ShouldContain(
            """
            private static global::MapTo.Tests.TargetEnum MapToSourceEnum(global::MapTo.Tests.SourceEnum source)
            {
                return source switch
                {
                    global::MapTo.Tests.SourceEnum.Value1 => global::MapTo.Tests.TargetEnum.Value1,
                    global::MapTo.Tests.SourceEnum.Value2 => global::MapTo.Tests.TargetEnum.Value2,
                    _ => throw new global::System.ArgumentOutOfRangeException("source", source, "Unable to map enum value 'MapTo.Tests.SourceEnum' to 'MapTo.Tests.TargetEnum'.")
                };
            }
            """);
    }

    [Fact]
    public void When_TargetClassEnumStrategyIsByValue_Should_NotGenerateEnumMappingMethod()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleClassWithTwoEnumProperties(enumMappingStrategy: EnumMappingStrategy.ByValue);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var targetClass = compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldNotBeNull();
        targetClass.ShouldContain(
            """
            target.Prop1 = (global::MapTo.Tests.TargetEnum)sourceClass.Prop1;
            target.Prop2 = (global::MapTo.Tests.TargetEnum)sourceClass.Prop2;
            """);
    }

    [Fact]
    public void When_TargetRecordEnumStrategyIsByValue_Should_NotGenerateEnumMappingMethod()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile().WithBody(
            """
            public enum SourceEnum
            {
                Value1,
                Value2
            }

            public enum TargetEnum
            {
                Value1,
                Value2
            }

            public record SourceClass(SourceEnum Prop1);

            [MapFrom(typeof(SourceClass))]
            public record TargetClass(TargetEnum Prop1);
            """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        compilation.Dump(_output);
        diagnostics.ShouldBeSuccessful();

        var targetClass = compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldNotBeNull();
        targetClass.ShouldContain("return new TargetClass((global::MapTo.Tests.TargetEnum)sourceClass.Prop1);");
    }

    [Fact]
    public void When_TargetEnumNotAnnotated_Should_MapEnumByValue()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleClassWithTwoEnumProperties(enumMappingStrategy: null);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var targetClass = compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldNotBeNull();
        targetClass.ShouldContain(
            """
            target.Prop1 = (global::MapTo.Tests.TargetEnum)sourceClass.Prop1;
            target.Prop2 = (global::MapTo.Tests.TargetEnum)sourceClass.Prop2;
            """);
    }

    [Fact]
    public void When_TargetEnumIsByNameCaseSensitive_Should_MapMembersWithSameCasing()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleClassWithTwoEnumProperties(enumMappingStrategy: EnumMappingStrategy.ByName, lowercaseTargetEnum: true);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var targetClass = compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldNotBeNull();
        targetClass.ShouldContain(
            """
            private static global::MapTo.Tests.TargetEnum MapToSourceEnum(global::MapTo.Tests.SourceEnum source)
            {
                return source switch
                {
                    global::MapTo.Tests.SourceEnum.Value1 => global::MapTo.Tests.TargetEnum.Value1,
                    _ => throw new global::System.ArgumentOutOfRangeException("source", source, "Unable to map enum value 'MapTo.Tests.SourceEnum' to 'MapTo.Tests.TargetEnum'.")
                };
            }
            """);
    }

    [Fact]
    public void When_TargetRecordEnumStrategyIsByName_Should_GenerateEnumMappingMethod()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile().WithBody(
            """
            public enum SourceEnum
            {
                Value1,
                Value2,
                Value3
            }

            public enum TargetEnum
            {
                Value1,
                Value2
            }

            public record SourceClass(SourceEnum Prop1);

            [MapFrom(typeof(SourceClass), EnumMappingStrategy = EnumMappingStrategy.ByName)]
            public record TargetClass(TargetEnum Prop1);
            """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var targetClass = compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldNotBeNull();
        targetClass.ShouldContain("return new TargetClass(MapToSourceEnum(sourceClass.Prop1));");
        targetClass.ShouldContain(
            """
            private static global::MapTo.Tests.TargetEnum MapToSourceEnum(global::MapTo.Tests.SourceEnum source)
            {
                return source switch
                {
                    global::MapTo.Tests.SourceEnum.Value1 => global::MapTo.Tests.TargetEnum.Value1,
                    global::MapTo.Tests.SourceEnum.Value2 => global::MapTo.Tests.TargetEnum.Value2,
                    _ => throw new global::System.ArgumentOutOfRangeException("source", source, "Unable to map enum value 'MapTo.Tests.SourceEnum' to 'MapTo.Tests.TargetEnum'.")
                };
            }
            """);
    }

    [Fact]
    public void When_TargetEnumIsByNameCaseInsensitive_Should_IgnoreCasing()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleClassWithTwoEnumProperties(enumMappingStrategy: EnumMappingStrategy.ByNameCaseInsensitive, lowercaseTargetEnum: true);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var targetClass = compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldNotBeNull();
        targetClass.ShouldContain(
            """
            private static global::MapTo.Tests.TargetEnum MapToSourceEnum(global::MapTo.Tests.SourceEnum source)
            {
                return source switch
                {
                    global::MapTo.Tests.SourceEnum.Value1 => global::MapTo.Tests.TargetEnum.Value1,
                    global::MapTo.Tests.SourceEnum.Value2 => global::MapTo.Tests.TargetEnum.value2,
                    _ => throw new global::System.ArgumentOutOfRangeException("source", source, "Unable to map enum value 'MapTo.Tests.SourceEnum' to 'MapTo.Tests.TargetEnum'.")
                };
            }
            """);
    }

    [Fact]
    public void When_TargetEnumHasMapFromAttribute_Should_UseItToMap()
    {
        // Arrange
        var builder = ScenarioBuilder.EnumWithMapFromAttribute(EnumMappingStrategy.ByName);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var targetClass = compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldNotBeNull();
        targetClass.ShouldContain(
            """
            private static global::MapTo.Tests.TargetEnum MapToSourceEnum(global::MapTo.Tests.SourceEnum source)
            {
                return source switch
                {
                    global::MapTo.Tests.SourceEnum.Value1 => global::MapTo.Tests.TargetEnum.Value1,
                    global::MapTo.Tests.SourceEnum.Value2 => global::MapTo.Tests.TargetEnum.Value2,
                    _ => throw new global::System.ArgumentOutOfRangeException("source", source, "Unable to map enum value 'MapTo.Tests.SourceEnum' to 'MapTo.Tests.TargetEnum'.")
                };
            }
            """);
    }

    [Fact]
    public void When_TargetClassEnumStrategyHasFallbackValue_Should_DefaultToFallbackValueInsteadOfException()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleClassWithTwoEnumProperties(enumMappingStrategy: EnumMappingStrategy.ByName, fallbackValue: "TargetEnum.Value2");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var targetClass = compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldNotBeNull();
        targetClass.ShouldContain(
            """
            target.Prop1 = MapToSourceEnum(sourceClass.Prop1);
            target.Prop2 = MapToSourceEnum(sourceClass.Prop2);
            """);

        targetClass.ShouldContain(
            """
            private static global::MapTo.Tests.TargetEnum MapToSourceEnum(global::MapTo.Tests.SourceEnum source)
            {
                return source switch
                {
                    global::MapTo.Tests.SourceEnum.Value1 => global::MapTo.Tests.TargetEnum.Value1,
                    global::MapTo.Tests.SourceEnum.Value2 => global::MapTo.Tests.TargetEnum.Value2,
                    _ => global::MapTo.Tests.TargetEnum.Value2
                };
            }
            """);
    }

    [Fact]
    public void StrictEnumMappingIsSourceOnly_When_AllSourceEnumMembersHaveNotMapped_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile().WithBody(
            """
            public enum SourceEnum
            {
                Value1,
                Value2,
                Value3
            }

            public enum TargetEnum
            {
                Value1,
                Value2
            }

            public record SourceClass(SourceEnum Prop1);

            [MapFrom(typeof(SourceClass), StrictEnumMapping = StrictEnumMapping.SourceOnly)]
            public record TargetClass(TargetEnum Prop1);
            """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        var targetEnumSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetEnum").ShouldNotBeNull();
        var sourceEnumSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.SourceEnum").ShouldNotBeNull();
        var missingMember = sourceEnumSymbol.GetMembers().OfType<IFieldSymbol>().First(m => m.Name == "Value3");

        diagnostics.ShouldNotBeSuccessful(
            DiagnosticsFactory.StringEnumMappingSourceOnlyError(missingMember, targetEnumSymbol),
            ignoreDiagnosticsIds: new[] { "MT3002" });
    }

    [Fact]
    public void StrictEnumMappingIsSourceOnly_When_AllTargetEnumMembersHaveNotMapped_Should_NotReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile().WithBody(
            """
            public enum SourceEnum
            {
                Value1,
                Value2
            }

            public enum TargetEnum
            {
                Value1,
                Value2,
                Value3
            }

            public record SourceClass(SourceEnum Prop1);

            [MapFrom(typeof(SourceClass), StrictEnumMapping = StrictEnumMapping.SourceOnly)]
            public record TargetClass(TargetEnum Prop1);
            """);

        // Act
        var (_, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
    }

    [Fact]
    public void StrictEnumMappingIsTargetOnly_When_AllTargetEnumMembersHaveNotMapped_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile().WithBody(
            """
            public enum SourceEnum
            {
                Value1,
                Value2
            }

            public enum TargetEnum
            {
                Value1,
                Value2,
                Value3
            }

            public record SourceClass(SourceEnum Prop1);

            [MapFrom(typeof(SourceClass), StrictEnumMapping = StrictEnumMapping.TargetOnly)]
            public record TargetClass(TargetEnum Prop1);
            """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        var targetEnumSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetEnum").ShouldNotBeNull();
        var sourceEnumSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.SourceEnum").ShouldNotBeNull();
        var missingMember = targetEnumSymbol.GetMembers().OfType<IFieldSymbol>().First(m => m.Name == "Value3");

        diagnostics.ShouldNotBeSuccessful(
            DiagnosticsFactory.StringEnumMappingTargetOnlyError(missingMember, sourceEnumSymbol),
            ignoreDiagnosticsIds: new[] { "MT3002" });
    }

    [Fact]
    public void StrictEnumMappingIsTargetOnly_When_AllSourceEnumMembersHaveNotMapped_Should_NotReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile().WithBody(
            """
            public enum SourceEnum
            {
                Value1,
                Value2,
                Value3
            }

            public enum TargetEnum
            {
                Value1,
                Value2
            }

            public record SourceClass(SourceEnum Prop1);

            [MapFrom(typeof(SourceClass), StrictEnumMapping = StrictEnumMapping.TargetOnly)]
            public record TargetClass(TargetEnum Prop1);
            """);

        // Act
        var (_, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
    }

    [Fact]
    public void StrictEnumMappingIsSourceAndTarget_When_AllTargetEnumMembersHaveNotMapped_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile().WithBody(
            """
            public enum SourceEnum
            {
                Value1,
                Value2
            }

            public enum TargetEnum
            {
                Value1,
                Value2,
                Value3
            }

            public record SourceClass(SourceEnum Prop1);

            [MapFrom(typeof(SourceClass), StrictEnumMapping = StrictEnumMapping.SourceAndTarget)]
            public record TargetClass(TargetEnum Prop1);
            """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        var targetEnumSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetEnum").ShouldNotBeNull();
        var sourceEnumSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.SourceEnum").ShouldNotBeNull();
        var missingMember = targetEnumSymbol.GetMembers().OfType<IFieldSymbol>().First(m => m.Name == "Value3");

        diagnostics.ShouldNotBeSuccessful(
            DiagnosticsFactory.StringEnumMappingTargetOnlyError(missingMember, sourceEnumSymbol),
            ignoreDiagnosticsIds: new[] { "MT3002" });
    }

    [Fact]
    public void StrictEnumMappingIsSourceAndTarget_When_AllSourceEnumMembersHaveNotMapped_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile().WithBody(
            """
            public enum SourceEnum
            {
                Value1,
                Value2,
                Value3
            }

            public enum TargetEnum
            {
                Value1,
                Value2
            }

            public record SourceClass(SourceEnum Prop1);

            [MapFrom(typeof(SourceClass), StrictEnumMapping = StrictEnumMapping.SourceAndTarget)]
            public record TargetClass(TargetEnum Prop1);
            """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        var targetEnumSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetEnum").ShouldNotBeNull();
        var sourceEnumSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.SourceEnum").ShouldNotBeNull();
        var missingMember = sourceEnumSymbol.GetMembers().OfType<IFieldSymbol>().First(m => m.Name == "Value3");

        diagnostics.ShouldNotBeSuccessful(
            DiagnosticsFactory.StringEnumMappingSourceOnlyError(missingMember, targetEnumSymbol),
            ignoreDiagnosticsIds: new[] { "MT3002" });
    }
}