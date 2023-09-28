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
        nestedSourceFile.AddClass(Accessibility.Public, "Employee").WithProperty<int>("Id").WithProperty<string>("Name");
        nestedSourceFile.AddClass(Accessibility.Public, "Manager").WithProperty<int>("Id").WithProperty<string>("Name").WithProperty("List<Employee>", "Employees");

        var sourceFile = builder.AddFile(usings: globalUsings);
        sourceFile.AddClass(Accessibility.Public, "EmployeeModel", partial: true, attributes: "[MapFrom(typeof(Employee))]")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name");

        sourceFile.AddClass(Accessibility.Public, "ManagerModel", partial: true, attributes: "[MapFrom(typeof(Manager))]")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithProperty("List<EmployeeModel>", "Employees");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetGeneratedFileSyntaxTree("MapTo.Tests.ManagerModel.g.cs")
            .GetClassDeclaration("ManagerMapToExtensions")
            .ShouldContain("Employees = manager.Employees.Select(global::MapTo.Tests.EmployeeMapToExtensions.MapToEmployeeModel).ToList()");
    }

    [Fact]
    public void When_MappedPropertyTypeIsArrayOfMappedObjectsWithReferenceHandlingEnabled_Should_GenerateMapItemArrays()
    {
        // Arrange
        var builder = ScenarioBuilder.BuildAlbumAndArtistModels(TestSourceBuilderOptions.Create(analyzerConfigOptions: new Dictionary<string, string>
        {
            [nameof(CodeGeneratorOptions.ReferenceHandling)] = ReferenceHandling.Enabled.ToString()
        }));

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var extensionClass = compilation.GetGeneratedFileSyntaxTree("MapTo.Tests.AlbumViewModel.g.cs").GetClassDeclaration("AlbumMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain(
            """
            if (!ReferenceEquals(album.Artists, null))
            {
                target.Artists = MapToArtistViewModelArray(album.Artists, referenceHandler);
            }
            """);

        extensionClass.ShouldContain(
            """
            private static global::MapTo.Tests.ArtistViewModel[] MapToArtistViewModelArray(global::MapTo.Tests.Artist[] sourceArray, global::System.Collections.Generic.Dictionary<int, object> referenceHandler)
            {
                var targetArray = new global::MapTo.Tests.ArtistViewModel[sourceArray.Length];
                for (var i = 0; i < sourceArray.Length; i++)
                {
                    targetArray[i] = global::MapTo.Tests.ArtistMapToExtensions.MapToArtistViewModel(sourceArray[i], referenceHandler);
                }
            
                return targetArray;
            }
            """);
    }

    [Fact]
    public void When_MappedPropertyTypeIsArrayOfMappedObjectsWithReferenceHandlingDisabled_Should_GenerateMapItemArrays()
    {
        // Arrange
        var builder = ScenarioBuilder.BuildSpotifyModels(TestSourceBuilderOptions.Create(analyzerConfigOptions: new Dictionary<string, string>
        {
            [nameof(CodeGeneratorOptions.ReferenceHandling)] = ReferenceHandling.Disabled.ToString()
        }));

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var extensionClass = compilation.GetGeneratedFileSyntaxTree("ExternalTestData.Models.SpotifyAlbum.g.cs").GetClassDeclaration("SpotifyAlbumDtoMapToExtensions")
            .ShouldNotBeNull();

        extensionClass.ShouldContain(
            """
            if (!ReferenceEquals(spotifyAlbumDto.Artists, null))
            {
                target.Artists = MapToArtistArray(spotifyAlbumDto.Artists);
            }
            """);

        extensionClass.ShouldContain(
            """
            private static global::ExternalTestData.Models.Artist[] MapToArtistArray(global::ExternalTestData.Models.ArtistDto[] sourceArray)
            {
                var targetArray = new global::ExternalTestData.Models.Artist[sourceArray.Length];
                for (var i = 0; i < sourceArray.Length; i++)
                {
                    targetArray[i] = global::ExternalTestData.Models.ArtistDtoMapToExtensions.MapToArtist(sourceArray[i]);
                }
            
                return targetArray;
            }
            """);
    }

    [Fact]
    public void When_MappedPropertyTypeIsArrayOfMappedObjectsWithReferenceHandlingAuto_Should_GenerateMapItemArrays()
    {
        // Arrange
        var builder = ScenarioBuilder.BuildSpotifyModels(TestSourceBuilderOptions.Create(analyzerConfigOptions: new Dictionary<string, string>
        {
            [nameof(CodeGeneratorOptions.ReferenceHandling)] = ReferenceHandling.Auto.ToString()
        }));

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var extensionClass = compilation.GetGeneratedFileSyntaxTree("ExternalTestData.Models.SpotifyAlbum.g.cs")
            .GetClassDeclaration("SpotifyAlbumDtoMapToExtensions")
            .ShouldNotBeNull();

        extensionClass.ShouldContain(
            """
            if (!ReferenceEquals(spotifyAlbumDto.Artists, null))
            {
                target.Artists = MapToArtistArray(spotifyAlbumDto.Artists, referenceHandler);
            }
            """);

        extensionClass.ShouldContain(
            """
            private static global::ExternalTestData.Models.Artist[] MapToArtistArray(global::ExternalTestData.Models.ArtistDto[] sourceArray, global::System.Collections.Generic.Dictionary<int, object> referenceHandler)
            {
                var targetArray = new global::ExternalTestData.Models.Artist[sourceArray.Length];
                for (var i = 0; i < sourceArray.Length; i++)
                {
                    targetArray[i] = global::ExternalTestData.Models.ArtistDtoMapToExtensions.MapToArtist(sourceArray[i], referenceHandler);
                 }
            
                 return targetArray;
             }
            """);

        extensionClass.ShouldContain(
            """
            if (!ReferenceEquals(spotifyAlbumDto.Copyrights, null))
            {
                target.Copyrights = MapToCopyrightArray(spotifyAlbumDto.Copyrights);
            }
            """);

        extensionClass.ShouldContain(
            """
            private static global::ExternalTestData.Models.Copyright[] MapToCopyrightArray(global::ExternalTestData.Models.CopyrightDto[] sourceArray)
            {
                var targetArray = new global::ExternalTestData.Models.Copyright[sourceArray.Length];
                for (var i = 0; i < sourceArray.Length; i++)
                {
                    targetArray[i] = global::ExternalTestData.Models.CopyrightDtoMapToExtensions.MapToCopyright(sourceArray[i]);
                }
            
                return targetArray;
            }
            """);
    }

    [Theory]
    [InlineData(ReferenceHandling.Auto)]
    [InlineData(ReferenceHandling.Enabled)]
    [InlineData(ReferenceHandling.Disabled)]
    public void When_MappedPropertyTypeIsArrayOfPrimitivesWithCopyPrimitiveTypesEnabled_Should_GenerateMapPrimitiveArrays(ReferenceHandling referenceHandling)
    {
        // Arrange
        var builder = ScenarioBuilder.BuildSpotifyModels(TestSourceBuilderOptions.Create(analyzerConfigOptions: new Dictionary<string, string>
        {
            [nameof(CodeGeneratorOptions.ReferenceHandling)] = referenceHandling.ToString(),
            [nameof(CodeGeneratorOptions.CopyPrimitiveArrays)] = "true"
        }));

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var extensionClass = compilation.GetGeneratedFileSyntaxTree("ExternalTestData.Models.SpotifyAlbum.g.cs").GetClassDeclaration("SpotifyAlbumDtoMapToExtensions")
            .ShouldNotBeNull();

        extensionClass.ShouldContain(
            """
            if (!ReferenceEquals(spotifyAlbumDto.AvailableMarkets, null))
            {
                target.AvailableMarkets = MapToStringArray(spotifyAlbumDto.AvailableMarkets);
            }
            """);

        extensionClass.ShouldContain(
            """
            private static string[] MapToStringArray(string[] sourceArray)
            {
                var targetArray = new string[sourceArray.Length];
                global::System.Array.Copy(sourceArray, targetArray, sourceArray.Length);
            
                return targetArray;
            }
            """);
    }
}