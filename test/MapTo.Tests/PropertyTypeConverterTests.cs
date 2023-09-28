using MapTo.Diagnostics;

namespace MapTo.Tests;

public class PropertyTypeConverterTests
{
    private readonly ITestOutputHelper _output;

    public PropertyTypeConverterTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void When_SourceAndTargetPropertyTypesAreCompatible_Should_NotReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name");

        // Act
        var (_, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
    }

    [Fact]
    public void When_SourceAndTargetPropertyTypesAreNotCompatible_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<string>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id", propertyType: PropertyType.ReadOnly | PropertyType.AutoProperty)
            .WithProperty<string>("Name");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        var targetClassDeclaration = compilation.GetClassDeclaration("TargetClass");
        targetClassDeclaration.ShouldNotBeNull();

        var propertySymbol = compilation
            .GetSemanticModel(targetClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(targetClassDeclaration.Members[0]);

        propertySymbol.ShouldNotBeNull();

        var expectedError = DiagnosticsFactory.PropertyTypeConverterRequiredError(propertySymbol);
        diagnostics.ShouldNotBeSuccessful(expectedError);
    }

    [Fact]
    public void When_SourceAndTargetPropertyTypesAreNotCompatibleButHasPropertyTypeConverter_Should_NotReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<string>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", partial: true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id", attribute: "[PropertyTypeConverter(\"StringToIntTypeConverter\")]")
            .WithProperty<string>("Name")
            .WithStaticMethod("int", "StringToIntTypeConverter", parameters: new[] { "string source" }, body: "return int.Parse(source);");

        // Act
        var (_, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
    }

    [Fact]
    public void When_PropertyTypeConverterMethodNotFound_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<string>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", partial: true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id", attribute: "[PropertyTypeConverter(\"RandomMethod\")]")
            .WithProperty<string>("Name");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        var targetClassDeclaration = compilation.GetClassDeclaration("TargetClass");
        targetClassDeclaration.ShouldNotBeNull();

        var propertySymbol = compilation
            .GetSemanticModel(targetClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(targetClassDeclaration.Members[0])
            .ShouldNotBeNull();

        var typeConverterAttribute = propertySymbol.GetAttributes()[0].ShouldNotBeNull();

        diagnostics.ShouldNotBeSuccessful(DiagnosticsFactory.PropertyTypeConverterMethodNotFoundInTargetClassError(propertySymbol, typeConverterAttribute));
    }

    [Fact]
    public void When_PropertyTypeConverterMethodIsNotStatic_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<string>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", partial: true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id", attribute: "[PropertyTypeConverter(\"StringToIntTypeConverter\")]")
            .WithProperty<string>("Name")
            .WithMethod("int", "StringToIntTypeConverter", parameters: new[] { "string source" }, body: "return int.Parse(source);");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        var targetClassDeclaration = compilation.GetClassDeclaration("TargetClass").ShouldNotBeNull();
        var methodSymbol = compilation
            .GetSemanticModel(targetClassDeclaration.SyntaxTree)
            .GetDeclaredSymbol(targetClassDeclaration.Members[2]) as IMethodSymbol;

        methodSymbol.ShouldNotBeNull();
        diagnostics.ShouldNotBeSuccessful(DiagnosticsFactory.PropertyTypeConverterMethodIsNotStaticError(methodSymbol));
    }

    [Fact]
    public void When_PropertyTypeConverterMethodDoesNotHaveCompatibleReturnType_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<string>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", partial: true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id", attribute: "[PropertyTypeConverter(\"StringToIntTypeConverter\")]")
            .WithProperty<string>("Name")
            .WithStaticMethod("double", "StringToIntTypeConverter", parameters: new[] { "string source" }, body: "return int.Parse(source);");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        var targetClassDeclaration = compilation.GetClassDeclaration("TargetClass").ShouldNotBeNull();
        var semanticModel = compilation.GetSemanticModel(targetClassDeclaration.SyntaxTree);

        var propertySymbol = (semanticModel.GetDeclaredSymbol(targetClassDeclaration.Members[0]) as IPropertySymbol).ShouldNotBeNull();
        var methodSymbol = semanticModel.GetDeclaredSymbol(targetClassDeclaration.Members[2]) as IMethodSymbol;

        methodSymbol.ShouldNotBeNull();
        diagnostics.ShouldNotBeSuccessful(DiagnosticsFactory.PropertyTypeConverterMethodReturnTypeCompatibilityError(propertySymbol, methodSymbol));
    }

    [Fact]
    public void When_PropertyTypeConverterMethodDoesNotHaveCorrectInputType_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<string>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", partial: true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id", attribute: "[PropertyTypeConverter(\"StringToIntTypeConverter\")]")
            .WithProperty<string>("Name")
            .WithStaticMethod("int", "StringToIntTypeConverter", parameters: new[] { "long source" }, body: "return int.Parse(source.ToString());");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        var sourceClassDeclaration = compilation.GetClassDeclaration("SourceClass").ShouldNotBeNull();
        var sourceSemanticModel = compilation.GetSemanticModel(sourceClassDeclaration.SyntaxTree);
        var sourcePropertySymbol = (sourceSemanticModel.GetDeclaredSymbol(sourceClassDeclaration.Members[0]) as IPropertySymbol).ShouldNotBeNull();

        var targetClassDeclaration = compilation.GetClassDeclaration("TargetClass").ShouldNotBeNull();
        var targetSemanticModel = compilation.GetSemanticModel(targetClassDeclaration.SyntaxTree);
        var targetMethodSymbol = (targetSemanticModel.GetDeclaredSymbol(targetClassDeclaration.Members[2]) as IMethodSymbol).ShouldNotBeNull();

        diagnostics.ShouldNotBeSuccessful(DiagnosticsFactory.PropertyTypeConverterMethodInputTypeCompatibilityError(sourcePropertySymbol, targetMethodSymbol));
    }

    [Theory]
    [InlineData("int")]
    [InlineData("object")]
    public void When_PropertyTypeConverterMethodDoesNotHaveCorrectAdditionalParametersType_Should_ReportDiagnostics(string type)
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<string>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", partial: true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id", attribute: "[PropertyTypeConverter(\"StringToIntTypeConverter\", Parameters = new[] { \"g\" })]")
            .WithProperty<string>("Name")
            .WithStaticMethod("int", "StringToIntTypeConverter", parameters: new[] { "string source", $"{type} parameters" }, body: "return int.Parse(source.ToString());");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        var targetClassDeclaration = compilation.GetClassDeclaration("TargetClass").ShouldNotBeNull();
        var semanticModel = compilation.GetSemanticModel(targetClassDeclaration.SyntaxTree);
        var methodSymbol = semanticModel.GetDeclaredSymbol(targetClassDeclaration.Members[2]) as IMethodSymbol;

        methodSymbol.ShouldNotBeNull();
        diagnostics.ShouldNotBeSuccessful(DiagnosticsFactory.PropertyTypeConverterMethodAdditionalParametersTypeCompatibilityError(methodSymbol));
    }

    [Fact]
    public void When_PropertyTypeConverterMethodDoesNotHaveAdditionalParametersArgument_Should_ReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<string>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", partial: true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id", attribute: "[PropertyTypeConverter(\"StringToIntTypeConverter\", Parameters = new[] { \"g\" })]")
            .WithProperty<string>("Name")
            .WithStaticMethod("int", "StringToIntTypeConverter", parameters: new[] { "string source" }, body: "return int.Parse(source.ToString());");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        var targetClassDeclaration = compilation.GetClassDeclaration("TargetClass").ShouldNotBeNull();
        var semanticModel = compilation.GetSemanticModel(targetClassDeclaration.SyntaxTree);
        var propertySymbol = (semanticModel.GetDeclaredSymbol(targetClassDeclaration.Members[0]) as IPropertySymbol).ShouldNotBeNull();
        var methodSymbol = semanticModel.GetDeclaredSymbol(targetClassDeclaration.Members[2]) as IMethodSymbol;

        methodSymbol.ShouldNotBeNull();
        diagnostics.ShouldNotBeSuccessful(DiagnosticsFactory.PropertyTypeConverterMethodAdditionalParametersIsMissingWarning(propertySymbol, methodSymbol));
    }

    [Theory]
    [InlineData("object[]")]
    [InlineData("object[]?")]
    public void When_PropertyTypeConverterMethodHasCorrectAdditionalParametersType_Should_SpecifyTheParameters(string type)
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile(supportNullableReferenceTypes: true);
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<double>("Id").WithProperty<int>("Index");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", partial: true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id", attribute: """[PropertyTypeConverter("DoubleToIntTypeConverter", Parameters = new object[] { "g", 2 })]""")
            .WithProperty<int>("Index")
            .WithStaticMethod("int", "DoubleToIntTypeConverter", parameters: new[] { "double source", $"{type} parameters" }, body: "return 10;");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var targetClassDeclaration = compilation.GetClassDeclaration("SourceClassMapToExtensions", "MapTo.Tests.TargetClass.g.cs").ShouldNotBeNull();
        targetClassDeclaration.ShouldContain("""Id = global::MapTo.Tests.TargetClass.DoubleToIntTypeConverter(sourceClass.Id, new object[] { "g", 2 })""");
    }

    [Fact]
    public void When_PropertyTypeConverterMethodExists_Should_HaveSameNullabilityAsSourceType()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile(supportNullableReferenceTypes: true);
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty("string?", "Id", defaultValue: "string.Empty");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", partial: true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id", attribute: """[PropertyTypeConverter("StringToIntTypeConverter")]""")
            .WithProperty<int>("Index")
            .WithStaticMethod("int", "StringToIntTypeConverter", parameters: new[] { "string source" }, body: "return int.Parse(source);");

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        var targetClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var sourceClassDeclaration = compilation.GetClassDeclaration("SourceClass", "TestFile1.g.cs").ShouldNotBeNull();
        var semanticModel = compilation.GetSemanticModel(targetClassDeclaration.SyntaxTree);

        var sourceProperty = (semanticModel.GetDeclaredSymbol(sourceClassDeclaration.Members[0]) as IPropertySymbol).ShouldNotBeNull();
        var methodSymbol = semanticModel.GetDeclaredSymbol(targetClassDeclaration.Members[2]) as IMethodSymbol;

        methodSymbol.ShouldNotBeNull();
        diagnostics.ShouldNotBeSuccessful(DiagnosticsFactory.PropertyTypeConverterMethodInputTypeNullCompatibilityError(sourceProperty, methodSymbol));
    }
}