using MapTo.Generators;

namespace MapTo.Tests;

public class CodeWriterTests
{
    [Fact]
    public void WriteLineJoin_DoesNotWriteAnythingIfNoValuesAreAvailable()
    {
        // Arrange
        var writer = new CodeWriter();
        var values = Array.Empty<string?>();
        const string Separator = ", ";

        // Act
        writer.WriteLineJoin(Separator, values);

        // Assert
        var expectedOutput = writer.NewLine;
        Assert.Equal(expectedOutput, writer.ToString());
    }

    [Fact]
    public void WriteLineJoin_JoinsValuesWithSeparator()
    {
        // Arrange
        var writer = new CodeWriter();
        var values = new[] { "foo", null, "bar", "baz" };
        const string Separator = ",";

        // Act
        writer.WriteLineJoin(Separator, values);

        // Assert
        var expectedOutput = $"foo,{writer.NewLine},{writer.NewLine}bar,{writer.NewLine}baz{writer.NewLine}";
        Assert.Equal(expectedOutput, writer.ToString());
    }

    [Fact]
    public void WriteLineJoin_JoinsValuesWithSeparatorAndNoNulls()
    {
        // Arrange
        var writer = new CodeWriter();
        var values = new[] { "foo", "bar", "baz" };
        const string Separator = ",";

        // Act
        writer.WriteLineJoin(Separator, values);

        // Assert
        var expectedOutput = $"foo,{writer.NewLine}bar,{writer.NewLine}baz{writer.NewLine}";
        Assert.Equal(expectedOutput, writer.ToString());
    }

    [Fact]
    public void WriteLineJoin_OnlyWritesSingleValueIfOnlyOneIsAvailable()
    {
        // Arrange
        var writer = new CodeWriter();
        var values = new List<string?> { "foo" };
        const string Separator = ", ";

        // Act
        writer.WriteLineJoin(Separator, values);

        // Assert
        var expectedOutput = $"foo{writer.NewLine}";
        Assert.Equal(expectedOutput, writer.ToString());
    }
}