using MapTo.Extensions;
using MapTo.Mappings;

namespace MapTo.Generators;

internal static class ProjectionGenerator
{
    internal static CodeWriter WriteProjectionPartialMethods(this CodeWriter writer, TargetMapping mapping, CompilerOptions compilerOptions)
    {
        foreach (var projection in mapping.Projections.Where(p => p.IsPartial))
        {
            var (_, originalAccessibility, methodName, returnType, parameterType, parameterName, _) = projection;
            var parameterIsNullable = projection.IsNullable(compilerOptions);
            var nullSyntax = parameterIsNullable ? compilerOptions.NullableReferenceSyntax : string.Empty;
            var accessibility = originalAccessibility.ToString().ToLowerInvariant();

            writer
                .WriteReturnNotNullIfNotNullAttributeIf(parameterIsNullable, parameterName)
                .WriteLine($"{accessibility} static partial {returnType.FullName}{nullSyntax} {methodName}({parameterType.FullName}{nullSyntax} {parameterName})")
                .WriteOpeningBracket()
                .WriteLine($"return global::{mapping.Namespace}.{mapping.ExtensionClassName}.{methodName}({parameterName});")
                .WriteClosingBracket();
        }

        return writer;
    }

    internal static CodeWriter WriteProjectionExtensionMethods(this CodeWriter writer, TargetMapping mapping, CompilerOptions compilerOptions)
    {
        var mapMethodName = $"{mapping.Options.MapMethodPrefix}{mapping.Name}";

        foreach (var projection in mapping.Projections)
        {
            var (_, _, methodName, returnType, parameterType, parameterName, _) = projection;
            var parameterIsNullable = projection.IsNullable(compilerOptions);
            var nullSyntax = parameterIsNullable ? compilerOptions.NullableReferenceSyntax : string.Empty;
            var accessibility = projection.Accessibility.ToString().ToLowerInvariant();

            writer
                .WriteLine()
                .WriteReturnNotNullIfNotNullAttributeIf(parameterIsNullable, parameterName)
                .Write($"{accessibility} static {returnType.FullName}{nullSyntax} {methodName}(this {parameterType.FullName}{nullSyntax} {parameterName}");

            // If there are any required properties with ignored attribute, we should add them as parameters
            var requiredProperties = mapping.Properties.Where(p => p is { IsRequired: true, InitializationMode: PropertyInitializationMode.None }).ToArray();
            var mapMethodParameters = new string[requiredProperties.Length];

            for (var i = 0; i < requiredProperties.Length; i++)
            {
                var property = requiredProperties[i];
                mapMethodParameters[i] = property.Name.ToParameterNameCasing();

                writer.Write(", ").Write(property.TypeName).WriteWhitespace().Write(mapMethodParameters[i]);
            }

            writer
                .WriteClosingParenthesis()
                .WriteOpeningBracket()
                .WriteParameterNullCheckIf(parameterIsNullable, parameterName)
                .WriteProjectionForIEnumerableParameter(projection, mapMethodName, mapMethodParameters)
                .WriteProjectionForCountableParameter(projection, mapMethodName, mapMethodParameters)
                .WriteClosingBracket();
        }

        return writer;
    }

    private static CodeWriter WriteProjectionForIEnumerableParameter(this CodeWriter writer, ProjectionMapping projection, string mapMethodName, string[] mapMethodParameters)
    {
        var (_, _, _, returnType, parameterType, parameterName, _) = projection;
        if (parameterType.IsCountable)
        {
            return writer;
        }

        return writer
            .Write("return ")
            .Write(returnType.EnumerableType switch
            {
                EnumerableType.Enumerable => string.Empty,
                EnumerableType.List or EnumerableType.ReadOnlyList or EnumerableType.Collection => $"global::{KnownTypes.LinqToList}(",
                EnumerableType.Span or EnumerableType.Memory or EnumerableType.ReadOnlyMemory or EnumerableType.ReadOnlySpan =>
                    $"new {returnType.FullName}(global::{KnownTypes.LinqToArray}(",
                EnumerableType.ImmutableArray => $"global::{KnownTypes.SystemCollectionImmutableArray}.ToImmutableArray(",
                _ => $"global::{KnownTypes.LinqToArray}("
            })
            .Write($"global::{KnownTypes.LinqEnumerable}.Select({parameterName}, x => {mapMethodName}(x").WriteJoinedParameterNames(mapMethodParameters).Write("))")
            .WriteIf(returnType.EnumerableType is not EnumerableType.Enumerable, ")")
            .WriteIf(returnType.EnumerableType is EnumerableType.Span or EnumerableType.Memory or EnumerableType.ReadOnlyMemory or EnumerableType.ReadOnlySpan, ")")
            .WriteLine(";");
    }

