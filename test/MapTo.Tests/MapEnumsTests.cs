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

    [Theory]
    [InlineData("[IgnoreEnumMember] Value3", null)]
    [InlineData(null, "[IgnoreEnumMember] Value3")]
    public void When_IgnoreEnumMemberAttributeIsOnMember_Should_NotReportDiagnotics(string? sourceExtraMember, string? targetExtraMember)
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile().WithBody(
            $$"""
              public enum SourceEnum
              {
                  Value1,
                  Value2
                  {{(sourceExtraMember is not null ? $",{sourceExtraMember}" : string.Empty)}}
              }

              public enum TargetEnum
              {
                  Value1,
                  Value2
                  {{(targetExtraMember is not null ? $",{targetExtraMember}" : string.Empty)}}
              }

              public record SourceClass(SourceEnum Prop1);

              [MapFrom(typeof(SourceClass), StrictEnumMapping = StrictEnumMapping.SourceAndTarget)]
              public record TargetClass(TargetEnum Prop1);
              """);

        // Act
        var (_, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
    }

    [Fact]
    public void When_IgnoreEnumMemberAttributeIsOnEnumAndTargetHasExtraMember_Should_NotReportDiagnostics()
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

            [IgnoreEnumMember(TargetEnum.Value3)]
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
        var (_, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
    }

    [Fact]
    public void When_IgnoreEnumMemberAttributeIsOnEnumAndSourceHasExtraMember_Should_NotReportDiagnostics()
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

            [IgnoreEnumMember(SourceEnum.Value2)]
            [IgnoreEnumMember(SourceEnum.Value3)]
            public enum TargetEnum
            {
                Value1
            }

            public record SourceClass(SourceEnum Prop1);

            [MapFrom(typeof(SourceClass), StrictEnumMapping = StrictEnumMapping.SourceAndTarget)]
            public record TargetClass(TargetEnum Prop1);
            """);

        // Act
        var (_, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
    }

    [Fact]
    public void When_IgnoreEnumMemberAttributeIsOnMapClassAndSourceHasExtraMember_Should_NotReportDiagnostics()
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
                Value1
            }

            public record SourceClass(SourceEnum Prop1);

            [IgnoreEnumMember(SourceEnum.Value2)]
            [IgnoreEnumMember(SourceEnum.Value3)]
            [MapFrom(typeof(SourceClass), StrictEnumMapping = StrictEnumMapping.SourceAndTarget)]
            public record TargetClass(TargetEnum Prop1);
            """);

        // Act
        var (_, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
    }

    [Fact]
    public void When_IgnoreEnumMemberAttributeIsOnMapClassAndTargetHasExtraMember_Should_NotReportDiagnostics()
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

            [IgnoreEnumMember(TargetEnum.Value3)]
            [MapFrom(typeof(SourceClass), StrictEnumMapping = StrictEnumMapping.SourceAndTarget)]
            public record TargetClass(TargetEnum Prop1);
            """);

        // Act
        var (_, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
    }

    [Fact]
    public void When_IgnoreEnumMemberAttributeIsOnEnumMemberWithParameter_Should_ReportDiagnostics()
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
                [IgnoreEnumMember(TargetEnum.Value3)]
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
        var attribute = targetEnumSymbol.GetMembers().OfType<IFieldSymbol>().First(m => m.Name == "Value3").GetAttributes().First();

        diagnostics.ShouldNotBeSuccessful(
            DiagnosticsFactory.IgnoreEnumMemberWithParameterOnMemberError(attribute, KnownTypes.Create(compilation).IgnoreEnumMemberAttributeTypeSymbol),
            ignoreDiagnosticsIds: new[] { "MT3002" });
    }

    [Fact]
    public void When_IgnoreEnumMemberAttributeIsOnEnumWithoutParameter_Should_ReportDiagnostics()
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

            [IgnoreEnumMember()]
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
        var attribute = targetEnumSymbol.GetAttributes().First();

        diagnostics.ShouldNotBeSuccessful(
            DiagnosticsFactory.IgnoreEnumMemberWithoutParameterTypeError(attribute, KnownTypes.Create(compilation).IgnoreEnumMemberAttributeTypeSymbol),
            ignoreDiagnosticsIds: new[] { "MT3002" });
    }

    [Fact]
    public void When_IgnoreEnumMemberAttributeIsOnMapClassWithoutParameter_Should_ReportDiagnostics()
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
                Value3,
                Value4
            }

            public record SourceClass(SourceEnum Prop1);

            [IgnoreEnumMember]
            [MapFrom(typeof(SourceClass), StrictEnumMapping = StrictEnumMapping.SourceAndTarget)]
            public record TargetClass(TargetEnum Prop1);
            """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        var targetClassSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetClass").ShouldNotBeNull();
        var attribute = targetClassSymbol.GetAttributes().First();

        diagnostics.ShouldNotBeSuccessful(
            DiagnosticsFactory.IgnoreEnumMemberWithoutParameterTypeError(attribute, KnownTypes.Create(compilation).IgnoreEnumMemberAttributeTypeSymbol),
            ignoreDiagnosticsIds: new[] { "MT3002" });
    }

    [Fact]
    public void When_DirectlyMappingEnumByValue_Should_CastTheSourceEnum()
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

            [MapFrom(typeof(SourceEnum))]
            public enum TargetEnum
            {
                Value1,
                Value2
            }
            """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var targetClass = compilation.GetClassDeclaration("SourceEnumMapToExtensions").ShouldNotBeNull();
        targetClass.ShouldContain(
            """
            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("sourceEnum")]
            public static TargetEnum? MapToTargetEnum(this SourceEnum? sourceEnum)
            {
                return sourceEnum is null ? null : (TargetEnum)sourceEnum;
            }
            """);
    }

    [Fact]
    public void When_DirectlyMappingEnumByName_Should_MapSourceEnumMembers()
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

            [MapFrom(typeof(SourceEnum), EnumMappingStrategy = EnumMappingStrategy.ByName)]
            public enum TargetEnum
            {
                Value1,
                Value2
            }
            """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var targetClass = compilation.GetClassDeclaration("SourceEnumMapToExtensions").ShouldNotBeNull();
        targetClass.ShouldContain(
            """
            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("sourceEnum")]
            public static TargetEnum? MapToTargetEnum(this SourceEnum? sourceEnum)
            {
                if (sourceEnum is null)
                {
                    return null;
                }

               return sourceEnum switch
               {
                   global::MapTo.Tests.SourceEnum.Value1 => global::MapTo.Tests.TargetEnum.Value1,
                   global::MapTo.Tests.SourceEnum.Value2 => global::MapTo.Tests.TargetEnum.Value2,
                   _ => throw new global::System.ArgumentOutOfRangeException("sourceEnum", sourceEnum, "Unable to map enum 'MapTo.Tests.SourceEnum' to 'MapTo.Tests.TargetEnum'.")
               };
            }
            """);
    }

    [Fact]
    public void When_DirectlyMappingEnumByNameCaseInsensitive_Should_MapSourceEnumMembers()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile().WithBody(
            """
            public enum SourceEnum
            {
                Value1,
                value2,
                Value3
            }

            [MapFrom(typeof(SourceEnum), EnumMappingStrategy = EnumMappingStrategy.ByNameCaseInsensitive)]
            public enum TargetEnum
            {
                Value1,
                Value2
            }
            """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var targetClass = compilation.GetClassDeclaration("SourceEnumMapToExtensions").ShouldNotBeNull();
        targetClass.ShouldContain(
            """
            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("sourceEnum")]
            public static TargetEnum? MapToTargetEnum(this SourceEnum? sourceEnum)
            {
                if (sourceEnum is null)
                {
                    return null;
                }

                return sourceEnum switch
                {
                    global::MapTo.Tests.SourceEnum.Value1 => global::MapTo.Tests.TargetEnum.Value1,
                    global::MapTo.Tests.SourceEnum.value2 => global::MapTo.Tests.TargetEnum.Value2,
                    _ => throw new global::System.ArgumentOutOfRangeException("sourceEnum", sourceEnum, "Unable to map enum 'MapTo.Tests.SourceEnum' to 'MapTo.Tests.TargetEnum'.")
                };
            }
            """);
    }
}