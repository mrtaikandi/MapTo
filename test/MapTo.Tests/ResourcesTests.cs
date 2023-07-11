namespace MapTo.Tests;

public class ResourcesTests
{
    private const string EmptySource = "";

    [Theory]
    [InlineData(@"src\MapTo\Resources\MapFromAttribute.cs")]
    [InlineData(@"src\MapTo\Resources\IgnorePropertyAttribute.cs")]
    public void VerifyAttributesAreEmitted(string path)
    {
        // Arrange
        var currentDirectory = Environment.CurrentDirectory;
        var absolutePath = Path.Combine(currentDirectory[..currentDirectory.IndexOf("test\\", StringComparison.Ordinal)], path);
        var expected = File.ReadAllText(absolutePath);
        var name = Path.GetFileNameWithoutExtension(path);

        // Act
        var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(EmptySource, analyzerConfigOptions: AnalyzerOptions.DisableXmlDoc);

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.SyntaxTrees.ShouldContainSource(name, expected);
    }
}