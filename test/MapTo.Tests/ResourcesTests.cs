namespace MapTo.Tests;

public class ResourcesTests
{
    private const string EmptySource = "";

    [Theory]
    [InlineData($@"src\MapTo\Resources\{WellKnownTypes.IgnorePropertyAttributeName}.cs")]
    [InlineData($@"src\MapTo\Resources\{WellKnownTypes.MapConstructorAttributeName}.cs")]
    [InlineData($@"src\MapTo\Resources\{WellKnownTypes.MapFromAttributeName}.cs")]
    [InlineData($@"src\MapTo\Resources\{WellKnownTypes.MapPropertyAttributeName}.cs")]
    [InlineData($@"src\MapTo\Resources\{WellKnownTypes.PropertyTypeConverterAttributeName}.cs")]
    public void VerifyAttributesAreEmitted(string path)
    {
        // Arrange
        var currentDirectory = Environment.CurrentDirectory;
        var absolutePath = Path.Combine(currentDirectory[..currentDirectory.IndexOf($"test{Path.DirectorySeparatorChar}", StringComparison.Ordinal)], path);
        var expected = File.ReadAllText(absolutePath);
        var name = $"{Path.GetFileNameWithoutExtension(path)}.g.cs";

        // Act
        var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(EmptySource, analyzerConfigOptions: AnalyzerOptions.DisableXmlDoc);

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.ShouldContainSourceFile(name, expected);
    }
}