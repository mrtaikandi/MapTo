namespace MapTo.Tests;

public class CodeGeneratorOptionsTests
{
    [Theory]
    [InlineData("GeneratedMethodsAccessModifier", "Public", AccessModifier.Public)]
    [InlineData("GeneratedMethodsAccessModifier", "Internal", AccessModifier.Internal)]
    [InlineData("GeneratedMethodsAccessModifier", "Private", AccessModifier.Private)]
    [InlineData("MapMethodPrefix", "To", "To")]
    [InlineData("MapExtensionClassSuffix", "GeneratedExtension", "GeneratedExtension")]
    [InlineData("UseReferenceHandling", "true", true)]
    [InlineData("UseReferenceHandling", "false", false)]
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