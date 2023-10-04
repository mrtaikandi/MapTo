using MapTo.Diagnostics;

namespace MapTo.Tests;

public class MapFromCollectionTests
{
    private readonly ITestOutputHelper _output;

    public MapFromCollectionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData("List<EmployeeModel>", "List<Employee>", "ToList")]
    [InlineData("IList<EmployeeModel>", "List<Employee>", "ToList")]
    [InlineData("ICollection<EmployeeModel>", "List<Employee>", "ToList")]
    [InlineData("IEnumerable<EmployeeModel>", "List<Employee>", "ToArray")]
    [InlineData("IReadOnlyList<EmployeeModel>", "List<Employee>", "ToArray")]
    [InlineData("IReadOnlyCollection<EmployeeModel>", "List<Employee>", "ToArray")]
    [InlineData("List<EmployeeModel>", "IList<Employee>", "ToList")]
    [InlineData("IList<EmployeeModel>", "IList<Employee>", "ToList")]
    [InlineData("ICollection<EmployeeModel>", "IList<Employee>", "ToList")]
    [InlineData("IEnumerable<EmployeeModel>", "IList<Employee>", "ToArray")]
    [InlineData("IReadOnlyList<EmployeeModel>", "IList<Employee>", "ToArray")]
    [InlineData("IReadOnlyCollection<EmployeeModel>", "IList<Employee>", "ToArray")]
    [InlineData("List<EmployeeModel>", "ICollection<Employee>", "ToList")]
    [InlineData("IList<EmployeeModel>", "ICollection<Employee>", "ToList")]
    [InlineData("ICollection<EmployeeModel>", "ICollection<Employee>", "ToList")]
    [InlineData("IEnumerable<EmployeeModel>", "ICollection<Employee>", "ToArray")]
    [InlineData("IReadOnlyList<EmployeeModel>", "ICollection<Employee>", "ToArray")]
    [InlineData("IReadOnlyCollection<EmployeeModel>", "ICollection<Employee>", "ToArray")]
    [InlineData("List<EmployeeModel>", "IReadOnlyList<Employee>", "ToList")]
    [InlineData("IList<EmployeeModel>", "IReadOnlyList<Employee>", "ToList")]
    [InlineData("ICollection<EmployeeModel>", "IReadOnlyList<Employee>", "ToList")]
    [InlineData("IEnumerable<EmployeeModel>", "IReadOnlyList<Employee>", "ToArray")]
    [InlineData("IReadOnlyList<EmployeeModel>", "IReadOnlyList<Employee>", "ToArray")]
    [InlineData("IReadOnlyCollection<EmployeeModel>", "IReadOnlyList<Employee>", "ToArray")]
    [InlineData("List<EmployeeModel>", "IReadOnlyCollection<Employee>", "ToList")]
    [InlineData("IList<EmployeeModel>", "IReadOnlyCollection<Employee>", "ToList")]
    [InlineData("ICollection<EmployeeModel>", "IReadOnlyCollection<Employee>", "ToList")]
    [InlineData("IEnumerable<EmployeeModel>", "IReadOnlyCollection<Employee>", "ToArray")]
    [InlineData("IReadOnlyList<EmployeeModel>", "IReadOnlyCollection<Employee>", "ToArray")]
    [InlineData("IReadOnlyCollection<EmployeeModel>", "IReadOnlyCollection<Employee>", "ToArray")]
    [InlineData("List<EmployeeModel>", "Employee[]", "ToList")]
    [InlineData("IList<EmployeeModel>", "Employee[]", "ToList")]
    [InlineData("ICollection<EmployeeModel>", "Employee[]", "ToList")]
    [InlineData("IEnumerable<EmployeeModel>", "Employee[]", "ToArray")]
    [InlineData("IReadOnlyList<EmployeeModel>", "Employee[]", "ToArray")]
    [InlineData("IReadOnlyCollection<EmployeeModel>", "Employee[]", "ToArray")]
    public void When_MappedPropertyTypeIsCollectionOfMappedObjects_Should_MapCollectionItems(string targetCollectionType, string sourceCollectionType, string expectedLinqMethod)
    {
        // Arrange
        var builder = new TestSourceBuilder();

        var globalUsings = new[] { "System.Collections.Generic" };

        var nestedSourceFile = builder.AddFile(usings: globalUsings);
        nestedSourceFile.AddClass(Accessibility.Public, "Employee").WithProperty<int>("Id").WithProperty<string>("Name");
        nestedSourceFile.AddClass(Accessibility.Public, "Manager").WithProperty<int>("Id").WithProperty<string>("Name").WithProperty(sourceCollectionType, "Employees");

        var sourceFile = builder.AddFile(usings: globalUsings);
        sourceFile.AddClass(Accessibility.Public, "EmployeeModel", partial: true, attributes: "[MapFrom(typeof(Employee))]")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name");

        sourceFile.AddClass(Accessibility.Public, "ManagerModel", partial: true, attributes: "[MapFrom(typeof(Manager))]")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithProperty(targetCollectionType, "Employees");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetGeneratedFileSyntaxTree("MapTo.Tests.ManagerModel.g.cs")
            .GetClassDeclaration("ManagerMapToExtensions")
            .ShouldContain($"Employees = manager.Employees?.Select<global::MapTo.Tests.Employee, global::MapTo.Tests.EmployeeModel>(global::MapTo.Tests.EmployeeMapToExtensions.MapToEmployeeModel).{expectedLinqMethod}()");
    }

    [Theory]
    [InlineData("EmployeeModel[]", "List<Employee>", false)]
    [InlineData("EmployeeModel[]", "IList<Employee>", false)]
    [InlineData("EmployeeModel[]", "ICollection<Employee>", false)]
    [InlineData("EmployeeModel[]", "IReadOnlyList<Employee>", false)]
    [InlineData("EmployeeModel[]", "IReadOnlyCollection<Employee>", false)]
    [InlineData("EmployeeModel[]", "Employee[]", true)]
    public void When_MappedPropertyTypeIsArrayOfMappedObjects_Should_GenerateMapItemArraysOnlyIfSourceIsArray(
        string targetCollectionType,
        string sourceCollectionType,
        bool shouldGenerateMapItemArrays)
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var globalUsings = new[] { "System.Collections.Generic" };

        var nestedSourceFile = builder.AddFile(usings: globalUsings);
        nestedSourceFile.AddClass(Accessibility.Public, "Employee").WithProperty<int>("Id").WithProperty<string>("Name");
        nestedSourceFile.AddClass(Accessibility.Public, "Manager").WithProperty<int>("Id").WithProperty<string>("Name").WithProperty(sourceCollectionType, "Employees");

        var sourceFile = builder.AddFile(usings: globalUsings);
        sourceFile.AddClass(Accessibility.Public, "EmployeeModel", partial: true, attributes: "[MapFrom(typeof(Employee))]")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name");

        sourceFile.AddClass(Accessibility.Public, "ManagerModel", partial: true, attributes: "[MapFrom(typeof(Manager))]")
            .WithProperty<int>("Id")
            .WithProperty<string>("Name")
            .WithProperty(targetCollectionType, "Employees");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetGeneratedFileSyntaxTree("MapTo.Tests.ManagerModel.g.cs").GetClassDeclaration("ManagerMapToExtensions").ShouldNotBeNull();

        if (!shouldGenerateMapItemArrays)
        {
            extensionClass.ShouldContain("Employees = manager.Employees?.Select(global::MapTo.Tests.EmployeeMapToExtensions.MapToEmployeeModel).ToArray()");
        }
        else
        {
            extensionClass.ShouldContain("target.Employees = MapToEmployeeModelArray(manager.Employees);");

            extensionClass.ShouldContain(
                """
                private static global::MapTo.Tests.EmployeeModel[] MapToEmployeeModelArray(global::MapTo.Tests.Employee[] sourceArray)
                {
                    var targetArray = new global::MapTo.Tests.EmployeeModel[sourceArray.Length];
                    for (var i = 0; i < sourceArray.Length; i++)
                    {
                        targetArray[i] = global::MapTo.Tests.EmployeeMapToExtensions.MapToEmployeeModel(sourceArray[i]);
                    }
                
                    return targetArray;
                }
                """);
        }
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
        extensionClass.ShouldContain("target.Artists = MapToArtistViewModelArray(album.Artists, referenceHandler);");

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

        extensionClass.ShouldContain("target.Artists = MapToArtistArray(spotifyAlbumDto.Artists);");
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
        var builder = ScenarioBuilder.BuildSpotifyModels(TestSourceBuilderOptions.Create(
            analyzerConfigOptions: new Dictionary<string, string>
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

        extensionClass.ShouldContain("target.Artists = MapToArtistArray(spotifyAlbumDto.Artists, referenceHandler);");
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

        extensionClass.ShouldContain("target.Copyrights = MapToCopyrightArray(spotifyAlbumDto.Copyrights);");

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

        extensionClass.ShouldContain("target.AvailableMarkets = MapToStringArray(spotifyAlbumDto.AvailableMarkets);");
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

    [Theory]
    [InlineData("List<string>", false)]
    [InlineData("string[]", false)]
    [InlineData("ArrayList", true)]
    public void When_SourcePropertyTypeIsNonGenericEnumerable_Should_OnlyMapToPropertyWithSameType(string targetPropertyType, bool isCompatible)
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var globalUsings = new[] { "System.Collections", "System.Collections.Generic" };

        var nestedSourceFile = builder.AddFile(usings: globalUsings);
        nestedSourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty("ArrayList", "Prop1");
        nestedSourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty(targetPropertyType, "Prop1");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        if (isCompatible)
        {
            diagnostics.ShouldBeSuccessful();
        }
        else
        {
            var targetClassDeclaration = compilation.GetClassDeclaration("TargetClass");
            targetClassDeclaration.ShouldNotBeNull();

            var propertySymbol = compilation
                .GetSemanticModel(targetClassDeclaration.SyntaxTree)
                .GetDeclaredSymbol(targetClassDeclaration.Members[0])
                .ShouldNotBeNull();

            var expectedError = DiagnosticsFactory.PropertyTypeConverterRequiredError(propertySymbol);
            diagnostics.ShouldNotBeSuccessful(expectedError);
        }
    }

    [Theory]
    [InlineData(NullHandling.Auto)]
    [InlineData(NullHandling.SetNull)]
    public void When_NullContextIsDisabled_NullHandlingAutoAndSetNull_Should_BeTheSame(NullHandling nullHandling)
    {
        // Arrange
        var builder = new TestSourceBuilder(
            supportNullReferenceTypes: false,
            analyzerConfigOptions: new Dictionary<string, string>
            {
                [nameof(CodeGeneratorOptions.NullHandling)] = nullHandling.ToString()
            });

        var sourceFile = builder.AddFile(usings: new[] { "System.Collections", "System.Collections.Generic" });
        sourceFile.AddClass(Accessibility.Public, "MiddleClass").WithProperty<string>("Key");
        sourceFile.AddClass(Accessibility.Public, "MappedMiddleClass", attributes: "[MapFrom(typeof(MiddleClass))]").WithProperty<string>("Key");

        sourceFile.AddClass(Accessibility.Public, "SourceClass")
            .WithProperty("List<int>", "Prop1")
            .WithProperty("string[]", "Prop2")
            .WithProperty("MiddleClass[]", "Prop3")
            .WithProperty("List<MiddleClass>", "Prop4");

        sourceFile.AddClass(Accessibility.Public, "TargetClass", true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty("List<int>", "Prop1")
            .WithProperty("string[]", "Prop2")
            .WithProperty("MiddleClass[]", "Prop3")
            .WithProperty("List<MappedMiddleClass>", "Prop4");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        compilation.Dump(_output);
        diagnostics.ShouldBeSuccessful();

        var extensionClass = compilation.GetClassDeclaration("SourceClassMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain(
            """
            target.Prop1 = sourceClass.Prop1;

            if (sourceClass.Prop2 is not null)
            {
                target.Prop2 = sourceClass.Prop2;
            }

            if (sourceClass.Prop3 is not null)
            {
                target.Prop3 = sourceClass.Prop3;
            }

            if (sourceClass.Prop4 is not null)
            {
                target.Prop4 = sourceClass.Prop4.Select<global::MapTo.Tests.MiddleClass, global::MapTo.Tests.MappedMiddleClass>(global::MapTo.Tests.MiddleClassMapToExtensions.MapToMappedMiddleClass).ToList();
            }
            """);
    }
}