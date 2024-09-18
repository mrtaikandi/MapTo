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

        diagnostics.ShouldNotBeSuccessful(DiagnosticsFactory.PropertyTypeConverterMethodInputTypeCompatibilityError(
            sourcePropertyName: sourcePropertySymbol.Name,
            sourcePropertyType: sourcePropertySymbol.Type,
            converterMethodSymbol: targetMethodSymbol));
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

        var targetClassDeclaration = compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions", "MapTo.Tests.TargetClass.g.cs").ShouldNotBeNull();
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
        diagnostics.ShouldNotBeSuccessful(DiagnosticsFactory.PropertyTypeConverterMethodInputTypeNullCompatibilityError(sourceProperty.Name, methodSymbol));
    }

    [Fact]
    public void When_PropertyIsEnumerableAndHasIncorrectPropertyTypeConverter_Should_UseReportDiagnostics()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var globalUsings = new[] { "System", "System.Collections", "System.Collections.Generic" };

        var nestedSourceFile = builder.AddFile(usings: globalUsings, supportNullableReferenceTypes: true);
        nestedSourceFile.AddClass(Accessibility.Public, "SourceClass")
            .WithProperty("ArraySegment<byte>", "Prop1");

        nestedSourceFile.AddClass(accessibility: Accessibility.Public, name: "TargetClass", partial: true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty("byte[]?", "Prop1", attributes: ["[PropertyTypeConverter(nameof(MapProp1))]"])
            .WithStaticMethod("byte[]?", "MapProp1", "return segment?.ToArray();", parameter: "ArraySegment<byte>? segment");

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        var targetClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var sourceClassDeclaration = compilation.GetClassDeclaration("SourceClass", "TestFile1.g.cs").ShouldNotBeNull();
        var semanticModel = compilation.GetSemanticModel(targetClassDeclaration.SyntaxTree);

        var sourceProperty = (semanticModel.GetDeclaredSymbol(sourceClassDeclaration.Members[0]) as IPropertySymbol).ShouldNotBeNull();
        var methodSymbol = semanticModel.GetDeclaredSymbol(targetClassDeclaration.Members[1]) as IMethodSymbol;

        methodSymbol.ShouldNotBeNull();

        diagnostics.ShouldNotBeSuccessful(DiagnosticsFactory.PropertyTypeConverterMethodInputTypeCompatibilityError(sourceProperty.Name, sourceProperty.Type, methodSymbol));
    }

    [Fact]
    public void When_PropertyIsGenericEnumerableAndHasPropertyTypeConverter_Should_UseTheTypeConverter()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var globalUsings = new[] { "System", "System.Collections", "System.Collections.Generic" };

        builder
            .AddFile(usings: globalUsings, supportNullableReferenceTypes: false)
            .AddClass(Accessibility.Public, "SourceClass")
            .WithProperty("IDictionary", "Prop1");

        builder
            .AddFile(usings: globalUsings, supportNullableReferenceTypes: true)
            .AddClass(accessibility: Accessibility.Public, name: "TargetClass", partial: true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty("IReadOnlyDictionary<object, object?>?", "Prop1", attributes: ["[PropertyTypeConverter(nameof(MapProp1))]"])
            .WithStaticMethod("IReadOnlyDictionary<object, object?>?", "MapProp1", "throw new NotImplementedException();", parameter: "IDictionary? segment");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var targetClassDeclaration = compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions").ShouldNotBeNull();
        targetClassDeclaration.ShouldContain("target.Prop1 = global::MapTo.Tests.TargetClass.MapProp1(sourceClass.Prop1);");
    }

    [Fact]
    public void When_PropertyTypeConverterMethodExistsInAnotherClass_Should_UseTheExternalClass()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile(supportNullableReferenceTypes: true);
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty("string", "Id", defaultValue: "string.Empty");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", partial: true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id", attribute: """[PropertyTypeConverter("MapTo.Internals.TypeConverter.StringToIntTypeConverter")]""")
            .WithProperty<int>("Index");

        builder.AddFile("TypeConverters", supportNullableReferenceTypes: true, ns: "MapTo.Internals")
            .AddClass(Accessibility.Internal, "TypeConverter", isStatic: true)
            .WithStaticMethod("int", "StringToIntTypeConverter", parameters: new[] { "string source" }, body: "return int.Parse(source);");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var targetClassDeclaration = compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions", "MapTo.Tests.TargetClass.g.cs").ShouldNotBeNull();
        targetClassDeclaration.ShouldContain("Id = global::MapTo.Internals.TypeConverter.StringToIntTypeConverter(sourceClass.Id)");
    }

    [Fact]
    public void When_PropertyTypeConverterMethodExistsInAnotherClass_Should_UseTheExternalClassWithImportedNamespace()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile(supportNullableReferenceTypes: true, usings: ["MapTo.Internals"]);
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty("string", "Id", defaultValue: "string.Empty");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", partial: true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id", attribute: """[PropertyTypeConverter(nameof(TypeConverter.StringToIntTypeConverter))]""")
            .WithProperty<int>("Index");

        builder.AddFile("TypeConverters", supportNullableReferenceTypes: true, ns: "MapTo.Internals")
            .AddClass(Accessibility.Internal, "TypeConverter", isStatic: true)
            .WithStaticMethod("int", "StringToIntTypeConverter", parameters: new[] { "string source" }, body: "return int.Parse(source);");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var targetClassDeclaration = compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions", "MapTo.Tests.TargetClass.g.cs").ShouldNotBeNull();
        targetClassDeclaration.ShouldContain("Id = global::MapTo.Internals.TypeConverter.StringToIntTypeConverter(sourceClass.Id)");
    }

    [Fact]
    public void When_PropertyTypeConverterIsUsedInBaseClass_Should_UseTheConverterMethod()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile(supportNullableReferenceTypes: false)
            .WithBody(
                """
                public class Source
                {
                    public int Prop1 { get; init; }
                    public string Prop3 { get; init; } = "0";
                }
                """);

        builder.AddFile(supportNullableReferenceTypes: true)
            .WithBody(
                """
                public abstract class TargetBase
                {
                    [PropertyTypeConverter(nameof(TypeConverters.ConvertToDouble))]
                    public double Prop3 { get; init; }
                }
                """);

        builder.AddFile(supportNullableReferenceTypes: true)
            .WithBody(
                """
                [MapFrom<Source>]
                public class Target : TargetBase
                {
                    public int Prop1 { get; init; }
                }
                """);

        builder.AddFile("TypeConverters", supportNullableReferenceTypes: true)
            .WithBody(
                """
                public static class TypeConverters
                {
                    internal static double ConvertToDouble(string source) => double.Parse(source);
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
            public static Target? MapToTarget(this Source? source)
            {
                if (source is null)
                {
                    return null;
                }

                return new Target
                {
                    Prop1 = source.Prop1,
                    Prop3 = global::MapTo.Tests.TypeConverters.ConvertToDouble(source.Prop3)
                };
            }
            """);
    }
}