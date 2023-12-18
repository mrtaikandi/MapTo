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
        compilation.GetClassDeclaration("SourceRecordMapToExtensions")
            .ShouldBe(
                $$"""
                  {{ScenarioBuilder.GeneratedCodeAttribute}}
                  public static class SourceRecordMapToExtensions
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
        compilation.GetClassDeclaration("SourceRecordMapToExtensions")
            .ShouldContain(
                """
                [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                public static DestinationRecord[]? MapToDestinationRecords(this SourceRecord[]? source)
                {
                    if (source is null)
                    {
                        return null;
                    }
                
                    var target = new DestinationRecord[source.Length];
                    for (var i = 0; i < target.Length; i++)
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
        var builder = ScenarioBuilder.SimpleMappedRecordInSameNamespace(mapFromProjectionType: ProjectionType.Enumerable);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceRecordMapToExtensions")
            .ShouldContain(
                """
                [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                public static global::System.Collections.Generic.IEnumerable<DestinationRecord>? MapToDestinationRecords(this global::System.Collections.Generic.IEnumerable<SourceRecord>? source)
                {
                    return source is null ? null : global::System.Linq.Enumerable.Select(source, x => MapToDestinationRecord(x));
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
        compilation.GetClassDeclaration("SourceRecordMapToExtensions")
            .ShouldContain(
                """
                [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                public static global::System.Collections.Generic.ICollection<DestinationRecord>? MapToDestinationRecords(this global::System.Collections.Generic.ICollection<SourceRecord>? source)
                {
                    if (source is null)
                    {
                        return null;
                    }
                    
                    var target = new global::System.Collections.Generic.List<DestinationRecord>(source.Count);
                    foreach (var item in source)
                    {
                        target.Add(MapToDestinationRecord(item));
                    }
                    
                    return target;
                }
                """);
    }

    [Fact]
    public void When_ProjectionIsCollection_Should_GenerateProjectionExtensionMethods()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleMappedRecordInSameNamespace(mapFromProjectionType: ProjectionType.Collection);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceRecordMapToExtensions")
            .ShouldContain(
                """
                [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                public static global::System.Collections.ObjectModel.Collection<DestinationRecord>? MapToDestinationRecords(this global::System.Collections.ObjectModel.Collection<SourceRecord>? source)
                {
                    if (source is null)
                    {
                        return null;
                    }
                    
                    var target = new global::System.Collections.Generic.List<DestinationRecord>(source.Count);
                    foreach (var item in source)
                    {
                        target.Add(MapToDestinationRecord(item));
                    }
                    
                    return new global::System.Collections.ObjectModel.Collection<DestinationRecord>(target);
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
        compilation.GetClassDeclaration("SourceRecordMapToExtensions")
            .ShouldContain(
                """
                [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                public static global::System.Collections.Generic.IReadOnlyCollection<DestinationRecord>? MapToDestinationRecords(this global::System.Collections.Generic.IReadOnlyCollection<SourceRecord>? source)
                {
                    if (source is null)
                    {
                        return null;
                    }
                    
                    var target = new global::System.Collections.Generic.List<DestinationRecord>(source.Count);
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
        compilation.GetClassDeclaration("SourceRecordMapToExtensions")
            .ShouldContain(
                """
                [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                public static global::System.Collections.Generic.IList<DestinationRecord>? MapToDestinationRecords(this global::System.Collections.Generic.IList<SourceRecord>? source)
                {
                    if (source is null)
                    {
                        return null;
                    }
                    
                    var target = new global::System.Collections.Generic.List<DestinationRecord>(source.Count);
                    foreach (var item in source)
                    {
                        target.Add(MapToDestinationRecord(item));
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
        compilation.GetClassDeclaration("SourceRecordMapToExtensions")
            .ShouldContain(
                """
                [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                public static global::System.Collections.Generic.IReadOnlyList<DestinationRecord>? MapToDestinationRecords(this global::System.Collections.Generic.IReadOnlyList<SourceRecord>? source)
                {
                    if (source is null)
                    {
                        return null;
                    }
                    
                    var target = new global::System.Collections.Generic.List<DestinationRecord>(source.Count);
                    foreach (var item in source)
                    {
                        target.Add(MapToDestinationRecord(item));
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
        compilation.GetClassDeclaration("SourceRecordMapToExtensions")
            .ShouldContain(
                """
                [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                public static global::System.Collections.Generic.List<DestinationRecord>? MapToDestinationRecords(this global::System.Collections.Generic.List<SourceRecord>? source)
                {
                    if (source is null)
                    {
                        return null;
                    }
                    
                    var target = new global::System.Collections.Generic.List<DestinationRecord>(source.Count);
                    foreach (var item in source)
                    {
                        target.Add(MapToDestinationRecord(item));
                    }
                    
                    return target;
                }
                """);
    }

    [Fact]
    public void When_ProjectionIsSpan_Should_GenerateProjectionExtensionMethods()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleMappedRecordInSameNamespace(mapFromProjectionType: ProjectionType.Span);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceRecordMapToExtensions")
            .ShouldContain(
                """
                public static global::System.Span<DestinationRecord> MapToDestinationRecords(this global::System.Span<SourceRecord> source)
                {
                    var target = new DestinationRecord[source.Length];
                    for (var i = 0; i < target.Length; i++)
                    {
                        target[i] = MapToDestinationRecord(source[i]);
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
        compilation.GetClassDeclaration("SourceRecordMapToExtensions")
            .ShouldContain(
                """
                [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                public static global::System.Memory<DestinationRecord>? MapToDestinationRecords(this global::System.Memory<SourceRecord>? source)
                {
                    if (source is null)
                    {
                        return null;
                    }
                
                    var sourceSpan = source.Value.Span;
                    var target = new DestinationRecord[sourceSpan.Length];
                
                    for (var i = 0; i < target.Length; i++)
                    {
                        target[i] = MapToDestinationRecord(sourceSpan[i]);
                    }
                
                    return target;
                }
                """);
    }

    [Fact]
    public void When_ProjectionIsReadOnlySpan_Should_GenerateProjectionExtensionMethods()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleMappedRecordInSameNamespace(mapFromProjectionType: ProjectionType.ReadOnlySpan);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceRecordMapToExtensions")
            .ShouldContain(
                """
                public static global::System.ReadOnlySpan<DestinationRecord> MapToDestinationRecords(this global::System.ReadOnlySpan<SourceRecord> source)
                {
                    var target = new DestinationRecord[source.Length];
                    for (var i = 0; i < target.Length; i++)
                    {
                        target[i] = MapToDestinationRecord(source[i]);
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
        compilation.GetClassDeclaration("SourceRecordMapToExtensions")
            .ShouldContain(
                """
                [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                public static global::System.ReadOnlyMemory<DestinationRecord>? MapToDestinationRecords(this global::System.ReadOnlyMemory<SourceRecord>? source)
                {
                    if (source is null)
                    {
                        return null;
                    }
                
                    var sourceSpan = source.Value.Span;
                    var target = new DestinationRecord[sourceSpan.Length];
                
                    for (var i = 0; i < target.Length; i++)
                    {
                        target[i] = MapToDestinationRecord(sourceSpan[i]);
                    }
                
                    return target;
                }
                """);
    }
}