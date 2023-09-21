namespace MapTo.Tests;

public class ResourcesTests
{
    private const string EmptySource = "";

    [Theory]
    [InlineData($@"src\MapTo\Resources\{KnownTypes.IgnorePropertyAttributeName}.cs")]
    [InlineData($@"src\MapTo\Resources\{KnownTypes.MapConstructorAttributeName}.cs")]
    [InlineData($@"src\MapTo\Resources\{KnownTypes.MapFromAttributeName}.cs")]
    [InlineData($@"src\MapTo\Resources\{KnownTypes.MapPropertyAttributeName}.cs")]
    [InlineData($@"src\MapTo\Resources\{KnownTypes.PropertyTypeConverterAttributeName}.cs")]
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