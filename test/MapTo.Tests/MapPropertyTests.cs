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
        compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
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
        compilation.GetClassDeclaration("SourceClassToTargetClassMapToExtensions").ShouldContain(
            """
            return new TargetClass
            {
                Id = sourceClass.Id,
                FullName = sourceClass.Name
            };
            """);
    }

    [Fact]
    public void When_PropertyTypeIsMappedInAnotherNamespace_Should_MapToFullyQualifiedExtensionName()
    {
        // Arrange
        var builder = new TestSourceBuilder();
        var sourceFile1 = builder.AddFile(ns: "Node.System");
        sourceFile1.AddClass(Accessibility.Public, "MemoryStatus").WithProperty<int>("Id");
        sourceFile1.AddClass(Accessibility.Public, "SystemInfo").WithProperty("MemoryStatus", "MemoryStatus");

        var sourceFile2 = builder.AddFile(ns: "Contract.System");
        sourceFile2.AddClass(Accessibility.Public, "MemoryStatus").WithProperty<int>("Id");
        sourceFile2.AddClass(Accessibility.Public, "SystemInfo").WithProperty("MemoryStatus", "MemoryStatus");

        builder.AddEmptyFile().WithBody(
            """
            using MapTo;
            using N = Node.System;
            using C = Contract.System;

            [assembly: Map<N.MemoryStatus, C.MemoryStatus>]
            [assembly: Map<N.SystemInfo, C.SystemInfo>]
            """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        compilation.Dump(_output);
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SystemInfoToSystemInfoMapToExtensions").ShouldNotBeNull().ShouldContain(
            """
            public static global::Contract.System.SystemInfo? MapToSystemInfo(this global::Node.System.SystemInfo? systemInfo)
            {
                if (systemInfo is null)
                {
                    return null;
                }

                var target = new SystemInfo();

                if (systemInfo.MemoryStatus is not null)
                {
                    target.MemoryStatus = global::Contract.System.MemoryStatusToMemoryStatusMapToExtensions.MapToMemoryStatus(systemInfo.MemoryStatus);
                }

                return target;
            }
            """);
    }

    [Fact]
    public void When_TypeConverterIsUsedWithConstructorParameters_Should_GenerateTypeConverterMethod()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(LanguageVersion.Latest, supportNullReferenceTypes: true));
        builder.AddFile(supportNullableReferenceTypes: true, ns: "Source", usings: ["System"]).WithBody(
            """
            public class Node
            {
                public int Id { get; set; }
                public string[] Processors { get; set; } = Array.Empty<string>();
            }
            """);

        builder.AddFile(supportNullableReferenceTypes: true, ns: "Contract").WithBody(
            """
            public record Node(int Id, string Processor);
            """);

        builder.AddEmptyFile().WithBody(
            """
            using MapTo;
            using System.Linq;

            [Map<Source.Node, Contract.Node>(nameof(SystemInfoMappings))]
            file static class Mappings
            {
                internal static void SystemInfoMappings(MappingConfiguration<Source.Node, Contract.Node> map)
                {
                    map.ForProperty(p => p.Processor).MapTo(p => p.Processors).UseTypeConverter<string[]>(p => p.First());
                }
            }
            """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        compilation.Dump(_output);
        diagnostics.ShouldBeSuccessful();
    }
}