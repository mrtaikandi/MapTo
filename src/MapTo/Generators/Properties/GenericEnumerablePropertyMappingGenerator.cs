using MapTo.Mappings;
using static MapTo.NullHandling;
using static Microsoft.CodeAnalysis.NullableAnnotation;

namespace MapTo.Generators.Properties;

internal sealed class GenericEnumerablePropertyMappingGenerator : PropertyMappingGenerator
{
    /// <inheritdoc />
    protected override bool HandleCore(PropertyGeneratorContext context)
    {
        var (writer, property, parameter, targetInstanceName, _, refHandler) = context;
        if (property.TypeConverter is { Explicit: true } ||
            property.TypeConverter.Type is { EnumerableType: EnumerableType.Array or EnumerableType.None } ||
            targetInstanceName is null)
        {
            return false;
        }

        parameter = $"{parameter}.{property.SourceName}";
        var parameterName = property switch
        {
            { NullHandling: SetNull } => parameter,
            { NullHandling: Auto, SourceType.NullableAnnotation: NotAnnotated } => parameter,
            { SourceType.IsNullable: true } => $"{parameter}?",
            _ => parameter
        };

        switch (property)
        {
            case { NullHandling: ThrowException, SourceType.IsNullable: true }:
                writer.Write($"{targetInstanceName}.{property.Name} = ")
                    .Wrap(Map(writer, property, parameterName, refHandler))
                    .Write($" ?? throw new global::{KnownTypes.ArgumentNullException}(nameof({parameter}));");

                break;

            case { NullHandling: SetEmptyCollection, SourceType.IsNullable: true }:
                writer.Write($"{targetInstanceName}.{property.Name} = ")
                    .Wrap(Map(writer, property, parameterName, refHandler))
                    .Write($" ?? {property.TypeConverter.Type.EmptySourceCodeString()};");

                break;

            case { NullHandling: SetNull, SourceType.IsNullable: true }:
                writer.Write("if (").WriteIsNotNullCheck(parameter).WriteLine(")")
                    .WriteOpeningBracket()
                    .Write($"{targetInstanceName}.{property.Name} = ")
                    .Wrap(Map(writer, property, parameterName, refHandler))
                    .WriteLine(";")
                    .WriteClosingBracket();

                break;

            case { NullHandling: Auto, SourceType.NullableAnnotation: not NotAnnotated, TypeConverter.Type.NullableAnnotation: NotAnnotated }:
                writer.Write($"{targetInstanceName}.{property.Name} = ")
                    .Wrap(Map(writer, property, parameterName, refHandler))
                    .Write($" ?? {property.TypeConverter.Type.EmptySourceCodeString()};");

                break;

            default:
                writer.Write($"{targetInstanceName}.{property.Name} = ").Wrap(Map(writer, property, parameterName, refHandler)).WriteLine(";");
                break;
        }

        return true;
    }

    private static CodeWriter Map(CodeWriter writer, PropertyMapping property, string parameterName, string? refHandler)
    {
        var p = property.ParameterName[0];
        var converterMethod = property.TypeConverter.MethodFullName;
        var linqMethod = property.TypeConverter.Type.EnumerableType.ToLinqSourceCodeString();
        var isPrimitive = property.SourceType.ElementTypeIsPrimitive && property.Type.ElementTypeIsPrimitive;

        return property switch
        {
            { TypeConverter.HasParameter: true } =>
                writer.Write(parameterName).Write(".Select(").Write(p).Write(" => ").Write(converterMethod).Write("(").Write(p).Write(")).").Write(linqMethod),
            { TypeConverter.ReferenceHandling: true } =>
                writer.Write(parameterName).Write(".Select(").Write(p).Write(" => ").Write(converterMethod).Write("(").Write(p).Write(", ").Write(refHandler).Write(")).")
                    .Write(linqMethod),
            { TypeConverter: { IsMapToExtensionMethod: false, Explicit: false } } when isPrimitive => writer.Write(parameterName).Write(".").Write(linqMethod),
            _ => writer.Write(parameterName).Write($".Select<{property.SourceType.ElementTypeName}, {property.Type.ElementTypeName}>({converterMethod}).{linqMethod}"),
        };
    }
}