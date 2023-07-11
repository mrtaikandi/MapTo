namespace MapTo.Generators;

internal readonly record struct LineBreakGenerator : ICodeGenerator
{
    /// <inheritdoc />
    public void BeginWrite(CodeWriter writer)
    {
        writer.WriteLine();
    }
}