    private static CodeWriter WriteProjectionForCountableParameter(this CodeWriter writer, ProjectionMapping projection, string mapMethodName, string[] mapMethodParameters)
    {
        var (_, _, _, returnType, parameterType, parameterName, _) = projection;
        if (!parameterType.IsCountable)
        {
            return writer;
        }

        if (parameterType is { IsReferenceType: false, NullableAnnotation: NullableAnnotation.Annotated })
        {
            parameterName = $"{parameterName}.Value";
        }

        projection = projection with
        {
            ParameterName = parameterType.EnumerableType switch
            {
                EnumerableType.Memory or EnumerableType.ReadOnlyMemory when returnType.EnumerableType is EnumerableType.ImmutableArray => $"{parameterName}.Span.ToArray()",
                EnumerableType.Memory or EnumerableType.ReadOnlyMemory => $"{parameterName}.Span",
                EnumerableType.Span when returnType.EnumerableType is EnumerableType.ImmutableArray => $"{parameterName}.ToArray()",
                _ => parameterName
            }
        };

        return returnType switch
        {
            { EnumerableType: EnumerableType.ImmutableArray } => writer.WriteExtensionBodyForImmutables(projection.ParameterName, mapMethodName, mapMethodParameters),
            { IsFixedSize: true } or { IsCountable: false } => writer.WriteExtensionBodyForFixedSizeTypesOrSpans(projection, mapMethodName, mapMethodParameters),
            { IsFixedSize: false, IsCountable: true } => writer.WriteExtensionBodyForCountableTypes(projection, mapMethodName, mapMethodParameters)
        };
    }

    private static bool IsNullable(this ProjectionMapping projectionMapping, CompilerOptions compilerOptions) => compilerOptions.NullableReferenceTypes
        ? projectionMapping.ParameterType.NullableAnnotation == NullableAnnotation.Annotated
        : projectionMapping.ParameterType.IsNullable;

    private static CodeWriter WriteParameterNullCheckIf(this CodeWriter writer, bool condition, string parameterName)
    {
        if (condition)
        {
            writer
                .Write("if (").WriteIsNullCheck(parameterName).WriteLine(")")
                .WriteOpeningBracket()
                .WriteLine("return null;")
                .WriteClosingBracket()
                .WriteLine();
        }

        return writer;
    }

    private static CodeWriter WriteExtensionBodyForImmutables(this CodeWriter writer, string parameterName, string mapMethodName, string[] mapMethodParameters) => writer
        .Write($"return global::{KnownTypes.SystemCollectionImmutableArray}.ToImmutableArray(")
        .Write($"global::{KnownTypes.LinqEnumerable}.Select({parameterName}, x => {mapMethodName}(x").WriteJoinedParameterNames(mapMethodParameters).WriteLine(")));");

    private static CodeWriter WriteExtensionBodyForFixedSizeTypesOrSpans(this CodeWriter writer, ProjectionMapping projection, string mapMethodName, string[] mapMethodParameters)
    {
        var (_, _, _, returnType, parameterType, parameterName, _) = projection;
        var lengthMethodName = parameterType.IsFixedSize ? "Length" : "Count";
        writer.WriteLine($"var target = new {returnType.ElementTypeName}[{parameterName}.{lengthMethodName}];");

        if (parameterType.EnumerableType.HasIndexer())
        {
            writer
                .WriteLine($"for (var i = 0; i < {parameterName}.{lengthMethodName}; i++)")
                .WriteOpeningBracket()
                .Write($"target[i] = {mapMethodName}({parameterName}[i]").WriteJoinedParameterNames(mapMethodParameters).WriteLine(");")
                .WriteClosingBracket();
        }
        else
        {
            writer
                .WriteLine("var i = 0;")
                .WriteLine($"foreach (var item in {parameterName})")
                .WriteOpeningBracket()
                .Write($"target[i] = {mapMethodName}(item").WriteJoinedParameterNames(mapMethodParameters).WriteLine(");")
                .WriteLine("i++;")
                .WriteClosingBracket();
        }

        return writer.WriteLine().WriteLine("return target;");
    }

    private static CodeWriter WriteExtensionBodyForCountableTypes(this CodeWriter writer, ProjectionMapping projection, string mapMethodName, string[] mapMethodParameters)
    {
        var (_, _, _, returnType, parameterType, parameterName, _) = projection;
        var lengthMethodName = parameterType.IsFixedSize ? "Length" : "Count";
        writer.WriteLine($"var target = new global::{KnownTypes.GenericList}<{returnType.ElementTypeName}>({parameterName}.{lengthMethodName});");

        if (parameterType.EnumerableType.HasIndexer())
        {
            writer
                .WriteLine($"for (var i = 0; i < {parameterName}.{lengthMethodName}; i++)")
                .WriteOpeningBracket()
                .Write($"target.Add({mapMethodName}({parameterName}[i]").WriteJoinedParameterNames(mapMethodParameters).WriteLine("));")
                .WriteClosingBracket();
        }
        else
        {
            writer
                .WriteLine($"foreach (var item in {parameterName})")
                .WriteOpeningBracket()
                .Write($"target.Add({mapMethodName}(item").WriteJoinedParameterNames(mapMethodParameters).WriteLine("));")
                .WriteClosingBracket();
        }

        return writer.WriteLine().WriteLine("return target;");
    }

    private static CodeWriter WriteJoinedParameterNames(this CodeWriter writer, string[] parameters)
    {
        foreach (var parameter in parameters)
        {
            writer.Write(", ").Write(parameter);
        }

        return writer;
    }
}