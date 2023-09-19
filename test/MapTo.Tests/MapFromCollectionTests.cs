namespace MapTo.Tests;

public class MapFromCollectionTests
{
    private readonly ITestOutputHelper _output;

    public MapFromCollectionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void When_MappedPropertyTypeIsCollectionOfMappedObjects_Should_MapCollectionItems()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var globalUsings = new[] { "System.Collections.Generic" };

        var nestedSourceFile = builder.AddFile(usings: globalUsings);
        nestedSourceFile.AddClass(AccessModifier.Public, "Employee").WithProperty<int>("Id").WithProperty<string>("Name");
        nestedSourceFile.AddClass(AccessModifier.Public, "Manager").WithProperty<int>("Id").WithProperty<string>("Name").WithProperty("List<Employee>", "Employees");

        var sourceFile = builder.AddFile(usings: globalUsings);
        sourceFile.AddClass(AccessModifier.Public, "EmployeeModel", partial: true, attributes: "[MapFrom(typeof(Employee))]")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name");

        sourceFile.AddClass(AccessModifier.Public, "ManagerModel", partial: true, attributes: "[MapFrom(typeof(Manager))]")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithProperty("List<EmployeeModel>", "Employees");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetGeneratedFileSyntaxTree("MapTo.Tests.ManagerModel.g.cs")
            .GetClassDeclaration("ManagerMapToExtensions")
            .ShouldContain("Employees = manager.Employees.Select(MapTo.Tests.EmployeeMapToExtensions.MapToEmployeeModel).ToList()");
    }

    [Fact]
    public void When_MappedPropertyTypeIsArrayOfMappedObjects_Should_MapCollectionItems()
    {
        // Arrange
        var builder = ScenarioBuilder.BuildAlbumAndArtistModels();

        // Act
        var (_, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
    }
}