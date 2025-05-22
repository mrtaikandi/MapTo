using MapTo.Mappings;
using static MapTo.NullHandling;
using static Microsoft.CodeAnalysis.NullableAnnotation;

namespace MapTo.Generators.Properties;

internal sealed class TypeConverterPropertyMappingGenerator : PropertyMappingGenerator
{
    /// <inheritdoc />
    protected override bool HandleCore(PropertyGeneratorContext context)
    {
        var (writer, property, parameterName, targetInstanceName, _, referenceHandlerName) = context;
        if (!property.HasTypeConverter)
        {
            return false;
        }

        parameterName = $"{parameterName}.{property.SourceName}";

        switch (property)
        {
            case var _ when targetInstanceName is null:
                Map(writer, property, parameterName, referenceHandlerName);
                break;

            case { NullHandling: NullHandling.ThrowException }:
                writer.Write(targetInstanceName).Write(".").Write(property.Name).Write(" = ").WriteIsNullCheck(parameterName)
                    .Write("?").WriteThrowArgumentNullException(parameterName)
                    .Write(":").Wrap(Map(writer, property, parameterName, referenceHandlerName)).WriteLine(";");

                break;

            case { NullHandling: NullHandling.SetNull }:
            case { NullHandling: NullHandling.Auto, SourceType.NullableAnnotation: not NullableAnnotation.NotAnnotated }:
                writer.Write("if (").WriteIsNotNullCheck(parameterName).WriteLine(")")
                    .WriteOpeningBracket()
                    .Write(targetInstanceName).Write(".").Write(property.Name).Write(" = ").Wrap(Map(writer, property, parameterName, referenceHandlerName))
                    .WriteLine(";")
                    .WriteClosingBracket();

                break;

            case { TypeConverter: { IsMapToExtensionMethod: false, EnumMapping: { IsNull: false, Strategy: EnumMappingStrategy.ByValue } } }:
                writer.Write(targetInstanceName).Write(".").Write(property.Name).Write(" = (").Write(property.TypeName).Write(")").Write(parameterName).WriteLine(";");
                break;

            default:
                writer.Write(targetInstanceName).Write(".").Write(property.Name).Write(" = ").Wrap(Map(writer, property, parameterName, referenceHandlerName)).WriteLine(";");
                break;
        }

        return true;
    }

    private static CodeWriter Map(CodeWriter writer, PropertyMapping property, string parameterName, string? referenceHandlerName)
    {
        var typeConverter = property.TypeConverter;
        var isPrimitive = property.SourceType.ElementTypeIsPrimitive && property.Type.ElementTypeIsPrimitive;
        var parameter = property switch
        {
            { NullHandling: SetNull } => parameterName,
            { NullHandling: Auto, SourceType.NullableAnnotation: NotAnnotated } => parameterName,
            { SourceType.IsNullable: true } => $"{parameterName}?",
            _ => parameterName
        };

        return typeConverter switch
        {
            { HasParameter: true } => writer
                .Write(typeConverter.MethodFullName).Write("(").Write(parameterName).Write(", ").Write(typeConverter.Parameter).Write(")"),
            { HasParameter: false, IsMapToExtensionMethod: false, EnumMapping: { IsNull: false, Strategy: EnumMappingStrategy.ByValue } } => writer
                .Write("(").Write(property.TypeName).Write(")").Write(parameterName),
            { IsMapToExtensionMethod: false, Explicit: false } when isPrimitive => writer.Write(parameter).Write(".").Write(property.TypeConverter.Type.EnumerableType.ToLinqSourceCodeString()),
            { HasParameter: false, ReferenceHandling: false } => writer
                .Write(typeConverter.MethodFullName).Write("(").Write(parameterName).Write(")"),
            { HasParameter: false, ReferenceHandling: true } => writer
                .Write(typeConverter.MethodFullName).Write("(").Write(parameterName).Write(", ").Write(referenceHandlerName).Write(")")
        };
    }
}