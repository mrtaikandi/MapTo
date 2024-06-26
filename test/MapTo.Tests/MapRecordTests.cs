﻿namespace MapTo.Tests;

public class MapRecordTests
{
    private readonly ITestOutputHelper _output;

    public MapRecordTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void When_TargetRecord_Should_UseConstructorInitialization()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();

        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        sourceFile.AddClass(
            body: """
                  [MapFrom(typeof(SourceClass))]
                  public record TargetRecord(int Id, string Name);
                  """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation
            .GetClassDeclaration("SourceClassMapToExtensions")
            .ShouldContain("return new TargetRecord(sourceClass.Id, sourceClass.Name);");
    }

    [Fact]
    public void When_TargetRecordPropertiesAreAllInitOnly_Should_UseObjectInitializer()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();

        sourceFile.AddClass(
            body: """
                  public record SourceRecord(int Id, string Name);

                  [MapFrom(typeof(SourceRecord))]
                  public record TargetRecord
                  {
                      public int Id { get; init; }
                      public string Name { get; init; }
                  }
                  """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation
            .GetClassDeclaration("SourceRecordMapToExtensions")
            .ShouldContain(
                """
                return new TargetRecord
                {
                    Id = sourceRecord.Id,
                    Name = sourceRecord.Name
                };
                """);
    }

    [Fact]
    public void When_TargetRecordPropertiesIsIgnored_Should_NotParticipateInMapping()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();

        sourceFile.AddClass(
            body: """
                  public record SourceRecord(int Id, string Name);

                  [MapFrom(typeof(SourceRecord))]
                  public record TargetRecord(int Id)
                  {
                      [property: IgnoreProperty]
                      public string Name { get; init; }
                  }
                  """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation
            .GetClassDeclaration("SourceRecordMapToExtensions")
            .ShouldContain(
                """
                [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("sourceRecord")]
                public static TargetRecord? MapToTargetRecord(this SourceRecord? sourceRecord)
                {
                    if (sourceRecord is null)
                    {
                        return null;
                    }

                    return new TargetRecord(sourceRecord.Id);
                }
                """);
    }

    [Fact]
    public void When_TargetRecordInitPropertiesIsIgnored_Should_NotParticipateInMapping()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();

        sourceFile.AddClass(
            body: """
                  public record SourceRecord(int Id, string Name);

                  [MapFrom(typeof(SourceRecord))]
                  public record TargetRecord
                  {
                        public int Id { get; init; }

                        [property: IgnoreProperty]
                        public string Name { get; init; }
                  }
                  """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation
            .GetClassDeclaration("SourceRecordMapToExtensions")
            .ShouldContain(
                """
                return new TargetRecord
                {
                    Id = sourceRecord.Id
                };
                """);
    }

    [Fact]
    public void When_TargetRecordHasSecondaryConstructor_Should_ChooseTheOneWithMostParameters()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile = builder.AddFile();

        sourceFile.AddClass(
            body: """
                  public record SourceRecord(int Id, string Name, string Description);

                  [MapFrom(typeof(SourceRecord))]
                  public record TargetRecord(int Id)
                  {
                      public TargetRecord(int id, string name) : this(id) => Name = name;

                      public string Name { get; }

                      public string Description { get; init; }
                  }
                  """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation
            .GetClassDeclaration("SourceRecordMapToExtensions")
            .ShouldContain(
                """
                return new TargetRecord(sourceRecord.Id, sourceRecord.Name)
                {
                    Description = sourceRecord.Description
                }
                """);
    }

