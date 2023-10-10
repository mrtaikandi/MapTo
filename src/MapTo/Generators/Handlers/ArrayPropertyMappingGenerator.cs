using MapTo.Mappings;
using static MapTo.NullHandling;
using static Microsoft.CodeAnalysis.NullableAnnotation;

namespace MapTo.Generators.Handlers;

internal sealed class ArrayPropertyMappingGenerator : PropertyMappingGenerator
{
    /// <inheritdoc />
    protected override bool HandleCore(PropertyGeneratorContext context)
    {
        var (writer, property, parameterName, targetInstanceName, copyPrimitiveArrays, refHandler) = context;
        if (property is { TypeConverter: not { Type.EnumerableType: EnumerableType.Array } } || targetInstanceName is null)
        {
            return false;
        }

        parameterName = $"{parameterName}.{property.SourceName}";

        if (property.SourceType.IsArray)
        {
            MapArray(writer, property, parameterName, targetInstanceName, copyPrimitiveArrays, refHandler);
        }
        else
        {
            SelectArray(writer, property, parameterName, targetInstanceName, refHandler);
        }

        return true;
    }

    private static void MapArray(CodeWriter writer, PropertyMapping property, string parameterName, string targetInstanceName, bool copyPrimitiveArrays, string? refHandler)
    {
        switch (property)
        {
            case { NullHandling: ThrowException, SourceType.IsNullable: true, TypeConverter.IsMapToExtensionMethod: false } when !copyPrimitiveArrays:
                writer.Write($"{targetInstanceName}.{property.Name} = ")
                    .Wrap(Map(writer, property, parameterName, copyPrimitiveArrays, refHandler))
                    .Write(" ?? ").WriteThrowArgumentNullException(parameterName).WriteLine(";");

                break;

            case { NullHandling: ThrowException }:
                writer.Write($"{targetInstanceName}.{property.Name} = ")
                    .WriteIsNullCheck(parameterName).Write(" ? ").WriteThrowArgumentNullException(parameterName)
                    .Write(" : ").Wrap(Map(writer, property, parameterName, copyPrimitiveArrays, refHandler)).WriteLine(";");

                break;

            case { NullHandling: SetEmptyCollection, SourceType.IsNullable: true, TypeConverter.IsMapToExtensionMethod: false } when !copyPrimitiveArrays:
                writer.Write($"{targetInstanceName}.{property.Name} = ")
                    .Wrap(Map(writer, property, parameterName, copyPrimitiveArrays, refHandler))
                    .Write(" ?? ").Write(property.TypeConverter.Type.EmptySourceCodeString()).WriteLine(";");

                break;

            case { NullHandling: SetEmptyCollection }:
                writer.Write($"{targetInstanceName}.{property.Name} = ")
                    .WriteIsNullCheck(parameterName).Write(" ? ").Write(property.TypeConverter.Type.EmptySourceCodeString())
                    .Write(" : ").Wrap(Map(writer, property, parameterName, copyPrimitiveArrays, refHandler)).WriteLine(";");

                break;

            case { NullHandling: SetNull, SourceType.IsNullable: true }:
                writer.Write("if (").WriteIsNotNullCheck(parameterName).WriteLine(")")
                    .WriteOpeningBracket()
                    .Write($"{targetInstanceName}.{property.Name} = ")
                    .Wrap(Map(writer, property, parameterName, copyPrimitiveArrays, refHandler))
                    .WriteLine(";")
                    .WriteClosingBracket();

                break;

            case { NullHandling: Auto, SourceType.NullableAnnotation: not NotAnnotated, TypeConverter.Type.NullableAnnotation: NotAnnotated } when !copyPrimitiveArrays:
                writer.Write($"{targetInstanceName}.{property.Name} = ")
                    .Wrap(Map(writer, property, parameterName, copyPrimitiveArrays, refHandler))
                    .Write(" ?? ").Write(property.TypeConverter.Type.EmptySourceCodeString()).WriteLine(";");

                break;

            case { NullHandling: Auto, SourceType.NullableAnnotation: not NotAnnotated, TypeConverter.Type.NullableAnnotation: NotAnnotated } when copyPrimitiveArrays:
                writer.Write($"{targetInstanceName}.{property.Name} = ")
                    .WriteIsNullCheck(parameterName).Write(" ? ").Write(property.TypeConverter.Type.EmptySourceCodeString())
                    .Write(" : ").Wrap(Map(writer, property, parameterName, copyPrimitiveArrays, refHandler)).WriteLine(";");

                break;

            case { NullHandling: Auto, SourceType.NullableAnnotation: not NotAnnotated, TypeConverter.Type.NullableAnnotation: Annotated }:
                writer.Write("if (").WriteIsNotNullCheck(parameterName).WriteLine(")")
                    .WriteOpeningBracket()
                    .Write($"{targetInstanceName}.{property.Name} = ")
                    .Wrap(Map(writer, property, parameterName, copyPrimitiveArrays, refHandler))
                    .WriteLine(";")
                    .WriteClosingBracket();

                break;

            default:
                writer.Write($"{targetInstanceName}.{property.Name} = ").Wrap(Map(writer, property, parameterName, copyPrimitiveArrays, refHandler)).WriteLine(";");
                break;
        }

        return;

        static CodeWriter Map(CodeWriter writer, PropertyMapping property, string parameterName, bool copyPrimitiveArrays, string? refHandler)
        {
            return property.TypeConverter switch
            {
                { IsMapToExtensionMethod: false } when !copyPrimitiveArrays => writer.Write(parameterName),
                { IsMapToExtensionMethod: true, ReferenceHandling: true } => writer
                    .Write(property.TypeConverter.MethodName).Write("Array").Write("(")
                    .Write(parameterName).Write(", ").Write(refHandler).Write(")"),
                _ => writer.Write(property.TypeConverter.MethodName).Write("Array").Write("(").Write(parameterName).Write(")")
            };
        }
    }

