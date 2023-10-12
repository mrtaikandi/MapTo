namespace MapTo.Tests;

public class FlatteningTests
{
    private readonly ITestOutputHelper _output;

    public FlatteningTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void When_MappingToDifferentProperty_Should_HandleNestedProperty()
    {
        // Arrange
        var builder = ScenarioBuilder.ProductOrderAndOrderLineItem();
        var file = builder.AddFile("FlatteningTest", supportNullableReferenceTypes: true);
        file.AddClass(Accessibility.Public, "OrderDto", attributes: new[] { "[MapFrom(typeof(Order))]" })
            .WithProperty("string?", "CustomerName", attributes: new[] { """[MapProperty(From = "Customer.Name")]""" });

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var extensionClass = compilation.GetClassDeclaration("OrderMapToExtensions", "MapTo.Tests.OrderDto.g.cs").ShouldNotBeNull();
        extensionClass.ShouldContain(
            """
            return new OrderDto
            {
                CustomerName = order.Customer?.Name
            };
            """);
    }

    [Theory]
    [InlineData("[MapProperty(From = nameof(Order.Customer.Name))]")]
    [InlineData("""[MapProperty(From = "Customer.Name")]""")]
    public void When_MappingToDifferentNotNullProperty_Should_HandleNestedProperty(string mapPropertyAttribute)
    {
        // Arrange
        var builder = ScenarioBuilder.ProductOrderAndOrderLineItem();
        var file = builder.AddFile("FlatteningTest", supportNullableReferenceTypes: true);
        file.AddClass(Accessibility.Public, "OrderDto", attributes: new[] { "[MapFrom(typeof(Order))]" })
            .WithProperty<string>("CustomerName", attribute: mapPropertyAttribute, defaultValue: "null!");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var extensionClass = compilation.GetClassDeclaration("OrderMapToExtensions", "MapTo.Tests.OrderDto.g.cs").ShouldNotBeNull();
        extensionClass.ShouldContain(
            """
            return new OrderDto
            {
                CustomerName = order.Customer?.Name ?? string.Empty
            };
            """);
    }

    [Theory]
    [InlineData("""[MapProperty(From = "GetTotal")]""")]
    [InlineData("""[MapProperty(From = "Order.GetTotal")]""")]
    [InlineData("[MapProperty(From = nameof(Order.GetTotal))]")]
    public void When_MappingToMethodName_Should_AllowParameterLessMethodMembers(string mapPropertyAttribute)
    {
        // Arrange
        var builder = ScenarioBuilder.ProductOrderAndOrderLineItem();
        var file = builder.AddFile("FlatteningTest", supportNullableReferenceTypes: true);
        file.AddClass(Accessibility.Public, "OrderDto", attributes: new[] { "[MapFrom(typeof(Order))]" })
            .WithProperty<decimal>("Total", attribute: mapPropertyAttribute);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        compilation.Dump(_output);
        diagnostics.ShouldBeSuccessful();

        var extensionClass = compilation.GetClassDeclaration("OrderMapToExtensions", "MapTo.Tests.OrderDto.g.cs").ShouldNotBeNull();
        extensionClass.ShouldContain("target.Total = order.GetTotal();");
    }
}