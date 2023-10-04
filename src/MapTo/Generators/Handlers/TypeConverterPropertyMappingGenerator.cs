using MapTo.Mappings;

namespace MapTo.Generators.Handlers;

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

            default:
                writer.Write(targetInstanceName).Write(".").Write(property.Name).Write(" = ").Wrap(Map(writer, property, parameterName, referenceHandlerName)).WriteLine(";");
                break;
        }

        return true;
    }

    private static CodeWriter Map(CodeWriter writer, PropertyMapping property, string parameterName, string? referenceHandlerName)
    {
        var typeConverter = property.TypeConverter;

        return typeConverter switch
        {
            { HasParameter: true } => writer
                .Write(typeConverter.MethodFullName).WriteOpenParenthesis().Write(parameterName).Write(", ").Write(typeConverter.Parameter).WriteClosingParenthesis(),
            { HasParameter: false, ReferenceHandling: false } => writer
                .Write(typeConverter.MethodFullName).WriteOpenParenthesis().Write(parameterName).WriteClosingParenthesis(),
            { HasParameter: false, ReferenceHandling: true } => writer
                .Write(typeConverter.MethodFullName).WriteOpenParenthesis().Write(parameterName).Write(", ").Write(referenceHandlerName).WriteClosingParenthesis()
        };
    }
}