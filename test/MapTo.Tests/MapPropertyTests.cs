namespace MapTo.Tests;

public class MapPropertyTests
{
    private readonly ITestOutputHelper _output;

    public MapPropertyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void When_TargetPropertyHasDifferentNameThanSourceProperty_Should_IgnoreTheProperty()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id")
            .WithProperty<string>("FullName");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("""
                           return new TargetClass
                           {
                               Id = sourceClass.Id
                           };
                           """);
    }

    [Theory]
    [InlineData("""[MapProperty(From = "Name")]""")]
    [InlineData("[MapProperty(From = nameof(SourceClass.Name))]")]
    public void When_TargetPropertyHasDifferentNameButAnnotatedWithMapProperty_Should_MapToCorrectSourceProperty(string attribute)
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Id")
            .WithProperty<string>("FullName", attribute: attribute);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldContain(
            """
            return new TargetClass
            {
                Id = sourceClass.Id,
                FullName = sourceClass.Name
            };
            """);
    }
}