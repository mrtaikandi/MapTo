using MapTo.Diagnostics;

namespace MapTo.Tests;

public class ConstructorMappingTests
{
    private readonly ITestOutputHelper _output;

    public ConstructorMappingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void When_TargetWithReadOnlyPropertyAndConstructor_Should_NotGenerateConstructorAndUseConstructorInitializer()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id", propertyType: PropertyType.ReadOnly | PropertyType.AutoProperty)
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
    public void When_TargetWithReadOnlyPropertyAndNoConstructors_Should_GenerateConstructor()
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
            .ShouldContain("""
                           return new TargetClass(sourceClass.Id)
                           {
                               Name = sourceClass.Name
                           };
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
        var targetClassDeclaration = compilation.GetClassDeclaration("TargetClass").ShouldNotBeNull();
        var expectedError = DiagnosticsFactory.MissingPartialKeywordOnTargetClassError(
            targetClassDeclaration.Identifier.GetLocation(),
            targetClassDeclaration.Identifier.ValueText);

        diagnostics.ShouldNotBeSuccessful(expectedError);
    }

    [Fact]
    public void When_TargetHasMultipleConstructorsWithSameParameterCount_Should_ChooseAnyOfThem()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();

        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id", propertyType: PropertyType.ReadOnly | PropertyType.AutoProperty)
            .WithProperty<string>("Name", propertyType: PropertyType.ReadOnly | PropertyType.AutoProperty)
            .WithConstructor("public TargetClass(string name, int id) => (Name, Id) = (name, id);")
            .WithConstructor("public TargetClass(int id, string name) => (Id, Name) = (id, name);");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation
            .GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("return new TargetClass(sourceClass.Name, sourceClass.Id);");
    }

    [Fact]
    public void When_TargetHasMultipleConstructorsWithoutMapConstructorAttribute_Should_ChooseTheOneWithMostParameters()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();

        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name").WithProperty<int>("Prop");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithProperty<int>("IgnoreMe", attribute: "[IgnoreProperty]")
            .WithProperty<int>("Prop", propertyType: PropertyType.InitProperty | PropertyType.AutoProperty)
            .WithConstructor("public TargetClass(int id) => Id = id;")
            .WithConstructor("public TargetClass(string name) => Name = name;")
            .WithConstructor("public TargetClass(int id, string name) => (Id, Name) = (id, name);")
            .WithConstructor("public TargetClass(int id, string name, int ignoreMe) => (Id, Name, IgnoreMe) = (id, name, ignoreMe);");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        compilation
            .GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain(
                """
                return new TargetClass(sourceClass.Id, sourceClass.Name)
                {
                    Prop = sourceClass.Prop
                };
                """);
    }

    [Fact]
    public void When_TargetHasMultipleConstructors_Should_ChooseTheOneWithMapConstructorAttribute()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();

        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int?>("Id")
            .WithProperty<string>("Name")
            .WithConstructor("public TargetClass(int id) => Id = id;")
            .WithConstructor("[MapConstructor] public TargetClass(string name) => Name = name;")
            .WithConstructor("public TargetClass(int id, string name) => (Id, Name) = (id, name);");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        compilation
            .GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain(
                """
                return new TargetClass(sourceClass.Name)
                {
                    Id = sourceClass.Id
                };
                """);
    }

    [Fact]
    public void When_TargetHasMultipleConstructorsButWithOnlyOneMatchingMappedProperty_Should_ChooseTheOneWithMappedProperty()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();

        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name", attribute: "[IgnoreProperty]")
            .WithConstructor("public TargetClass(int id) => Id = id;")
            .WithConstructor("public TargetClass(string name) => Name = name;")
            .WithConstructor("public TargetClass(int id, string name) => (Id, Name) = (id, name);");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        compilation
            .GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("return new TargetClass(sourceClass.Id);");
    }

    [Fact]
    public void When_TargetHasInitMappedProperty_Should_UseObjectInitializer()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();

        sourceFile.AddClass(AccessModifier.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(AccessModifier.Public, "TargetClass", true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id", propertyType: PropertyType.AutoProperty | PropertyType.InitProperty)
            .WithProperty<string>("Name", attribute: "[IgnoreProperty]");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        compilation.Dump(_output);
        diagnostics.ShouldBeSuccessful();

        compilation
            .GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain(
                """
                return new TargetClass
                {
                    Id = sourceClass.Id
                };
                """);
    }
}