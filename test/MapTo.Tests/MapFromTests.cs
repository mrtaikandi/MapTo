namespace MapTo.Tests;

public class MapFromTests
{
    private readonly ITestOutputHelper _output;

    public MapFromTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp7_3, false)]
    [InlineData(LanguageVersion.CSharp7_3, true)]
    [InlineData(LanguageVersion.CSharp10, false)]
    [InlineData(LanguageVersion.CSharp10, true)]
    public void Should_AlwaysAnnotateTheReturnTypeWithNotNullIfNotNull(LanguageVersion version, bool supportNullReferenceTypes)
    {
        // Arrange
        var options = TestSourceBuilderOptions.Create(version, supportNullReferenceTypes: supportNullReferenceTypes);
        var builder = ScenarioBuilder.SimpleMappedClassInSameNamespaceAsSource(options);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation
            .GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("""[return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("sourceClass")]""");
    }

    [Fact]
    public void When_FoundEmptyAnnotatedClass_Should_NotGenerate()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile("Source");
        sourceFile.AddEmptyClass("SourceClass");
        sourceFile.AddEmptyClass("TargetClass", attributes: "[MapFrom(typeof(SourceClass))]");

        // Act
        var (compilation, diagnostics) = builder.Compile(sourceFile);

        // Assert
        diagnostics.ShouldBeSuccessful();

        var generatedCode = compilation.GetGeneratedFileSyntaxTree("Source")?.ToString();
        generatedCode.ShouldNotBeNullOrWhiteSpace();
        generatedCode.ShouldNotContain("partial class TargetClass");
        generatedCode.ShouldNotContain("static class SourceClassMapToExtensions");
    }

    [Fact]
    public void When_FoundMappingSource_With_DifferentNamespaces_Should_UseCorrectNamespace()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleMappedClassInDifferentNamespaceAsSource();

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldBe($$"""
                        {{ScenarioBuilder.GeneratedCodeAttribute}}
                        public static class SourceClassMapToExtensions
                        {
                            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("sourceClass")]
                            public static global::ExternalLibMap.TargetClass? MapToTargetClass(this global::ExternalLib.SourceClass? sourceClass)
                            {
                                if (ReferenceEquals(sourceClass, null))
                                {
                                    return null;
                                }
                        
                                return new TargetClass
                                {
                                    Id = sourceClass.Id,
                                    Name = sourceClass.Name
                                };
                            }
                        }
                        """);
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp7)]
    [InlineData(LanguageVersion.CSharp10)]
    public void When_FoundMappingSource_With_GetSetPropertyNames_Should_UseObjectInitializer(LanguageVersion languageVersion) => ScenarioBuilder
        .SimpleMappedClassInSameNamespaceAsSource(languageVersion)
        .CompileAndAssertExpectedGeneratedContent();

    [Fact]
    public void When_FoundMappingSource_With_InitPropertyNames_Should_UseObjectInitializer() => ScenarioBuilder
        .SimpleMappedClassInSameNamespaceAsSourceWithInitProperty()
        .CompileAndAssertExpectedGeneratedContent();

    [Fact]
    public void When_FoundMappingSource_WithReadOnlyPropertyNames_Should_UseGenerateAndUseConstructorInitializer()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int?>("Id", propertyType: PropertyType.ReadOnly | PropertyType.AutoProperty)
            .WithProperty<string>("Name");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldBe($$"""
                        {{ScenarioBuilder.GeneratedCodeAttribute}}
                        public static class SourceClassMapToExtensions
                        {
                            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("sourceClass")]
                            public static TargetClass? MapToTargetClass(this SourceClass? sourceClass)
                            {
                                if (ReferenceEquals(sourceClass, null))
                                {
                                    return null;
                                }
                        
                                return new TargetClass(sourceClass.Id)
                                {
                                    Name = sourceClass.Name
                                };
                            }
                        }
                        """);
    }

    [Fact]
    public void When_MapMethodPrefixIsProvided_Should_PrefixTheExtensionMethod()
    {
        // Arrange
        var options = TestSourceBuilderOptions.Create();
        options.AddAnalyzerConfigOption(nameof(CodeGeneratorOptions.MapMethodPrefix), "To");

        var builder = ScenarioBuilder.SimpleMappedClassInSameNamespaceAsSource(options);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldContain("public static TargetClass? ToTargetClass(this SourceClass? sourceClass)");
    }

    [Fact]
    public void When_NullableContextIsDisabled_Should_NotEmmitNullable()
    {
        // Arrange
        var options = TestSourceBuilderOptions.Create(supportNullReferenceTypes: false);
        var builder = ScenarioBuilder.SimpleMappedClassInSameNamespaceAsSource(options);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation
            .GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("public static TargetClass MapToTargetClass([global::System.Diagnostics.CodeAnalysis.AllowNull] this SourceClass sourceClass)");
    }

    [Fact]
    public void When_NullableContextIsEnabled_Should_EmmitNullable()
    {
        // Arrange
        var options = TestSourceBuilderOptions.Create(supportNullReferenceTypes: true);
        var builder = ScenarioBuilder.SimpleMappedClassInSameNamespaceAsSource(options);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation
            .GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("public static TargetClass? MapToTargetClass(this SourceClass? sourceClass)");
    }

    [Fact]
    public void When_NullableStaticAnalysis_IsEnabled_Should_AnnotateWithNullableAttributes()
    {
        // Arrange
        var options = TestSourceBuilderOptions.Create(LanguageVersion.CSharp7_3, supportNullReferenceTypes: false);
        var builder = ScenarioBuilder.SimpleMappedClassInSameNamespaceAsSource(options);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation
            .GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("public static TargetClass MapToTargetClass([global::System.Diagnostics.CodeAnalysis.AllowNull] this SourceClass sourceClass)");
    }

    [Fact]
    public void When_SourceAndTargetNamespacesAreTheSame_Should_NotEmmitGlobalNamespace()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleMappedClassInSameNamespaceAsSource();

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldBe($$"""
                                                                                 {{ScenarioBuilder.GeneratedCodeAttribute}}
                                                                                 public static class SourceClassMapToExtensions
                                                                                 {
                                                                                     [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("sourceClass")]
                                                                                     public static TargetClass? MapToTargetClass(this SourceClass? sourceClass)
                                                                                     {
                                                                                         if (ReferenceEquals(sourceClass, null))
                                                                                         {
                                                                                             return null;
                                                                                         }
                                                                                 
                                                                                         return new TargetClass
                                                                                         {
                                                                                             Id = sourceClass.Id,
                                                                                             Name = sourceClass.Name
                                                                                         };
                                                                                     }
                                                                                 }
                                                                                 """);
    }

    [Fact]
    public void When_TargetWithReadOnlyPropertyAndConstructor_Should_NotCreateConstructorAndUseConstructorInitializer()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int?>("Id", propertyType: PropertyType.ReadOnly | PropertyType.AutoProperty)
            .WithProperty<string>("Name")
            .WithConstructor("""
                             public TargetClass(int id)
                             {
                                 Id = id;
                             }
                             """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("""
                           return new TargetClass(sourceClass.Id)
                           {
                               Name = sourceClass.Name
                           };
                           """);
    }

    [Fact]
    public void When_TargetWithReadOnlyPropertyAndNoConstructor_Should_CreateConstructorForReadOnlyProperties()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int?>("Id", propertyType: PropertyType.ReadOnly | PropertyType.AutoProperty)
            .WithProperty<string>("Name");

        // Act
        var (compilation, _) = builder.Compile(false);

        // Assert
        compilation.GetGeneratedFileSyntaxTree("TargetClass").GetClassDeclaration("TargetClass")?.DescendantNodes()
            .OfType<ConstructorDeclarationSyntax>().Single().ToString()
            .ShouldBe("""
                      public TargetClass(int? id)
                          {
                              Id = id;
                          }
                      """);
    }

    [Fact]
    public void When_TargetWithReadOnlyPropertyIsNotPartial_Should_ReportDiagnostic()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id", propertyType: PropertyType.ReadOnly | PropertyType.AutoProperty)
            .WithProperty<string>("Name");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        var targetClassDeclaration = compilation.GetClassDeclaration("TargetClass");
        targetClassDeclaration.ShouldNotBeNull();
        var expectedError = DiagnosticsFactory.MissingPartialKeywordOnTargetClassError(
            targetClassDeclaration.Identifier.GetLocation(),
            targetClassDeclaration.Identifier.ValueText);

        diagnostics.ShouldNotBeSuccessful(expectedError);
    }

    [Fact]
    public void When_MappedPropertyTypeIsAlsoMapped_Should_UseMapToExtensionOnNestedPropertyType()
    {
        // Arrange
        var builder = new TestSourceBuilder();

        var nestedSourceFile = builder.AddFile();
        nestedSourceFile.AddClass(AccessModifier.Public, "NestedSourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        nestedSourceFile.AddClass(AccessModifier.Public, "NestedTargetClass", partial: true, attributes: "[MapFrom(typeof(NestedSourceClass))]")
            .WithProperty<int>("Id", propertyType: PropertyType.ReadOnly | PropertyType.AutoProperty)
            .WithProperty<string>("Name");

        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Prop1").WithProperty("NestedSourceClass", "Prop2");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", partial: true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Prop1")
            .WithProperty("NestedTargetClass", "Prop2");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        compilation.Dump(_output);
        diagnostics.ShouldBeSuccessful();
        compilation.GetGeneratedFileSyntaxTree("MapTo.Tests.TargetClass.g.cs")
            .GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("Prop2 = MapTo.Tests.NestedSourceClassMapToExtensions.MapToNestedTargetClass(sourceClass.Prop2)");
    }
}