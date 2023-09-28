using MapTo.CodeAnalysis;

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

    [Theory]
    [InlineData(LanguageVersion.CSharp7_3, false)]
    [InlineData(LanguageVersion.CSharp7_3, true)]
    [InlineData(LanguageVersion.CSharp10, false)]
    [InlineData(LanguageVersion.CSharp10, true)]
    public void Should_WriteNullableReferenceSyntaxIfEnabled(LanguageVersion version, bool supportNullReferenceTypes)
    {
        // Arrange
        var options = TestSourceBuilderOptions.Create(version, supportNullReferenceTypes: supportNullReferenceTypes);
        var builder = ScenarioBuilder.SimpleMappedClassInSameNamespaceAsSource(options);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var targetClass = compilation.GetClassDeclaration("SourceClassMapToExtensions");

        targetClass.ShouldContain(version > LanguageVersion.CSharp7_3 && supportNullReferenceTypes
            ? "public static TargetClass? MapToTargetClass(this SourceClass? sourceClass)"
            : "public static TargetClass MapToTargetClass([global::System.Diagnostics.CodeAnalysis.AllowNull] this SourceClass sourceClass)");
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
        compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldBe(
            $$"""
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
        diagnostics.ShouldBeSuccessful();
        compilation.GetGeneratedFileSyntaxTree("MapTo.Tests.TargetClass.g.cs")
            .GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("Prop2 = MapTo.Tests.NestedSourceClassMapToExtensions.MapToNestedTargetClass(sourceClass.Prop2)");
    }

    [Fact]
    public void When_BeforeMapMethodNotFound_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "InvalidMethodName")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        diagnostics.ShouldNotBeSuccessful(DiagnosticsFactory.BeforeOrAfterMapMethodNotFoundError(mapFromAttribute, nameof(MapFromAttribute.BeforeMap)));
    }

    [Theory]
    [InlineData("Protected")]
    [InlineData("Private")]
    public void When_BeforeMapMethodFoundButIsInaccessible_Should_ReportDiagnostics(string beforeMapAccessModifier)
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomBeforeMapMethod", string.Empty, Enum.Parse<AccessModifier>(beforeMapAccessModifier));

        // Act
        var (compilation, _) = builder.Compile(assertOutputCompilation: false);

        // Assert
        compilation.GetDiagnostics().ShouldNotBeSuccessful("CS0122", "'TargetClass.CustomBeforeMapMethod()' is inaccessible due to its protection level");
    }

    [Theory]
    [InlineData("ThirdParty.Utilities.HelperClass.CustomBeforeMapMethod")]
    [InlineData("nameof(ThirdParty.Utilities.HelperClass.CustomBeforeMapMethod)")]
    public void When_BeforeMapMethodFoundInAnotherClass_Should_CallBeforeMapping(string beforeMapMethod)
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: $"""[MapFrom(typeof(SourceClass), BeforeMap = "{beforeMapMethod}")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name");

        var externalFile = builder.AddFile("Helpers", ns: "ThirdParty.Utilities");
        externalFile.AddClass(AccessModifier.Public, "HelperClass")
            .WithStaticVoidMethod("CustomBeforeMapMethod", string.Empty);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("ThirdParty.Utilities.HelperClass.CustomBeforeMapMethod();");
    }

    [Fact]
    public void When_BeforeMapMethodFoundButHasMoreThanOneParameters_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomBeforeMapMethod", string.Empty, parameters: new[] { "SourceClass sourceClass", "string additionalParameter" });

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        compilation.Dump(_output);
        var sourceTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.SourceClass").ShouldNotBeNull();

        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        diagnostics.ShouldNotBeSuccessful(DiagnosticsFactory.BeforeOrAfterMapMethodInvalidParameterError(mapFromAttribute, nameof(MapFromAttribute.BeforeMap), sourceTypeSymbol));
    }

    [Fact]
    public void When_BeforeMapMethodFoundButHasIncorrectTypeParameter_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomBeforeMapMethod", string.Empty, parameters: new[] { "TargetClass sourceClass" });

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        compilation.Dump(_output);
        var sourceTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.SourceClass").ShouldNotBeNull();

        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        diagnostics.ShouldNotBeSuccessful(DiagnosticsFactory.BeforeOrAfterMapMethodInvalidParameterError(mapFromAttribute, nameof(MapFromAttribute.BeforeMap), sourceTypeSymbol));
    }

    [Theory]
    [InlineData("Public")]
    [InlineData("Internal")]
    public void When_BeforeMapMethodFoundHasNoParameterAndIsVoid_Should_CallBeforeMapping(string beforeMapAccessModifier)
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomBeforeMapMethod", string.Empty, Enum.Parse<AccessModifier>(beforeMapAccessModifier));

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("MapTo.Tests.TargetClass.CustomBeforeMapMethod();");
    }

    [Fact]
    public void When_BeforeMapMethodFoundHasParameterAndIsVoid_Should_CallBeforeMapping()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: false));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomBeforeMapMethod", string.Empty, parameters: new[] { "SourceClass source" });

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        compilation.Dump(_output);
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("MapTo.Tests.TargetClass.CustomBeforeMapMethod(sourceClass);");
    }

    [Fact]
    public void When_BeforeMapMethodFoundButHasIncorrectReturnType_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: false));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticMethod("TargetClass", "CustomBeforeMapMethod", string.Empty, parameters: new[] { "SourceClass sourceClass" });

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        compilation.Dump(_output);
        var sourceTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.SourceClass").ShouldNotBeNull();

        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        diagnostics.ShouldNotBeSuccessful(DiagnosticsFactory.BeforeOrAfterMapMethodInvalidReturnTypeError(mapFromAttribute, nameof(MapFromAttribute.BeforeMap), sourceTypeSymbol));
    }

    [Fact]
    public void When_BeforeMapMethodFoundHasParameterAndIsNotVoid_Should_CallBeforeMapping()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: false));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(body: "public class SourceSubClass : SourceClass { }");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticMethod("SourceSubClass", "CustomBeforeMapMethod", "throw new System.NotImplementedException();", parameters: new[] { "SourceClass source" });

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("sourceClass = MapTo.Tests.TargetClass.CustomBeforeMapMethod(sourceClass);");
    }

    [Fact]
    public void When_BeforeMapMethodFoundHasNoParametersButHasReturnType_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(body: "public class SourceSubClass : SourceClass { }");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticMethod("SourceSubClass", "CustomBeforeMapMethod", "throw new System.NotImplementedException();", parameters: null);

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        var sourceTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.SourceClass").ShouldNotBeNull();

        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        diagnostics.ShouldNotBeSuccessful(DiagnosticsFactory.BeforeOrAfterMapMethodMissingParameterError(mapFromAttribute, nameof(MapFromAttribute.BeforeMap), sourceTypeSymbol));
    }

    [Fact]
    public void When_BeforeMapMethodFoundHasParameterOfParentType_Should_CallBeforeMapping()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: false));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(body: "public class SourceSubClass : SourceClass { }");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceSubClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticMethod("SourceSubClass", "CustomBeforeMapMethod", "throw new System.NotImplementedException();", parameters: new[] { "SourceClass source" });

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceSubClassMapToExtensions")
            .ShouldContain("sourceSubClass = MapTo.Tests.TargetClass.CustomBeforeMapMethod(sourceSubClass);");
    }

    [Fact]
    public void When_BeforeMapMethodFoundWithoutNullableAnnotatedParameterAndNullableAnnotationIsEnabled_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: true));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomBeforeMapMethod", "throw new System.NotImplementedException();", parameters: new[] { "SourceClass sourceClass" });

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        diagnostics.ShouldNotBeSuccessful(
            DiagnosticsFactory.BeforeOrAfterMapMethodMissingParameterNullabilityAnnotationError(mapFromAttribute, nameof(MapFromAttribute.BeforeMap)));
    }

    [Fact]
    public void When_BeforeMapMethodFoundWithNullableAnnotatedParameterAndNullableAnnotationIsEnabled_Should_CallBeforeMapping()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: true));
        var sourceFile = builder.AddFile(supportNullableReferenceTypes: true);
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name", defaultValue: "string.Empty");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name", defaultValue: "string.Empty")
            .WithStaticVoidMethod("CustomBeforeMapMethod", "throw new System.NotImplementedException();", parameters: new[] { "SourceClass? sourceClass" });

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("MapTo.Tests.TargetClass.CustomBeforeMapMethod(sourceClass);");
    }

    [Fact]
    public void When_BeforeMapMethodFoundWithoutNullableAnnotatedReturnTypeAndNullableAnnotationIsEnabled_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: true));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticMethod("SourceClass", "CustomBeforeMapMethod", "throw new System.NotImplementedException();", parameters: new[] { "SourceClass? sourceClass" });

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        diagnostics.ShouldNotBeSuccessful(
            DiagnosticsFactory.BeforeOrAfterMapMethodMissingReturnTypeNullabilityAnnotationError(mapFromAttribute, nameof(MapFromAttribute.BeforeMap)));
    }

    [Fact]
    public void When_BeforeMapMethodFoundWithNullableAnnotatedReturnTypeAndNullableAnnotationIsEnabled_Should_CallBeforeMapping()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: true));
        var sourceFile = builder.AddFile(supportNullableReferenceTypes: true);
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name", defaultValue: "string.Empty");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name", defaultValue: "string.Empty")
            .WithStaticMethod("SourceClass?", "CustomBeforeMapMethod", "throw new System.NotImplementedException();", parameters: new[] { "SourceClass? sourceClass" });

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("sourceClass = MapTo.Tests.TargetClass.CustomBeforeMapMethod(sourceClass);");
    }

    [Fact]
    public void When_AfterMapMethodNotFound_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "InvalidMethodName")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        diagnostics.ShouldNotBeSuccessful(DiagnosticsFactory.BeforeOrAfterMapMethodNotFoundError(mapFromAttribute, nameof(MapFromAttribute.AfterMap)));
    }

    [Theory]
    [InlineData("Protected")]
    [InlineData("Private")]
    public void When_AfterMapMethodFoundButIsInaccessible_Should_ReportDiagnostics(string beforeMapAccessModifier)
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomAfterMapMethod", string.Empty, Enum.Parse<AccessModifier>(beforeMapAccessModifier));

        // Act
        var (compilation, _) = builder.Compile(assertOutputCompilation: false);

        // Assert
        compilation.Dump(_output);
        compilation.GetDiagnostics().ShouldNotBeSuccessful("CS0122", "'TargetClass.CustomAfterMapMethod()' is inaccessible due to its protection level");
    }

    [Theory]
    [InlineData("ThirdParty.Utilities.HelperClass.CustomAfterMapMethod")]
    [InlineData("nameof(ThirdParty.Utilities.HelperClass.CustomAfterMapMethod)")]
    public void When_AfterMapMethodFoundInAnotherClass_Should_CallBeforeMapping(string beforeMapMethod)
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: $"""[MapFrom(typeof(SourceClass), AfterMap = "{beforeMapMethod}")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name");

        var externalFile = builder.AddFile("Helpers", ns: "ThirdParty.Utilities");
        externalFile.AddClass(AccessModifier.Public, "HelperClass")
            .WithStaticVoidMethod("CustomAfterMapMethod", string.Empty);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("ThirdParty.Utilities.HelperClass.CustomAfterMapMethod();");
    }

    [Fact]
    public void When_AfterMapMethodFoundButHasMoreThanOneParameters_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomAfterMapMethod", string.Empty, parameters: new[] { "TargetClass target", "string additionalParameter" });

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        compilation.Dump(_output);
        var targetTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetClass").ShouldNotBeNull();

        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        diagnostics.ShouldNotBeSuccessful(DiagnosticsFactory.BeforeOrAfterMapMethodInvalidParameterError(mapFromAttribute, nameof(MapFromAttribute.AfterMap), targetTypeSymbol));
    }

    [Fact]
    public void When_AfterMapMethodFoundButHasIncorrectTypeParameter_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: false));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomAfterMapMethod", string.Empty, parameters: new[] { "SourceClass sourceClass" });

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        compilation.Dump(_output);
        var targetTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetClass").ShouldNotBeNull();

        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        diagnostics.ShouldNotBeSuccessful(DiagnosticsFactory.BeforeOrAfterMapMethodInvalidParameterError(mapFromAttribute, nameof(MapFromAttribute.AfterMap), targetTypeSymbol));
    }

    [Theory]
    [InlineData("Public")]
    [InlineData("Internal")]
    public void When_AfterMapMethodFoundHasNoParameterAndIsVoid_Should_CallBeforeMapping(string beforeMapAccessModifier)
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomAfterMapMethod", string.Empty, Enum.Parse<AccessModifier>(beforeMapAccessModifier));

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("MapTo.Tests.TargetClass.CustomAfterMapMethod();");
    }

    [Fact]
    public void When_AfterMapMethodFoundHasParameterAndIsVoid_Should_CallBeforeMapping()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: false));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomAfterMapMethod", string.Empty, parameters: new[] { "TargetClass target" });

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("MapTo.Tests.TargetClass.CustomAfterMapMethod(target);");
    }

    [Fact]
    public void When_AfterMapMethodFoundHasParameterAndIsNotVoid_Should_CallBeforeMapping()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: false));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(body: "public class SourceSubClass : SourceClass { }");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticMethod("SourceSubClass", "CustomAfterMapMethod", "throw new System.NotImplementedException();", parameters: new[] { "TargetClass target" });

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("MapTo.Tests.TargetClass.CustomAfterMapMethod(target);");
    }

    [Fact]
    public void When_AfterMapMethodFoundWithoutNullableAnnotatedParameterAndNullableAnnotationIsEnabled_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: true));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomAfterMapMethod", "throw new System.NotImplementedException();", parameters: new[] { "TargetClass target" });

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        diagnostics.ShouldNotBeSuccessful(
            DiagnosticsFactory.BeforeOrAfterMapMethodMissingParameterNullabilityAnnotationError(mapFromAttribute, nameof(MapFromAttribute.AfterMap)));
    }

    [Fact]
    public void When_AfterMapMethodFoundWithNullableAnnotatedParameterAndNullableAnnotationIsEnabled_Should_CallBeforeMapping()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: true));
        var sourceFile = builder.AddFile(supportNullableReferenceTypes: true);
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name", defaultValue: "string.Empty");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name", defaultValue: "string.Empty")
            .WithStaticVoidMethod("CustomAfterMapMethod", "throw new System.NotImplementedException();", parameters: new[] { "TargetClass? target" });

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("MapTo.Tests.TargetClass.CustomAfterMapMethod(target);");
    }
}