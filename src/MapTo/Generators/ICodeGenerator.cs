namespace MapTo.Generators;

internal interface ICodeGenerator
{
    void BeginWrite(CodeWriter writer);
}

internal interface ICodeBlockGenerator : ICodeGenerator
{
    void EndWrite(CodeWriter writer);
}