using MapTo.Mappings;

namespace MapTo.Tests;

public class MapConfigurationTests(ITestOutputHelper output)
{
    [Fact]
    public void When_ConfigurationMethodIsUsed_Should_CorrectlyUpdateTheConfigurations()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(LanguageVersion.Latest));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Prop1");
        sourceFile.AddClass(Accessibility.Public, "TargetClass").WithProperty<int>("Prop1");

        builder.AddFile(supportNullableReferenceTypes: true, fileName: "ConfigurationFile", usings: ["MapTo.Configuration"]).WithBody(
            """
            [Map<SourceClass, TargetClass>(nameof(Configuration.MappingConfiguration))]
            internal static class Configuration
            {
                public static void MappingConfiguration(MappingConfiguration<SourceClass, TargetClass> config)
                {
                    config.BeforeMap = nameof(TestBeforeMethodMap);
                    config.AfterMap = nameof(TestAfterMethodMap);
                    config.CopyPrimitiveArrays = true;
                    config.NullHandling = NullHandling.SetNull;
                    config.ReferenceHandling = ReferenceHandling.Enabled;
                    config.EnumMappingStrategy = EnumMappingStrategy.ByNameCaseInsensitive;
                    config.StrictEnumMapping = StrictEnumMapping.SourceAndTarget;
                    config.ProjectTo = ProjectionType.None;
                    config.EnumMappingFallbackValue = 2;
                }
            }
            """);

        var (compilation, _) = builder.Compile(false);
        var sourceTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.SourceClass").ShouldNotBeNull();
        var targetTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetClass").ShouldNotBeNull();

        var configurationSyntax = compilation.GetClassDeclaration("Configuration").ShouldNotBeNull();
        var configurationTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.Configuration").ShouldNotBeNull();
        var semanticModel = compilation.GetSemanticModel(configurationSyntax.SyntaxTree);
        var attribute = configurationTypeSymbol.GetAttributes().ShouldHaveSingleItem();

        // Act
        var mappingConfiguration = attribute.ToMappingConfiguration(semanticModel, sourceTypeSymbol, targetTypeSymbol);

        // Assert
        mappingConfiguration.IsSuccess.ShouldBeTrue();

        var configuration = mappingConfiguration.Value;
        configuration.BeforeMap.ShouldBe("nameof(TestBeforeMethodMap)");
        configuration.AfterMap.ShouldBe("nameof(TestAfterMethodMap)");
        configuration.CopyPrimitiveArrays.ShouldNotBeNull().ShouldBeTrue();
        configuration.NullHandling.ShouldBe(NullHandling.SetNull);
        configuration.ReferenceHandling.ShouldBe(ReferenceHandling.Enabled);
        configuration.EnumMappingStrategy.ShouldBe(EnumMappingStrategy.ByNameCaseInsensitive);
        configuration.StrictEnumMapping.ShouldBe(StrictEnumMapping.SourceAndTarget);
        configuration.ProjectTo.ShouldBe(ProjectionType.None);
        configuration.EnumMappingFallbackValue.ShouldBe(2);
    }

    [Fact]
    public void When_ConfigurationMethodIsUsedAtAssemblyLevel_Should_CorrectlyUpdateTheConfigurations()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(LanguageVersion.Latest));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Prop1");
        sourceFile.AddClass(Accessibility.Public, "TargetClass").WithProperty<int>("Prop1");

        builder.AddEmptyFile().WithBody(
            """
            [assembly: MapTo.Map<MapTo.Tests.SourceClass, MapTo.Tests.TargetClass>(nameof(MapTo.Tests.Configuration.MappingConfiguration))]
            """);

        builder.AddFile(supportNullableReferenceTypes: true, fileName: "ConfigurationFile", usings: ["MapTo.Configuration"]).WithBody(
            """
            [Map<SourceClass, TargetClass>(nameof(Configuration.MappingConfiguration))]
            internal static class Configuration
            {
                public static void MappingConfiguration(MappingConfiguration<SourceClass, TargetClass> config)
                {
                    config.BeforeMap = nameof(TestBeforeMethodMap);
                    config.AfterMap = nameof(TestAfterMethodMap);
                    config.CopyPrimitiveArrays = true;
                    config.NullHandling = NullHandling.SetNull;
                    config.ReferenceHandling = ReferenceHandling.Enabled;
                    config.EnumMappingStrategy = EnumMappingStrategy.ByNameCaseInsensitive;
                    config.StrictEnumMapping = StrictEnumMapping.SourceAndTarget;
                    config.ProjectTo = ProjectionType.None;
                    config.EnumMappingFallbackValue = 2;
                }
            }
            """);

        var (compilation, _) = builder.Compile(false);
        var sourceTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.SourceClass").ShouldNotBeNull();
        var targetTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetClass").ShouldNotBeNull();

        var configurationSyntax = compilation.GetClassDeclaration("Configuration").ShouldNotBeNull();
        var configurationTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.Configuration").ShouldNotBeNull();
        var semanticModel = compilation.GetSemanticModel(configurationSyntax.SyntaxTree);
        var attribute = configurationTypeSymbol.GetAttributes().ShouldHaveSingleItem();

        // Act
        var mappingConfiguration = attribute.ToMappingConfiguration(semanticModel, sourceTypeSymbol, targetTypeSymbol);

        // Assert
        mappingConfiguration.IsSuccess.ShouldBeTrue();

        var configuration = mappingConfiguration.Value;
        configuration.BeforeMap.ShouldBe("nameof(TestBeforeMethodMap)");
        configuration.AfterMap.ShouldBe("nameof(TestAfterMethodMap)");
        configuration.CopyPrimitiveArrays.ShouldNotBeNull().ShouldBeTrue();
        configuration.NullHandling.ShouldBe(NullHandling.SetNull);
        configuration.ReferenceHandling.ShouldBe(ReferenceHandling.Enabled);
        configuration.EnumMappingStrategy.ShouldBe(EnumMappingStrategy.ByNameCaseInsensitive);
        configuration.StrictEnumMapping.ShouldBe(StrictEnumMapping.SourceAndTarget);
        configuration.ProjectTo.ShouldBe(ProjectionType.None);
        configuration.EnumMappingFallbackValue.ShouldBe(2);
    }

    [Fact]
    public void When_SourceAndTargetPropertyTypesAreNotCompatibleButHasExplicitPropertyTypeConverter_Should_NotReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(LanguageVersion.Latest));
        var sourceFile = builder.AddFile();
        builder.AddFile(supportNullableReferenceTypes: true).WithBody(
            """
            [Map<SourceClass, TargetClass>(nameof(Configuration.MappingConfiguration))]
            internal static class Configuration
            {
                public static int StringToIntTypeConverter(string source) => int.Parse(source);
                public static int StringToIntTypeConverter(string source, object[]? parameters) => int.Parse(source);

                public static void MappingConfiguration(MappingConfiguration<SourceClass, TargetClass> config)
                {
                    config.ForProperty(x => x.Prop1).UseTypeConverter<string>(StringToIntTypeConverter);
                }
            }
            """);

        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<string>("Prop1").WithProperty<int>("Prop2");
        sourceFile.AddClass(Accessibility.Public, "TargetClass").WithProperty<int>("Prop1").WithProperty<int>("Prop2");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain("Prop1 = global::MapTo.Tests.Configuration.StringToIntTypeConverter(sourceClass.Prop1),");
    }

    [Fact]
    public void When_SourceAndTargetPropertyTypesAreNotCompatibleButHasExplicitPropertyTypeConverterWithParameters_Should_NotReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(LanguageVersion.Latest));
        var sourceFile = builder.AddFile();
        builder.AddFile(supportNullableReferenceTypes: true).WithBody(
            """
            [Map<SourceClass, TargetClass>(nameof(Configuration.MappingConfiguration))]
            internal static class Configuration
            {
                public static int StringToIntTypeConverter(string source, object[]? parameters) => int.Parse(source);

                public static void MappingConfiguration(MappingConfiguration<SourceClass, TargetClass> config)
                {
                    config.ForProperty(x => x.Prop1).UseTypeConverter<string>(StringToIntTypeConverter, new object[] { 1 });
                }
            }
            """);

        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<string>("Prop1").WithProperty<string>("Prop2");
        sourceFile.AddClass(Accessibility.Public, "TargetClass").WithProperty<int>("Prop1").WithProperty<string>("Prop2");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain("Prop1 = global::MapTo.Tests.Configuration.StringToIntTypeConverter(sourceClass.Prop1, new object[] { 1 }),");
    }

    [Theory]
    [InlineData("UseTypeConverter<string>(x => int.Parse(x))")]
    [InlineData("UseTypeConverter<string>((x) => int.Parse(x))")]
    [InlineData("UseTypeConverter((string x) => int.Parse(x))")]
    public void When_PropertyTypeConverterIsLambda_Should_GenerateStaticMethod(string converter)
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(LanguageVersion.Latest));
        var sourceFile = builder.AddFile();
        builder.AddFile(supportNullableReferenceTypes: true).WithBody(
            $$"""
              [Map<SourceClass, TargetClass>(nameof(Configuration.MappingConfiguration))]
              internal static class Configuration
              {
                  public static int StringToIntTypeConverter(string source, object[]? parameters) => int.Parse(source);

                  public static void MappingConfiguration(MappingConfiguration<SourceClass, TargetClass> config)
                  {
                      config.ProjectTo = ProjectionType.None;
                      config.ForProperty(x => x.Prop1).{{converter}};
                  }
              }
              """);

        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<string>("Prop1").WithProperty<string>("Prop2");
        sourceFile.AddClass(Accessibility.Public, "TargetClass").WithProperty<int>("Prop1").WithProperty<string>("Prop2");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain(
            """
            public static class SourceClassMapToExtensions
            {
                [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("sourceClass")]
                public static TargetClass? MapToTargetClass(this SourceClass? sourceClass)
                {
                    if (sourceClass is null)
                    {
                        return null;
                    }

                    return new TargetClass
                    {
                        Prop1 = Generated_Prop1ToInt32Converter(sourceClass.Prop1),
                        Prop2 = sourceClass.Prop2
                    };
                }

                private static int Generated_Prop1ToInt32Converter(string x) => int.Parse(x);
            }
            """);
    }

    [Theory]
    [InlineData("UseTypeConverter<string>((x, p) => int.Parse(x), new object[] { 1 })")]
    [InlineData("UseTypeConverter((string x, object[]? p) => int.Parse(x), new object[] { 1 })")]
    public void When_PropertyTypeConverterIsLambdaWithParameter_Should_GenerateStaticMethodAndPassTheParameter(string converter)
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(LanguageVersion.Latest));
        var sourceFile = builder.AddFile();
        builder.AddFile(supportNullableReferenceTypes: true).WithBody(
            $$"""
              [Map<SourceClass, TargetClass>(nameof(Configuration.MappingConfiguration))]
              internal static class Configuration
              {
                  public static int StringToIntTypeConverter(string source, object[]? parameters) => int.Parse(source);

                  public static void MappingConfiguration(MappingConfiguration<SourceClass, TargetClass> config)
                  {
                      config.ProjectTo = ProjectionType.None;
                      config.ForProperty(x => x.Prop1).{{converter}};
                  }
              }
              """);

        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<string>("Prop1").WithProperty<string>("Prop2");
        sourceFile.AddClass(Accessibility.Public, "TargetClass").WithProperty<int>("Prop1").WithProperty<string>("Prop2");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain(
            """
            public static class SourceClassMapToExtensions
            {
                [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("sourceClass")]
                public static TargetClass? MapToTargetClass(this SourceClass? sourceClass)
                {
                    if (sourceClass is null)
                    {
                        return null;
                    }

                    return new TargetClass
                    {
                        Prop1 = Generated_Prop1ToInt32Converter(sourceClass.Prop1, new object[] { 1 }),
                        Prop2 = sourceClass.Prop2
                    };
                }

                private static int Generated_Prop1ToInt32Converter(string x, object[] p) => int.Parse(x);
            }
            """);
    }

    [Fact]
    public void When_PropertyTypeConverterIsLambdaWithBody_Should_GenerateStaticMethodWithBody()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(LanguageVersion.Latest));
        var sourceFile = builder.AddFile();
        builder.AddFile(supportNullableReferenceTypes: true).WithBody(
            """
            [Map<SourceClass, TargetClass>(nameof(Configuration.MappingConfiguration))]
            internal static class Configuration
            {
                public static int StringToIntTypeConverter(string source, object[]? parameters) => int.Parse(source);

                public static void MappingConfiguration(MappingConfiguration<SourceClass, TargetClass> config)
                {
                    config.ProjectTo = ProjectionType.None;
                    config.ForProperty(x => x.Prop1).UseTypeConverter<string>(x =>
                    {
                        var y = int.Parse(x);
                        return y;
                    });
                }
            }
            """);

        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<string>("Prop1").WithProperty<string>("Prop2");
        sourceFile.AddClass(Accessibility.Public, "TargetClass").WithProperty<int>("Prop1").WithProperty<string>("Prop2");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain(
            """
            public static class SourceClassMapToExtensions
            {
                [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("sourceClass")]
                public static TargetClass? MapToTargetClass(this SourceClass? sourceClass)
                {
                    if (sourceClass is null)
                    {
                        return null;
                    }

                    return new TargetClass
                    {
                        Prop1 = Generated_Prop1ToInt32Converter(sourceClass.Prop1),
                        Prop2 = sourceClass.Prop2
                    };
                }

                private static int Generated_Prop1ToInt32Converter(string x)
                {
                    var y = int.Parse(x);
                    return y;
                }
            }
            """);
    }

    [Fact]
    public void When_PropertyTypeConverterIsLambdaWithBodyAndParameter_Should_GenerateStaticMethodWithBodyAndParameter()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(LanguageVersion.Latest));
        var sourceFile = builder.AddFile();
        builder.AddFile(supportNullableReferenceTypes: true).WithBody(
            """
            [Map<SourceClass, TargetClass>(nameof(Configuration.MappingConfiguration))]
            internal static class Configuration
            {
                public static int StringToIntTypeConverter(string source, object[]? parameters) => int.Parse(source);

                public static void MappingConfiguration(MappingConfiguration<SourceClass, TargetClass> config)
                {
                    config.ProjectTo = ProjectionType.None;
                    config.ForProperty(x => x.Prop1).UseTypeConverter<string>(
                        (x, p) =>
                        {
                            var y = int.Parse(x);
                            return y;
                        },
                        new object[] { 1 });
                }
            }
            """);

        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<string>("Prop1").WithProperty<string>("Prop2");
        sourceFile.AddClass(Accessibility.Public, "TargetClass").WithProperty<int>("Prop1").WithProperty<string>("Prop2");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain(
            """
            public static class SourceClassMapToExtensions
            {
                [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("sourceClass")]
                public static TargetClass? MapToTargetClass(this SourceClass? sourceClass)
                {
                    if (sourceClass is null)
                    {
                        return null;
                    }

                    return new TargetClass
                    {
                        Prop1 = Generated_Prop1ToInt32Converter(sourceClass.Prop1, new object[] { 1 }),
                        Prop2 = sourceClass.Prop2
                    };
                }

                private static int Generated_Prop1ToInt32Converter(string x, object[] p)
                {
                    var y = int.Parse(x);
                    return y;
                }
            }
            """);
    }

    [Fact]
    public void When_PropertyIsIgnoredInMappingConfiguration_Should_NotMapTheIgnoredProperty()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(LanguageVersion.Latest));
        var sourceFile = builder.AddFile();
        builder.AddFile(supportNullableReferenceTypes: true).WithBody(
            """
            [Map<SourceClass, TargetClass>(nameof(Configuration.MappingConfiguration))]
            internal static class Configuration
            {
                public static void MappingConfiguration(MappingConfiguration<SourceClass, TargetClass> config)
                {
                    config.ProjectTo = ProjectionType.None;
                    config.ForProperty(x => x.Prop1).Ignore();
                }
            }
            """);

        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<string>("Prop1").WithProperty<string>("Prop2");
        sourceFile.AddClass(Accessibility.Public, "TargetClass").WithProperty<int>("Prop1").WithProperty<string>("Prop2");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain(
            """
            public static class SourceClassMapToExtensions
            {
                [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("sourceClass")]
                public static TargetClass? MapToTargetClass(this SourceClass? sourceClass)
                {
                    if (sourceClass is null)
                    {
                        return null;
                    }

                    return new TargetClass
                    {
                        Prop2 = sourceClass.Prop2
                    };
                }
            }
            """);
    }

    [Fact]
    public void When_PropertyIsMappedToAnotherPropertyInMappingConfiguration_Should_MapPropertiesCorrectly()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(LanguageVersion.Latest));
        var sourceFile = builder.AddFile();
        builder.AddEmptyFile().WithBody(
            """
            [assembly: MapTo.Map<MapTo.Tests.SourceClass, MapTo.Tests.TargetClass>(nameof(MapTo.Tests.Configuration.MappingConfiguration))]
            """);

        builder.AddFile(supportNullableReferenceTypes: true).WithBody(
            """
            internal static class Configuration
            {
                public static void MappingConfiguration(MappingConfiguration<SourceClass, TargetClass> config)
                {
                    config.ProjectTo = ProjectionType.None;
                    config.ForProperty(p => p.Prop1).MapTo(p => p.Prop2);
                    config.ForProperty(p => p.Prop2).MapTo(p => p.Prop1);
                }
            }
            """);

        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<string>("Prop1").WithProperty<int>("Prop2");
        sourceFile.AddClass(Accessibility.Public, "TargetClass").WithProperty<int>("Prop1").WithProperty<string>("Prop2");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain(
            """
            public static class SourceClassMapToExtensions
            {
                [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("sourceClass")]
                public static TargetClass? MapToTargetClass(this SourceClass? sourceClass)
                {
                    if (sourceClass is null)
                    {
                        return null;
                    }

                    return new TargetClass
                    {
                        Prop1 = sourceClass.Prop2,
                        Prop2 = sourceClass.Prop1
                    };
                }
            }
            """);
    }

    [Fact]
    public void When_PropertyIsMappedToAnotherPropertyWithNullHandlingInMappingConfiguration_Should_MapPropertiesCorrectly()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(LanguageVersion.Latest));
        var sourceFile = builder.AddFile();
        builder.AddFile(supportNullableReferenceTypes: true).WithBody(
            """
            [Map<SourceClass, TargetClass>(nameof(Configuration.MappingConfiguration))]
            internal static class Configuration
            {
                public static int StringToIntTypeConverter(string source, object[]? parameters) => int.Parse(source);

                public static void MappingConfiguration(MappingConfiguration<SourceClass, TargetClass> config)
                {
                    config.ProjectTo = ProjectionType.None;
                    config.ForProperty(p => p.Prop1).MapTo(p => p.Prop2, NullHandling.SetNull);
                    config.ForProperty(p => p.Prop2).MapTo(p => p.Prop1, NullHandling.ThrowException);
                }
            }
            """);

        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<string>("Prop1").WithProperty<string>("Prop2");
        sourceFile.AddClass(Accessibility.Public, "TargetClass").WithProperty<string>("Prop1").WithProperty<string>("Prop2");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain(
            """
            public static class SourceClassMapToExtensions
            {
                [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("sourceClass")]
                public static TargetClass? MapToTargetClass(this SourceClass? sourceClass)
                {
                    if (sourceClass is null)
                    {
                        return null;
                    }

                    return new TargetClass
                    {
                        Prop1 = sourceClass.Prop2,
                        Prop2 = sourceClass.Prop1 ?? throw new global::System.ArgumentNullException(nameof(sourceClass.Prop1))
                    };
                }
            }
            """);
    }

    [Fact]
    public void When_MappingConfigurationIsUsedWithMethodChaining_Should_MapAndUseTypeConverter()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(LanguageVersion.Latest));
        var sourceFile = builder.AddFile();
        builder.AddFile(supportNullableReferenceTypes: true).WithBody(
            """
            [Map<SourceClass, TargetClass>(nameof(Configuration.MappingConfiguration))]
            internal static class Configuration
            {
                public static int StringToIntTypeConverter(string source) => int.Parse(source);

                public static void MappingConfiguration(MappingConfiguration<SourceClass, TargetClass> config)
                {
                    config.ForProperty(x => x.Prop1).MapTo(p => p.Prop2).UseTypeConverter<string>(StringToIntTypeConverter);
                    config.ForProperty(x => x.Prop2).UseTypeConverter<string>(StringToIntTypeConverter).MapTo(p => p.Prop1);
                    config.ForProperty(x => x.Prop3).MapTo(p => p.SourceProp3);
                    config.ForProperty(x => x.Prop4).MapTo(p => p.Prop4).UseTypeConverter<string>(StringToIntTypeConverter).Ignore();
                }
            }
            """);

        sourceFile.AddClass(Accessibility.Public, "SourceClass")
            .WithProperty<string>("Prop1")
            .WithProperty<string>("Prop2")
            .WithProperty<int>("SourceProp3")
            .WithProperty<string>("Prop4");

        sourceFile.AddClass(Accessibility.Public, "TargetClass")
            .WithProperty<int>("Prop1")
            .WithProperty<int>("Prop2")
            .WithProperty<int>("Prop3")
            .WithProperty<int>("Prop4");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain(
            """
            public static TargetClass? MapToTargetClass(this SourceClass? sourceClass)
            {
                if (sourceClass is null)
                {
                    return null;
                }

                return new TargetClass
                {
                    Prop1 = global::MapTo.Tests.Configuration.StringToIntTypeConverter(sourceClass.Prop2),
                    Prop2 = global::MapTo.Tests.Configuration.StringToIntTypeConverter(sourceClass.Prop1),
                    Prop3 = sourceClass.SourceProp3
                };
            }
            """);
    }
}