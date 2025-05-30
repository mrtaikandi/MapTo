﻿using MapTo.CodeAnalysis;
using MapTo.Diagnostics;
using MapTo.Mappings;

namespace MapTo.Tests;

public class MapFromTests
{
    private readonly ITestOutputHelper _output;

    public MapFromTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp7_3, false, false)]
    [InlineData(LanguageVersion.CSharp7_3, true, false)]
    [InlineData(LanguageVersion.CSharp10, false, false)]
    [InlineData(LanguageVersion.CSharp10, true, false)]
    [InlineData(LanguageVersion.CSharp11, false, true)]
    [InlineData(LanguageVersion.CSharp11, true, true)]
    public void Should_AlwaysAnnotateTheReturnTypeWithNotNullIfNotNull(LanguageVersion version, bool supportNullReferenceTypes, bool genericMapFromAttribute)
    {
        // Arrange
        var options = TestSourceBuilderOptions.Create(version, supportNullReferenceTypes: supportNullReferenceTypes);
        var builder = ScenarioBuilder.SimpleMappedClassInSameNamespaceAsSource(options, genericMapFromAttribute);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        compilation.Dump(_output);
        diagnostics.ShouldBeSuccessful();
        compilation
            .GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
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
        var targetClass = compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions");

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
        var builder = ScenarioBuilder.SimpleMappedClassInDifferentNamespaceAsSource(TestSourceBuilderOptions.Create());

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
            .ShouldBe($$"""
                        {{ScenarioBuilder.GeneratedCodeAttribute}}
                        public static partial class SourceClassToTargetClassMapToExtensions
                        {
                            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("sourceClass")]
                            public static global::ExternalLibMap.TargetClass? MapToTargetClass(this global::ExternalLib.SourceClass? sourceClass)
                            {
                                if (sourceClass is null)
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
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(analyzerConfigOptions: new Dictionary<string, string>
        {
            [nameof(CodeGeneratorOptions.ProjectionType)] = ProjectionType.None.ToString()
        }));

        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int?>("Id", propertyType: PropertyType.ReadOnly | PropertyType.AutoProperty)
            .WithProperty<string>("Name");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
            .ShouldBe($$"""
                        {{ScenarioBuilder.GeneratedCodeAttribute}}
                        public static partial class SourceClassToTargetClassMapToExtensions
                        {
                            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("sourceClass")]
                            public static TargetClass? MapToTargetClass(this SourceClass? sourceClass)
                            {
                                if (sourceClass is null)
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
        compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions").ShouldContain("public static TargetClass? ToTargetClass(this SourceClass? sourceClass)");
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
            .GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
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
            .GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
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
            .GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
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
        compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions").ShouldBe(
            $$"""
              {{ScenarioBuilder.GeneratedCodeAttribute}}
              public static partial class SourceClassToTargetClassMapToExtensions
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
                          Id = sourceClass.Id,
                          Name = sourceClass.Name
                      };
                  }
              }
              """);
    }

    [Fact]
    public void When_SourceAndTargetNamespacesAreGlobal_Should_UseApplicationName()
    {
        // Arrange
        var options = TestSourceBuilderOptions.Create(analyzerConfigOptions: new Dictionary<string, string>
        {
            [nameof(CodeGeneratorOptions.ProjectionType)] = ProjectionType.None.ToString()
        });

        var builder = new TestSourceBuilder(options);
        var sourceFile = builder.AddFile(ns: string.Empty, usings: new[] { "MapTo" });
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", attributes: "[MapFrom(typeof(SourceClass))]").WithProperty<int>("Id").WithProperty<string>("Name");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        compilation.Dump(_output);
        diagnostics.ShouldBeSuccessful();
        var generatedFile = compilation.GetGeneratedFileSyntaxTree("TargetClass.g.cs").ShouldNotBeNull();
        generatedFile.GetRoot().DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>().SingleOrDefault()?.Name.ToString().ShouldBe("MapTo.Tests.Dynamic");
    }

    [Fact]
    public void When_TargetWithReadOnlyPropertyAndNoConstructor_Should_CreateConstructorForReadOnlyProperties()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: "[MapFrom(typeof(SourceClass))]")
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
        nestedSourceFile.AddClass(Accessibility.Public, "NestedSourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        nestedSourceFile.AddClass(Accessibility.Public, "NestedTargetClass", partial: true, attributes: "[MapFrom(typeof(NestedSourceClass))]")
            .WithProperty<int>("Id", propertyType: PropertyType.ReadOnly | PropertyType.AutoProperty)
            .WithProperty<string>("Name");

        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Prop1").WithProperty("NestedSourceClass", "Prop2");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", partial: true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Prop1")
            .WithProperty("NestedTargetClass", "Prop2");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetGeneratedFileSyntaxTree("MapTo.Tests.TargetClass.g.cs")
            .GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
            .ShouldContain("Prop2 = global::MapTo.Tests.NestedSourceClassToNestedTargetClassMapToExtensions.MapToNestedTargetClass(sourceClass.Prop2)");
    }

    [Fact]
    public void When_BeforeMapMethodNotFound_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "InvalidMethodName")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        compilation.Dump(_output);
        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        var expectedError = DiagnosticsFactory.BeforeMapMethodNotFoundError(
            mapFromAttribute.ToMappingConfiguration());

        diagnostics.ShouldNotBeSuccessful(expectedError);
    }

    [Theory]
    [InlineData("Protected")]
    [InlineData("Private")]
    public void When_BeforeMapMethodFoundButIsInaccessible_Should_ReportDiagnostics(string beforeMapAccessibility)
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomBeforeMapMethod", string.Empty, Enum.Parse<Accessibility>(beforeMapAccessibility));

        // Act
        var (compilation, _) = builder.Compile(assertOutputCompilation: false);

        // Assert
        compilation.GetDiagnostics().ShouldNotBeSuccessful("CS0122", "'TargetClass.CustomBeforeMapMethod()' is inaccessible due to its protection level");
    }

    [Theory]
    [InlineData("nameof(HelperClass.CustomBeforeMapMethod)")]
    [InlineData("\"ThirdParty.Utilities.HelperClass.CustomBeforeMapMethod\"")]
    [InlineData("nameof(ThirdParty.Utilities.HelperClass.CustomBeforeMapMethod)")]
    public void When_BeforeMapMethodFoundInAnotherClass_Should_CallBeforeMapping(string beforeMapMethod)
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile(usings: new[] { "ThirdParty.Utilities" });
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: $"""[MapFrom(typeof(SourceClass), BeforeMap = {beforeMapMethod})]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name");

        var externalFile = builder.AddFile("Helpers", ns: "ThirdParty.Utilities");
        externalFile.AddClass(Accessibility.Public, "HelperClass")
            .WithStaticVoidMethod("CustomBeforeMapMethod", string.Empty);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
            .ShouldContain("ThirdParty.Utilities.HelperClass.CustomBeforeMapMethod();");
    }

    [Fact]
    public void When_BeforeMapMethodFoundButHasMoreThanOneParameters_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomBeforeMapMethod", string.Empty, parameters: new[] { "SourceClass sourceClass", "string additionalParameter" });

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        var sourceTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.SourceClass").ShouldNotBeNull();
        var targetTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetClass").ShouldNotBeNull();

        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        var expectedError = DiagnosticsFactory.BeforeMapMethodInvalidParameterError(mapFromAttribute.ToMappingConfiguration(), sourceTypeSymbol);
        diagnostics.ShouldNotBeSuccessful(expectedError);
    }

    [Fact]
    public void When_BeforeMapMethodFoundButHasIncorrectTypeParameter_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomBeforeMapMethod", string.Empty, parameters: ["TargetClass sourceClass"]);

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        var sourceTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.SourceClass").ShouldNotBeNull();
        var targetTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetClass").ShouldNotBeNull();

        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        var expectedError = DiagnosticsFactory.BeforeMapMethodInvalidParameterError(mapFromAttribute.ToMappingConfiguration(), sourceTypeSymbol);
        diagnostics.ShouldNotBeSuccessful(expectedError);
    }

    [Theory]
    [InlineData("Public", "\"CustomBeforeMapMethod\"")]
    [InlineData("Public", "nameof(CustomBeforeMapMethod)")]
    [InlineData("Internal", "nameof(CustomBeforeMapMethod)")]
    public void When_BeforeMapMethodFoundHasNoParameterAndIsVoid_Should_CallBeforeMapping(string beforeMapAccessibility, string methodName)
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: $"[MapFrom(typeof(SourceClass), BeforeMap = {methodName})]")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomBeforeMapMethod", string.Empty, Enum.Parse<Accessibility>(beforeMapAccessibility));

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
            .ShouldContain("MapTo.Tests.TargetClass.CustomBeforeMapMethod();");
    }

    [Fact]
    public void When_BeforeMapMethodFoundHasParameterAndIsVoid_Should_CallBeforeMapping()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: false));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomBeforeMapMethod", string.Empty, parameters: new[] { "SourceClass source" });

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
            .ShouldContain("MapTo.Tests.TargetClass.CustomBeforeMapMethod(sourceClass);");
    }

    [Fact]
    public void When_BeforeMapMethodFoundButHasIncorrectReturnType_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: false));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticMethod("TargetClass", "CustomBeforeMapMethod", string.Empty, parameters: new[] { "SourceClass sourceClass" });

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        var sourceTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.SourceClass").ShouldNotBeNull();
        var targetTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetClass").ShouldNotBeNull();

        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        var expectedError = DiagnosticsFactory.BeforeMapMethodInvalidReturnTypeError(mapFromAttribute.ToMappingConfiguration(), sourceTypeSymbol);
        diagnostics.ShouldNotBeSuccessful(expectedError);
    }

    [Fact]
    public void When_BeforeMapMethodFoundHasParameterAndIsNotVoid_Should_CallBeforeMapping()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: false));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(body: "public class SourceSubClass : SourceClass { }");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticMethod("SourceSubClass", "CustomBeforeMapMethod", "throw new System.NotImplementedException();", parameters: new[] { "SourceClass source" });

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
            .ShouldContain("sourceClass = global::MapTo.Tests.TargetClass.CustomBeforeMapMethod(sourceClass);");
    }

    [Fact]
    public void When_BeforeMapMethodFoundHasNoParametersButHasReturnType_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(body: "public class SourceSubClass : SourceClass { }");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticMethod("SourceSubClass", "CustomBeforeMapMethod", "throw new System.NotImplementedException();", parameters: null);

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);
        var targetTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetClass").ShouldNotBeNull();

        // Assert
        var sourceTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.SourceClass").ShouldNotBeNull();

        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        var expectedError = DiagnosticsFactory.BeforeMapMethodMissingParameterError(mapFromAttribute.ToMappingConfiguration(), sourceTypeSymbol);
        diagnostics.ShouldNotBeSuccessful(expectedError);
    }

    [Fact]
    public void When_BeforeMapMethodFoundHasParameterOfParentType_Should_CallBeforeMapping()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: false));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(body: "public class SourceSubClass : SourceClass { }");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceSubClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticMethod("SourceSubClass", "CustomBeforeMapMethod", "throw new System.NotImplementedException();", parameters: new[] { "SourceClass source" });

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceSubClassToTargetClassMapToExtensions")
            .ShouldContain("sourceSubClass = global::MapTo.Tests.TargetClass.CustomBeforeMapMethod(sourceSubClass);");
    }

    [Fact]
    public void When_BeforeMapMethodFoundWithoutNullableAnnotatedParameterAndNullableAnnotationIsEnabled_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: true));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomBeforeMapMethod", "throw new System.NotImplementedException();", parameters: new[] { "SourceClass sourceClass" });

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        var targetTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetClass").ShouldNotBeNull();
        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        var expectedError = DiagnosticsFactory.BeforeMapMethodMissingParameterNullabilityAnnotationError(mapFromAttribute.ToMappingConfiguration());
        diagnostics.ShouldNotBeSuccessful(expectedError);
    }

    [Fact]
    public void When_BeforeMapMethodFoundWithNullableAnnotatedParameterAndNullableAnnotationIsEnabled_Should_CallBeforeMapping()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: true));
        var sourceFile = builder.AddFile(supportNullableReferenceTypes: true);
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name", defaultValue: "string.Empty");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name", defaultValue: "string.Empty")
            .WithStaticVoidMethod("CustomBeforeMapMethod", "throw new System.NotImplementedException();", parameters: new[] { "SourceClass? sourceClass" });

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
            .ShouldContain("MapTo.Tests.TargetClass.CustomBeforeMapMethod(sourceClass);");
    }

    [Fact]
    public void When_BeforeMapMethodFoundWithoutNullableAnnotatedReturnTypeAndNullableAnnotationIsEnabled_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: true));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticMethod("SourceClass", "CustomBeforeMapMethod", "throw new System.NotImplementedException();", parameters: new[] { "SourceClass? sourceClass" });

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var targetTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetClass").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        var expectedError = DiagnosticsFactory.BeforeMapMethodMissingReturnTypeNullabilityAnnotationError(mapFromAttribute.ToMappingConfiguration());
        diagnostics.ShouldNotBeSuccessful(expectedError);
    }

    [Fact]
    public void When_BeforeMapMethodFoundWithNullableAnnotatedReturnTypeAndNullableAnnotationIsEnabled_Should_CallBeforeMapping()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: true));
        var sourceFile = builder.AddFile(supportNullableReferenceTypes: true);
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name", defaultValue: "string.Empty");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), BeforeMap = "CustomBeforeMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name", defaultValue: "string.Empty")
            .WithStaticMethod("SourceClass?", "CustomBeforeMapMethod", "throw new System.NotImplementedException();", parameters: new[] { "SourceClass? sourceClass" });

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
            .ShouldContain("sourceClass = global::MapTo.Tests.TargetClass.CustomBeforeMapMethod(sourceClass);");
    }

    [Fact]
    public void When_AfterMapMethodNotFound_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "InvalidMethodName")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        var targetTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetClass").ShouldNotBeNull();
        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        var expectedError = DiagnosticsFactory.AfterMapMethodNotFoundError(mapFromAttribute.ToMappingConfiguration());
        diagnostics.ShouldNotBeSuccessful(expectedError);
    }

    [Theory]
    [InlineData("Protected")]
    [InlineData("Private")]
    public void When_AfterMapMethodFoundButIsInaccessible_Should_ReportDiagnostics(string beforeMapAccessibility)
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomAfterMapMethod", string.Empty, Enum.Parse<Accessibility>(beforeMapAccessibility));

        // Act
        var (compilation, _) = builder.Compile(assertOutputCompilation: false);

        // Assert
        compilation.GetDiagnostics().ShouldNotBeSuccessful("CS0122", "'TargetClass.CustomAfterMapMethod()' is inaccessible due to its protection level");
    }

    [Theory]
    [InlineData("\"ThirdParty.Utilities.HelperClass.CustomAfterMapMethod\"")]
    [InlineData("nameof(ThirdParty.Utilities.HelperClass.CustomAfterMapMethod)")]
    [InlineData("nameof(HelperClass.CustomAfterMapMethod)")]
    public void When_AfterMapMethodFoundInAnotherClass_Should_CallAfterMapping(string afterMapMethod)
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile(usings: new[] { "ThirdParty.Utilities" });
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: $"""[MapFrom(typeof(SourceClass), AfterMap = {afterMapMethod})]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name");

        var externalFile = builder.AddFile("Helpers", ns: "ThirdParty.Utilities");
        externalFile.AddClass(Accessibility.Public, "HelperClass")
            .WithStaticVoidMethod("CustomAfterMapMethod", string.Empty);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
            .ShouldContain("ThirdParty.Utilities.HelperClass.CustomAfterMapMethod();");
    }

    [Fact]
    public void When_AfterMapMethodFoundButHasMoreThanTwoParameters_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomAfterMapMethod", string.Empty, parameters: new[] { "TargetClass target", "string additionalParameter" });

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        var targetTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetClass").ShouldNotBeNull();
        var sourceTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.SourceClass").ShouldNotBeNull();

        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        var expectedError = DiagnosticsFactory.AfterMapMethodInvalidParametersError(
            mapFromAttribute.ToMappingConfiguration(), sourceTypeSymbol, targetTypeSymbol);

        diagnostics.ShouldNotBeSuccessful(expectedError);
    }

    [Fact]
    public void When_AfterMapMethodFoundButOnlyHasSourceTypeParameter_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: false));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomAfterMapMethod", string.Empty, parameters: new[] { "SourceClass sourceClass" });

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        var targetTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetClass").ShouldNotBeNull();
        var sourceTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.SourceClass").ShouldNotBeNull();

        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        var expectedError = DiagnosticsFactory.AfterMapMethodInvalidParametersError(
            mapFromAttribute.ToMappingConfiguration(), sourceTypeSymbol, targetTypeSymbol);

        diagnostics.ShouldNotBeSuccessful(expectedError);
    }

    [Fact]
    public void When_AfterMapMethodFoundWithOneCorrectAndOneIncorrectParameters_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: false));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomAfterMapMethod", string.Empty, parameters: ["SourceClass sourceClass", "string something"]);

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        var targetTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetClass").ShouldNotBeNull();
        var sourceTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.SourceClass").ShouldNotBeNull();

        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        var expectedError = DiagnosticsFactory.AfterMapMethodInvalidParametersError(
            mapFromAttribute.ToMappingConfiguration(), sourceTypeSymbol, targetTypeSymbol);

        diagnostics.ShouldNotBeSuccessful(expectedError);
    }

    [Fact]
    public void When_AfterMapMethodFoundWithOneIncorrectAndOneCorrectParameters_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: false));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomAfterMapMethod", string.Empty, parameters: new[] { "string something", "TargetClass target" });

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        var targetTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetClass").ShouldNotBeNull();
        var sourceTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.SourceClass").ShouldNotBeNull();

        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        var expectedError = DiagnosticsFactory.AfterMapMethodInvalidParametersError(
            mapFromAttribute.ToMappingConfiguration(), sourceTypeSymbol, targetTypeSymbol);

        diagnostics.ShouldNotBeSuccessful(expectedError);
    }

    [Theory]
    [InlineData("Public", "\"CustomAfterMapMethod\"")]
    [InlineData("Public", "nameof(CustomAfterMapMethod)")]
    [InlineData("Internal", "nameof(CustomAfterMapMethod)")]
    public void When_AfterMapMethodFoundHasNoParameterAndIsVoid_Should_CallAfterMapping(string afterMapAccessibility, string methodName)
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: $"[MapFrom(typeof(SourceClass), AfterMap = {methodName})]")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomAfterMapMethod", string.Empty, Enum.Parse<Accessibility>(afterMapAccessibility));

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
            .ShouldContain("MapTo.Tests.TargetClass.CustomAfterMapMethod();");
    }

    [Fact]
    public void When_AfterMapMethodFoundHasParameterAndIsVoid_Should_CallAfterMapping()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: false));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomAfterMapMethod", string.Empty, parameters: new[] { "TargetClass target" });

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
            .ShouldContain("MapTo.Tests.TargetClass.CustomAfterMapMethod(target);");
    }

    [Fact]
    public void When_AfterMapMethodFoundHasParameterAndIsNotVoid_Should_CallAfterMapping()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: false));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(body: "public class TargetSubClass : TargetClass { }");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticMethod("TargetSubClass", "CustomAfterMapMethod", "throw new System.NotImplementedException();", parameters: new[] { "TargetClass target" });

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
            .ShouldContain("target = global::MapTo.Tests.TargetClass.CustomAfterMapMethod(target);");
    }

    [Fact]
    public void When_AfterMapMethodFoundWithoutNullableAnnotatedParameterAndNullableAnnotationIsEnabled_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: true));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticVoidMethod("CustomAfterMapMethod", "throw new System.NotImplementedException();", parameters: new[] { "TargetClass target" });

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        var targetTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetClass").ShouldNotBeNull();
        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        var expectedError = DiagnosticsFactory.AfterMapMethodMissingParameterNullabilityAnnotationError(mapFromAttribute.ToMappingConfiguration());
        diagnostics.ShouldNotBeSuccessful(expectedError);
    }

    [Fact]
    public void When_AfterMapMethodFoundWithNullableAnnotatedParameterAndNullableAnnotationIsEnabled_Should_CallAfterMapping()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: true));
        var sourceFile = builder.AddFile(supportNullableReferenceTypes: true);
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name", defaultValue: "string.Empty");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name", defaultValue: "string.Empty")
            .WithStaticVoidMethod("CustomAfterMapMethod", "throw new System.NotImplementedException();", parameters: new[] { "TargetClass? target" });

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
            .ShouldContain("MapTo.Tests.TargetClass.CustomAfterMapMethod(target);");
    }

    [Fact]
    public void When_AfterMapMethodFoundWithTargetAndSourceParameter_Should_CallAfterMap()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: true));
        var sourceFile = builder.AddFile(supportNullableReferenceTypes: true);
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name", defaultValue: "string.Empty");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name", defaultValue: "string.Empty")
            .WithStaticVoidMethod("CustomAfterMapMethod", "throw new System.NotImplementedException();", parameters: new[] { "TargetClass? target", "SourceClass? source" });

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
            .ShouldNotBeNull()
            .ShouldContain("MapTo.Tests.TargetClass.CustomAfterMapMethod(target, sourceClass);");
    }

    [Fact]
    public void When_AfterMapMethodFoundWithSourceAndTargetParameter_Should_CallAfterMap()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: true));
        var sourceFile = builder.AddFile(supportNullableReferenceTypes: true);
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name", defaultValue: "string.Empty");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name", defaultValue: "string.Empty")
            .WithStaticVoidMethod("CustomAfterMapMethod", "throw new System.NotImplementedException();", parameters: new[] { "SourceClass? source", "TargetClass? target" });

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
            .ShouldNotBeNull()
            .ShouldContain("MapTo.Tests.TargetClass.CustomAfterMapMethod(sourceClass, target);");
    }

    [Fact]
    public void When_AfterMapMethodFoundButHasIncorrectReturnType_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: false));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticMethod("SourceClassClass", "CustomAfterMapMethod", string.Empty, parameters: new[] { "TargetClass target" });

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        var targetTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetClass").ShouldNotBeNull();

        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        var expectedError = DiagnosticsFactory.AfterMapMethodInvalidReturnTypeError(mapFromAttribute.ToMappingConfiguration(), targetTypeSymbol);
        diagnostics.ShouldNotBeSuccessful(expectedError);
    }

    [Fact]
    public void When_AfterMapMethodFoundWithCorrectReturnTypeButNotTargetClassParameter_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: false));
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: """[MapFrom(typeof(SourceClass), AfterMap = "CustomAfterMapMethod")]""")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithStaticMethod("TargetClass", "CustomAfterMapMethod", "throw new System.NotImplementedException();", parameters: Array.Empty<string>());

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        var targetTypeSymbol = compilation.GetTypeByMetadataName("MapTo.Tests.TargetClass").ShouldNotBeNull();

        var extensionClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var mapFromAttribute = compilation.GetSemanticModel(extensionClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(extensionClassDeclaration)
            .ShouldNotBeNull()
            .GetAttribute<MapFromAttribute>()
            .ShouldNotBeNull();

        var expectedError = DiagnosticsFactory
            .AfterMapMethodMissingParameterError(mapFromAttribute.ToMappingConfiguration(), targetTypeSymbol);

        diagnostics.ShouldNotBeSuccessful(expectedError);
    }

    [Fact]
    public void When_SourceRecordDoesNotHaveRequiredPropertyAndTargetRequiredPropertyIsInBaseClass_Should_RequestValueFromExtensionArgs()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile(supportNullableReferenceTypes: true)
            .WithBody(
                """
                public class Source
                {
                    public int Prop1 { get; init; }
                    public double Prop2 { get; init; }
                }

                public abstract class TargetBase
                {
                    public required double Prop3 { get; init; }
                }

                [MapFrom<Source>]
                public class Target : TargetBase
                {
                    public required int Prop1 { get; init; }
                }
                """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        compilation.Dump(_output);
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceToTargetMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain(
            """
            public static Target? MapToTarget(this Source? source, double prop3)
            {
                if (source is null)
                {
                    return null;
                }

                return new Target
                {
                    Prop1 = source.Prop1,
                    Prop3 = prop3
                };
            }
            """);
    }
}