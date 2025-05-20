namespace MapTo.Tests;

public class MapProjectionTests
{
    private readonly ITestOutputHelper _output;

    public MapProjectionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void When_ProjectionIsNone_Should_NotGenerateProjectionExtensionMethods()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleMappedRecordInSameNamespace(mapFromProjectionType: ProjectionType.None);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceRecordToDestinationRecordMapToExtensions")
            .ShouldBe(
                ignoreWhitespace: true,
                $$"""
                  {{ScenarioBuilder.GeneratedCodeAttribute}}
                  public static partial class SourceRecordToDestinationRecordMapToExtensions
                  {
                      [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("sourceRecord")]
                      public static DestinationRecord? MapToDestinationRecord(this SourceRecord? sourceRecord)
                      {
                          if (sourceRecord is null)
                          {
                              return null;
                          }

                          return new DestinationRecord(sourceRecord.Value);
                      }
                  }
                  """);
    }

    [Fact]
    public void When_ProjectionIsArray_Should_GenerateProjectionExtensionMethods()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleMappedRecordInSameNamespace(mapFromProjectionType: ProjectionType.Array);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceRecordToDestinationRecordMapToExtensions").ShouldNotBeNull();
        var methods = extensionClass.Members.OfType<MethodDeclarationSyntax>().ToArray();
        methods.Length.ShouldBe(2);
        methods[1].ShouldBe(
            ignoreWhitespace: true,
            """
            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
            public static global::MapTo.Tests.DestinationRecord[]? MapToDestinationRecords(this global::MapTo.Tests.SourceRecord[]? source)
            {
                if (source is null)
                {
                    return null;
                }

                var target = new global::MapTo.Tests.DestinationRecord[source.Length];
                for (var i = 0; i < source.Length; i++)
                {
                    target[i] = MapToDestinationRecord(source[i]);
                }

                return target;
            }
            """);
    }

    [Fact]
    public void When_ProjectionIsEnumerable_Should_GenerateProjectionExtensionMethods()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleMappedRecordInSameNamespace(mapFromProjectionType: ProjectionType.IEnumerable);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceRecordToDestinationRecordMapToExtensions").ShouldNotBeNull();
        var methods = extensionClass.Members.OfType<MethodDeclarationSyntax>().ToArray();
        methods.Length.ShouldBe(2);
        methods[1].ShouldBe(
            ignoreWhitespace: true,
            """
            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
            public static global::System.Collections.Generic.IEnumerable<global::MapTo.Tests.DestinationRecord>? MapToDestinationRecords(this global::System.Collections.Generic.IEnumerable<global::MapTo.Tests.SourceRecord>? source)
            {
                if (source is null)
                {
                    return null;
                }

                return global::System.Linq.Enumerable.Select(source, x => MapToDestinationRecord(x));
            }
            """);
    }

    [Fact]
    public void When_ProjectionIsICollection_Should_GenerateProjectionExtensionMethods()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleMappedRecordInSameNamespace(mapFromProjectionType: ProjectionType.ICollection);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceRecordToDestinationRecordMapToExtensions").ShouldNotBeNull();
        var methods = extensionClass.Members.OfType<MethodDeclarationSyntax>().ToArray();
        methods.Length.ShouldBe(2);
        methods[1].ShouldBe(
            """
            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
            public static global::System.Collections.Generic.ICollection<global::MapTo.Tests.DestinationRecord>? MapToDestinationRecords(this global::System.Collections.Generic.ICollection<global::MapTo.Tests.SourceRecord>? source)
            {
                if (source is null)
                {
                    return null;
                }

                var target = new global::System.Collections.Generic.List<global::MapTo.Tests.DestinationRecord>(source.Count);
                foreach (var item in source)
                {
                    target.Add(MapToDestinationRecord(item));
                }

                return target;
            }
            """);
    }

    [Fact]
    public void When_ProjectionIsIReadOnlyCollection_Should_GenerateProjectionExtensionMethods()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleMappedRecordInSameNamespace(mapFromProjectionType: ProjectionType.IReadOnlyCollection);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceRecordToDestinationRecordMapToExtensions").ShouldNotBeNull();
        var methods = extensionClass.Members.OfType<MethodDeclarationSyntax>().ToArray();
        methods.Length.ShouldBe(2);
        methods[1].ShouldBe(
            ignoreWhitespace: true,
            """
            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
            public static global::System.Collections.Generic.IReadOnlyCollection<global::MapTo.Tests.DestinationRecord>? MapToDestinationRecords(this global::System.Collections.Generic.IReadOnlyCollection<global::MapTo.Tests.SourceRecord>? source)
            {
                if (source is null)
                {
                    return null;
                }

                var target = new global::System.Collections.Generic.List<global::MapTo.Tests.DestinationRecord>(source.Count);
                foreach (var item in source)
                {
                    target.Add(MapToDestinationRecord(item));
                }

                return target;
            }
            """);
    }

    [Fact]
    public void When_ProjectionIsIList_Should_GenerateProjectionExtensionMethods()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleMappedRecordInSameNamespace(mapFromProjectionType: ProjectionType.IList);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceRecordToDestinationRecordMapToExtensions").ShouldNotBeNull();
        var methods = extensionClass.Members.OfType<MethodDeclarationSyntax>().ToArray();
        methods.Length.ShouldBe(2);
        methods[1].ShouldBe(
            ignoreWhitespace: true,
            """
            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
            public static global::System.Collections.Generic.IList<global::MapTo.Tests.DestinationRecord>? MapToDestinationRecords(this global::System.Collections.Generic.IList<global::MapTo.Tests.SourceRecord>? source)
            {
                if (source is null)
                {
                    return null;
                }

                var target = new global::System.Collections.Generic.List<global::MapTo.Tests.DestinationRecord>(source.Count);
                for (var i = 0; i < source.Count; i++)
                {
                    target.Add(MapToDestinationRecord(source[i]));
                }

                return target;
            }
            """);
    }

    [Fact]
    public void When_ProjectionIsIReadOnlyList_Should_GenerateProjectionExtensionMethods()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleMappedRecordInSameNamespace(mapFromProjectionType: ProjectionType.IReadOnlyList);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceRecordToDestinationRecordMapToExtensions").ShouldNotBeNull();
        var methods = extensionClass.Members.OfType<MethodDeclarationSyntax>().ToArray();
        methods.Length.ShouldBe(2);
        methods[1].ShouldBe(
            ignoreWhitespace: true,
            """
            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
            public static global::System.Collections.Generic.IReadOnlyList<global::MapTo.Tests.DestinationRecord>? MapToDestinationRecords(this global::System.Collections.Generic.IReadOnlyList<global::MapTo.Tests.SourceRecord>? source)
            {
                if (source is null)
                {
                    return null;
                }

                var target = new global::System.Collections.Generic.List<global::MapTo.Tests.DestinationRecord>(source.Count);
                for (var i = 0; i < source.Count; i++)
                {
                    target.Add(MapToDestinationRecord(source[i]));
                }

                return target;
            }
            """);
    }

    [Fact]
    public void When_ProjectionIsList_Should_GenerateProjectionExtensionMethods()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleMappedRecordInSameNamespace(mapFromProjectionType: ProjectionType.List);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceRecordToDestinationRecordMapToExtensions").ShouldNotBeNull();
        var methods = extensionClass.Members.OfType<MethodDeclarationSyntax>().ToArray();
        methods.Length.ShouldBe(2);
        methods[1].ShouldBe(
            ignoreWhitespace: true,
            """
            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
            public static global::System.Collections.Generic.List<global::MapTo.Tests.DestinationRecord>? MapToDestinationRecords(this global::System.Collections.Generic.List<global::MapTo.Tests.SourceRecord>? source)
            {
                if (source is null)
                {
                    return null;
                }

                var target = new global::System.Collections.Generic.List<global::MapTo.Tests.DestinationRecord>(source.Count);
                for (var i = 0; i < source.Count; i++)
                {
                    target.Add(MapToDestinationRecord(source[i]));
                }

                return target;
            }
            """);
    }

    [Fact]
    public void When_ProjectionIsMemory_Should_GenerateProjectionExtensionMethods()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleMappedRecordInSameNamespace(mapFromProjectionType: ProjectionType.Memory);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceRecordToDestinationRecordMapToExtensions").ShouldNotBeNull();
        var methods = extensionClass.Members.OfType<MethodDeclarationSyntax>().ToArray();
        methods.Length.ShouldBe(2);
        methods[1].ShouldBe(
            ignoreWhitespace: true,
            """
            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
            public static global::System.Memory<global::MapTo.Tests.DestinationRecord>? MapToDestinationRecords(this global::System.Memory<global::MapTo.Tests.SourceRecord>? source)
            {
                if (source is null)
                {
                    return null;
                }

                var target = new global::MapTo.Tests.DestinationRecord[source.Value.Span.Length];
                for (var i = 0; i < source.Value.Span.Length; i++)
                {
                    target[i] = MapToDestinationRecord(source.Value.Span[i]);
                }

                return target;
            }
            """);
    }

    [Fact]
    public void When_ProjectionIsReadOnlyMemory_Should_GenerateProjectionExtensionMethods()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleMappedRecordInSameNamespace(mapFromProjectionType: ProjectionType.ReadOnlyMemory);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceRecordToDestinationRecordMapToExtensions").ShouldNotBeNull();
        var methods = extensionClass.Members.OfType<MethodDeclarationSyntax>().ToArray();
        methods.Length.ShouldBe(2);
        methods[1].ShouldBe(
            ignoreWhitespace: true,
            """
            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
            public static global::System.ReadOnlyMemory<global::MapTo.Tests.DestinationRecord>? MapToDestinationRecords(this global::System.ReadOnlyMemory<global::MapTo.Tests.SourceRecord>? source)
            {
                if (source is null)
                {
                    return null;
                }

                var target = new global::MapTo.Tests.DestinationRecord[source.Value.Span.Length];
                for (var i = 0; i < source.Value.Span.Length; i++)
                {
                    target[i] = MapToDestinationRecord(source.Value.Span[i]);
                }

                return target;
            }
            """);
    }

    [Fact]
    public void When_MultipleProjectionsExist_Should_GenerateProjectionExtensionMethodsForEach()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: true));
        builder.AddFile(supportNullableReferenceTypes: true, usings: new[] { "System.Collections.Generic" }).WithBody(
            """
            public record SourceRecord(int Value);

            [MapFrom(typeof(SourceRecord), ProjectTo = ProjectionType.Array | ProjectionType.IEnumerable | ProjectionType.List)]
            public partial record DestinationRecord(int Value)
            {
                public static partial IEnumerable<DestinationRecord> MapToDestinationRecords(IEnumerable<SourceRecord> myRandomName);
            }
            """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceRecordToDestinationRecordMapToExtensions").ShouldNotBeNull();
        var methods = extensionClass.Members.OfType<MethodDeclarationSyntax>().ToArray();
        methods.Length.ShouldBe(4);
        methods[1].ShouldBe(
            """
            public static global::System.Collections.Generic.IEnumerable<global::MapTo.Tests.DestinationRecord> MapToDestinationRecords(this global::System.Collections.Generic.IEnumerable<global::MapTo.Tests.SourceRecord> myRandomName)
            {
                return global::System.Linq.Enumerable.Select(myRandomName, x => MapToDestinationRecord(x));
            }
            """);

        methods[2].ShouldBe(
            ignoreWhitespace: true,
            """
            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
            public static global::MapTo.Tests.DestinationRecord[]? MapToDestinationRecords(this global::MapTo.Tests.SourceRecord[]? source)
            {
                if (source is null)
                {
                    return null;
                }

                var target = new global::MapTo.Tests.DestinationRecord[source.Length];
                for (var i = 0; i < source.Length; i++)
                {
                    target[i] = MapToDestinationRecord(source[i]);
                }

                return target;
            }
            """);

        methods[3].ShouldBe(
            """
            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
            public static global::System.Collections.Generic.List<global::MapTo.Tests.DestinationRecord>? MapToDestinationRecords(this global::System.Collections.Generic.List<global::MapTo.Tests.SourceRecord>? source)
            {
                if (source is null)
                {
                    return null;
                }

                var target = new global::System.Collections.Generic.List<global::MapTo.Tests.DestinationRecord>(source.Count);
                for (var i = 0; i < source.Count; i++)
                {
                    target.Add(MapToDestinationRecord(source[i]));
                }

                return target;
            }
            """);
    }
}