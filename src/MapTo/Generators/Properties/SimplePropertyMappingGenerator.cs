using MapTo.Mappings;
using static MapTo.NullHandling;
using static Microsoft.CodeAnalysis.NullableAnnotation;

namespace MapTo.Generators.Properties;

internal sealed class SimplePropertyMappingGenerator : PropertyMappingGenerator
{
    /// <inheritdoc />
    protected override bool HandleCore(PropertyGeneratorContext context)
    {
        var (writer, property, parameterName, targetInstanceName, _, _) = context;
        if (property.HasTypeConverter)
        {
            return false;
        }

        if (targetInstanceName is not null)
        {
            writer.Write(targetInstanceName).Write(".").Write(property.Name).Write(" = ");
        }

        parameterName = $"{parameterName}.{property.SourceName}";
        writer = property switch
        {
            { SourceType.IsNullable: false } => writer.Write(parameterName),
            { NullHandling: ThrowException } => writer.Write(parameterName).Write(" ?? ").WriteThrowArgumentNullException(parameterName),
            { NullHandling: SetEmptyCollection } => writer.Write(parameterName).Write(" ?? ").Write(property.Type.EmptySourceCodeString()),
            { NullHandling: Auto, Type.NullableAnnotation: NotAnnotated, SourceType: { IsNullable: true, NullableAnnotation: not NotAnnotated } } => writer
                .Write(parameterName).Write(" ?? ").Write(property.Type.EmptySourceCodeString()),
            _ => writer.Write(parameterName)
        };

        if (targetInstanceName is not null)
        {
            writer.WriteLine(";");
        }

        return true;
    }
}