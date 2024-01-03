using MapTo.Extensions;
using MapTo.Mappings;

namespace MapTo.Generators;

internal static class TypeInitializerGenerator
{
    internal static CodeWriter WriteConstructorInitializer(this CodeWriter writer, TargetMapping mapping) =>
        WriteConstructorInitializer(writer, mapping.ToTypeInitializerMapping());

    internal static CodeWriter WriteConstructorInitializer(this CodeWriter writer, TypeInitializerMapping mapping, bool inline = false)
    {
        var (sourceName, targetName, targetConstructor, targetProperties, options) = mapping;
        var sourceType = sourceName;
        var parameterName = sourceType.ToParameterNameCasing();
        var hasObjectInitializer = targetProperties.Any(p => p.InitializationMode == PropertyInitializationMode.ObjectInitializer);

        if (!targetConstructor.HasParameters)
        {
            writer.Write($"new {targetName}");
            return hasObjectInitializer ? writer.WriteNewLine() : writer.WriteLine("();");
        }

        writer.Write($"new {targetName}(");

        for (var i = 0; i < targetConstructor.Parameters.Length; i++)
        {
            var parameter = targetConstructor.Parameters[i];

            if (targetConstructor.HasParameterWithDefaultValue)
            {
                writer.Write(parameter.Name).Write(": ");
            }

            PropertyGenerator.Instance.Generate(new(writer, parameter.Property, parameterName, null, options.CopyPrimitiveArrays, null));
            writer.WriteIf(i < targetConstructor.Parameters.Length - 1, ", ");
        }

        return (inline, hasObjectInitializer) switch
        {
            (_, true) => writer.WriteLine(")"),
            (true, false) => writer.Write(")"),
            (false, false) => writer.WriteLine(");")
        };
    }

    internal static CodeWriter WriteObjectInitializer(this CodeWriter writer, TargetMapping mapping) =>
        WriteObjectInitializer(writer, mapping.ToTypeInitializerMapping());

    internal static CodeWriter WriteObjectInitializer(this CodeWriter writer, TypeInitializerMapping mapping, bool inline = false)
    {
        var (sourceName, _, _, targetProperties, options) = mapping;
        var properties = targetProperties.Where(p => p.InitializationMode == PropertyInitializationMode.ObjectInitializer).ToArray();
        if (properties.Length == 0)
        {
            return writer;
        }

        var sourceType = sourceName;
        var parameterName = sourceType.ToParameterNameCasing();

        writer.WriteOpeningBracket();

        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];

            writer.Write($"{property.Name} = ");
            PropertyGenerator.Instance.Generate(new(writer, property, parameterName, null, options.CopyPrimitiveArrays, null));
            writer.WriteIf(i < properties.Length - 1, ",").WriteLineIndented();
        }

        return writer.WriteClosingBracket(false).WriteLineIf(!inline, ";");
    }
}