    [Fact]
    public void When_SimpleRecords_Should_GenerateWithConstructorInitializer()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(analyzerConfigOptions: new Dictionary<string, string>
        {
            [nameof(CodeGeneratorOptions.ProjectionType)] = ProjectionType.None.ToString()
        }));

        builder.AddFile(supportNullableReferenceTypes: true)
            .WithBody(
                """
                public record Source(string FirstName, string LastName, string[] Tags);

                [MapFrom(typeof(Source))]
                public record Target(string FirstName, string LastName, string[] Tags);
                """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldBe(
            $$"""
              {{ScenarioBuilder.GeneratedCodeAttribute}}
              public static class SourceMapToExtensions
              {
                  [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                  public static Target? MapToTarget(this Source? source)
                  {
                      if (source is null)
                      {
                          return null;
                      }

                      return new Target(source.FirstName, source.LastName, source.Tags);
                  }
              }
              """);
    }

    [Fact]
    public void When_RecordsWithInitProperty_Should_GenerateWithObjectInitializer()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile(supportNullableReferenceTypes: true)
            .WithBody(
                """
                public record Source
                {
                    public string FirstName { get; init; } = null!;
                    public string LastName { get; init; } = null!;
                }

                [MapFrom(typeof(Source))]
                public record Target
                {
                    public string FirstName { get; init; } = null!;
                    public string LastName { get; init; } = null!;
                }
                """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain(
            """
            return new Target
            {
                FirstName = source.FirstName,
                LastName = source.LastName
            };
            """);
    }

    [Fact]
    public void When_RecordsWithConstructorAndInitProperty_Should_GenerateWithConstructorAndObjectInitializer()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile(supportNullableReferenceTypes: true)
            .WithBody(
                """
                public record Source(string FirstName)
                {
                    public string LastName { get; init; } = null!;
                }

                [MapFrom(typeof(Source))]
                public record Target(string LastName)
                {
                    public string FirstName { get; init; } = null!;
                }
                """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain(
            """
            return new Target(source.LastName)
            {
                FirstName = source.FirstName
            };
            """);
    }

    [Fact]
    public void When_RecordAndInheritance_Should_HandleSubtype()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile(supportNullableReferenceTypes: true)
            .WithBody(
                """
                public record SourceParent(string Name);
                public record SourceChild(string Name, string LastName) : SourceParent(Name);

                [MapFrom(typeof(SourceChild))]
                public record Target(
                    [property: MapProperty(From = nameof(SourceChild.Name))] string FirstName,
                    string LastName);
                """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceChildMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain("return new Target(sourceChild.Name, sourceChild.LastName);");
    }

    [Fact]
    public void When_RecordsWithConstructorAndDefaultValue_Should_GenerateWithConstructorAndIgnoreTheDefault()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile(supportNullableReferenceTypes: true)
            .WithBody(
                """
                public record Source(string FirstName)
                {
                    public string LastName { get; init; } = null!;
                }

                [MapFrom(typeof(Source))]
                public record Target(string LastName, int Level = 1)
                {
                    public string FirstName { get; init; } = null!;
                }
                """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain(
            """
            return new Target(LastName: source.LastName)
            {
                FirstName = source.FirstName
            };
            """);
    }

    [Fact]
    public void When_RecordsWithConstructorAndMultipleDefaultValues_Should_GenerateWithConstructorAndIgnoreTheDefault()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile(supportNullableReferenceTypes: true)
            .WithBody(
                """
                public record Source(string FirstName)
                {
                    public string LastName { get; init; } = null!;
                }

                [MapFrom(typeof(Source))]
                public record Target(string LastName, string Level = "High", string? FirstName = "Random");
                """);

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: false);

        // Assert
        compilation.Dump(_output);
        compilation.ShouldBeSuccessful();
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain("return new Target(LastName: source.LastName, FirstName: source.FirstName);");
    }

    [Fact]
    public void When_RecordsWithInitRequiredProperty_Should_GenerateWithObjectInitializer()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile(supportNullableReferenceTypes: true)
            .WithBody(
                """
                public record Source
                {
                    public int Prop1 { get; init; }
                    public double Prop2 { get; init; }
                }

                [MapFrom(typeof(Source))]
                public record Target
                {
                    public required int Prop1 { get; init; }
                    public required double Prop2 { get; init; }
                }
                """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain(
            """
            return new Target
            {
                Prop1 = source.Prop1,
                Prop2 = source.Prop2
            };
            """);
    }

    [Fact]
    public void When_RecordsWithIgnoredRequiredProperty_Should_RequestValueFromExtensionArgs()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile(supportNullableReferenceTypes: true)
            .WithBody(
                """
                public record Source
                {
                    public int Prop1 { get; init; }
                    public double Prop2 { get; init; }
                }

                [MapFrom(typeof(Source))]
                public record Target
                {
                    public required int Prop1 { get; init; }

                    [IgnoreProperty]
                    public required double Prop2 { get; init; }
                }
                """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        compilation.Dump(_output);
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceMapToExtensions").ShouldNotBeNull();
        extensionClass.ShouldContain(
            """
            public static Target? MapToTarget(this Source? source, double prop2)
            {
                if (source is null)
                {
                    return null;
                }

                return new Target
                {
                    Prop1 = source.Prop1,
                    Prop2 = prop2
                };
            }
            """);
    }

    [Fact]
    public void When_SourceRecordDoesNotHaveIgnoredRequiredProperty_Should_RequestValueFromExtensionArgs()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile(supportNullableReferenceTypes: true)
            .WithBody(
                """
                public record Source
                {
                    public int Prop1 { get; init; }
                    public double Prop2 { get; init; }
                }

                [MapFrom(typeof(Source))]
                public record Target
                {
                    public required int Prop1 { get; init; }

                    [IgnoreProperty]
                    public required double Prop3 { get; init; }
                }
                """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        compilation.Dump(_output);
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceMapToExtensions").ShouldNotBeNull();
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

    [Fact]
    public void When_SourceRecordDoesNotHaveRequiredProperty_Should_RequestValueFromExtensionArgs()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        builder.AddFile(supportNullableReferenceTypes: true)
            .WithBody(
                """
                public record Source
                {
                    public int Prop1 { get; init; }
                    public double Prop2 { get; init; }
                }

                [MapFrom(typeof(Source))]
                public record Target
                {
                    public required int Prop1 { get; init; }
                    public required double Prop3 { get; init; }
                }
                """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        compilation.Dump(_output);
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceMapToExtensions").ShouldNotBeNull();
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