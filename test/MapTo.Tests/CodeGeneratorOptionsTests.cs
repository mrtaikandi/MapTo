namespace MapTo.Tests;

public class CodeGeneratorOptionsTests
{
    [Theory]
    [InlineData(nameof(CodeGeneratorOptions.MapMethodPrefix), "To", "To")]
    [InlineData(nameof(CodeGeneratorOptions.MapExtensionClassSuffix), "GeneratedExtension", "GeneratedExtension")]
    [InlineData(nameof(CodeGeneratorOptions.ReferenceHandling), "Enabled", ReferenceHandling.Enabled)]
    [InlineData(nameof(CodeGeneratorOptions.ReferenceHandling), "Disabled", ReferenceHandling.Disabled)]
    [InlineData(nameof(CodeGeneratorOptions.ReferenceHandling), "Auto", ReferenceHandling.Auto)]
    [InlineData(nameof(CodeGeneratorOptions.CopyPrimitiveArrays), "true", true)]
    [InlineData(nameof(CodeGeneratorOptions.CopyPrimitiveArrays), "false", false)]
    [InlineData(nameof(CodeGeneratorOptions.ProjectionType), "IEnumerable", ProjectionType.IEnumerable)]
    [InlineData(nameof(CodeGeneratorOptions.ProjectionType), "Array | IEnumerable", ProjectionType.Array | ProjectionType.IEnumerable)]
    [InlineData(nameof(CodeGeneratorOptions.ProjectionType), "3", ProjectionType.Array | ProjectionType.IEnumerable)]
    public void Verify_AnalyzerConfigOption(string configName, string value, object expectedValue)
    {
        // Arrange
        var configOptionsProvider = new TestAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            [$"build_property.MapTo_{configName}"] = value
        });

        // Act
        var options = CodeGeneratorOptions.Create(configOptionsProvider, CancellationToken.None);

        // Assert
        options.GetType().GetProperty(configName).ShouldNotBeNull().GetValue(options).ShouldBe(expectedValue);
    }
}