    private static void SelectArray(CodeWriter writer, PropertyMapping property, string parameterName, string targetInstanceName, string? refHandler)
    {
        switch (property.NullHandling)
        {
            case ThrowException when property.SourceType.IsNullable:
                writer.Write($"{targetInstanceName}.{property.Name} = ")
                    .Wrap(Map(writer, property, parameterName, refHandler))
                    .Write(" ?? ").WriteThrowArgumentNullException(parameterName);

                break;

            case SetEmptyCollection when property.SourceType.IsNullable:
                writer.Write($"{targetInstanceName}.{property.Name} = ")
                    .Wrap(Map(writer, property, parameterName, refHandler))
                    .Write(" ?? ").Write(property.TypeConverter.Type.EmptySourceCodeString());

                break;

            case SetNull when property.SourceType.IsNullable:
                writer.Write("if (").WriteIsNotNullCheck(parameterName).WriteLine(")")
                    .WriteOpeningBracket()
                    .Write($"{targetInstanceName}.{property.Name} = ")
                    .Wrap(Map(writer, property, parameterName, refHandler))
                    .WriteLine(";")
                    .WriteClosingBracket();

                break;

            case Auto:
            default:
                writer.Write($"{targetInstanceName}.{property.Name} = ").Wrap(Map(writer, property, parameterName, refHandler)).Write(";");
                break;
        }

        return;

        static CodeWriter Map(CodeWriter writer, PropertyMapping property, string parameterName, string? refHandler)
        {
            var p = property.ParameterName[0];
            var converterMethod = property.TypeConverter.MethodFullName;
            var linqMethod = property.TypeConverter.Type.EnumerableType.ToLinqSourceCodeString();
            var parameter = property switch
            {
                { NullHandling: SetNull } => parameterName,
                { NullHandling: Auto, SourceType.NullableAnnotation: NotAnnotated } => parameterName,
                { SourceType.IsNullable: true } => $"{parameterName}?",
                _ => parameterName
            };

            return property.TypeConverter switch
            {
                { HasParameter: true } => writer
                    .Write(parameter).Write(".Select(").Write(p).Write(" => ")
                    .Write(converterMethod).Write("(").Write(p).Write(")).").Write(linqMethod),
                { ReferenceHandling: true } => writer
                    .Write(parameter).Write(".Select(").Write(p).Write(" => ")
                    .Write(converterMethod).Write("(").Write(p).Write(", ").Write(refHandler).Write(")).").Write(linqMethod),
                _ => writer.Write(parameter).Write(".Select(").Write(converterMethod).Write(").").Write(linqMethod)
            };
        }
    }
}