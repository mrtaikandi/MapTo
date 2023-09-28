using MapTo.Extensions;
using MapTo.Mappings;

namespace MapTo.Generators;

internal readonly record struct PartialClassGenerator(TargetMapping Mapping) : ICodeGenerator
{
    /// <inheritdoc />
    public void BeginWrite(CodeWriter writer)
    {
        if (!Mapping.IsPartial)
        {
            return;
        }

        writer
            .WriteGeneratedCodeAttribute()
            .WritePartialClassDefinition(Mapping)
            .WriteOpeningBracket() // Class opening bracket
            .WriteConstructor(Mapping.Constructor)
            .WriteClosingBracket() // Class closing bracket
            .WriteLine();
    }
}

internal static class PartialClassGeneratorExtensions
{
    internal static CodeWriter WriteConstructor(this CodeWriter writer, ConstructorMapping constructor)
    {
        if (!constructor.IsGenerated)
        {
            return writer;
        }

        return writer
            .Write($"public {constructor.Name}(")
            .WriteJoin(", ", constructor.Parameters.Select(a => $"{a.Type.FullName} {a.Name}"))
            .WriteClosingParenthesis()
            .WriteOpeningBracket()
            .WriteLines(constructor.Parameters.Select(a => $"{a.Property.Name} = {a.Name};"))
            .WriteClosingBracket();
    }

    internal static CodeWriter WritePartialClassDefinition(this CodeWriter writer, TargetMapping mapping) => writer
        .WriteLine($"{mapping.Modifier.ToLowercaseString()} partial class {mapping.Name}");
}