using MapTo.Mappings;

namespace MapTo.Generators;

internal static class ProjectionGenerator
{
    internal static CodeWriter WriteProjectionPartialMethods(this CodeWriter writer, TargetMapping mapping, CompilerOptions compilerOptions)
    {
        foreach (var projection in mapping.Projection)
        {
            var (_, originalAccessibility, methodName, returnType, parameterType, parameterName) = projection;
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
        var mapMethodName = $"global::{mapping.Namespace}.{mapping.ExtensionClassName}.{mapping.Options.MapMethodPrefix}{mapping.Name}";

        foreach (var projection in mapping.Projection)
        {
            var (_, _, methodName, returnType, parameterType, parameterName) = projection;
            var parameterIsNullable = projection.IsNullable(compilerOptions);
            var nullSyntax = parameterIsNullable ? compilerOptions.NullableReferenceSyntax : string.Empty;
            var accessibility = projection.Accessibility.ToString().ToLowerInvariant();

            writer
                .WriteLine()
                .WriteReturnNotNullIfNotNullAttributeIf(parameterIsNullable, parameterName)
                .WriteLine($"{accessibility} static {returnType.FullName}{nullSyntax} {methodName}(this {parameterType.FullName}{nullSyntax} {parameterName})")
                .WriteOpeningBracket()
                .WriteParameterNullCheckIf(parameterIsNullable, parameterName)
                .WriteProjectionForIEnumerableParameter(projection, mapMethodName)
                .WriteProjectionForCountableParameter(projection, mapMethodName)
                .WriteClosingBracket();
        }

        return writer;
    }

    private static CodeWriter WriteProjectionForIEnumerableParameter(this CodeWriter writer, ProjectionMapping projection, string mapMethodName)
    {
        var (_, _, _, returnType, parameterType, parameterName) = projection;
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
            .Write($"global::{KnownTypes.LinqEnumerable}.Select({parameterName}, x => {mapMethodName}(x))")
            .WriteIf(returnType.EnumerableType is not EnumerableType.Enumerable, ")")
            .WriteIf(returnType.EnumerableType is EnumerableType.Span or EnumerableType.Memory or EnumerableType.ReadOnlyMemory or EnumerableType.ReadOnlySpan, ")")
            .WriteLine(";");
    }

    private static CodeWriter WriteProjectionForCountableParameter(this CodeWriter writer, ProjectionMapping projection, string mapMethodName)
    {
        var (_, _, _, returnType, parameterType, parameterName) = projection;
        if (!parameterType.IsCountable)
        {
            return writer;
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
            { EnumerableType: EnumerableType.ImmutableArray } => writer.WriteExtensionBodyForImmutables(projection.ParameterName, mapMethodName),
            { IsFixedSize: true } or { IsCountable: false } => writer.WriteExtensionBodyForFixedSizeTypesOrSpans(projection, mapMethodName),
            { IsFixedSize: false, IsCountable: true } => writer.WriteExtensionBodyForCountableTypes(projection, mapMethodName)
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

    private static CodeWriter WriteExtensionBodyForImmutables(this CodeWriter writer, string parameterName, string mapMethodName) => writer
        .Write($"return global::{KnownTypes.SystemCollectionImmutableArray}.ToImmutableArray(")
        .WriteLine($"global::{KnownTypes.LinqEnumerable}.Select({parameterName}, x => {mapMethodName}(x)));");

    private static CodeWriter WriteExtensionBodyForFixedSizeTypesOrSpans(this CodeWriter writer, ProjectionMapping projection, string mapMethodName)
    {
        var (_, _, _, returnType, parameterType, parameterName) = projection;
        var lengthMethodName = parameterType.IsFixedSize ? "Length" : "Count";
        writer.WriteLine($"var target = new {returnType.ElementTypeName}[{parameterName}.{lengthMethodName}];");

        if (parameterType.EnumerableType.HasIndexer())
        {
            writer
                .WriteLine($"for (var i = 0; i < {parameterName}.{lengthMethodName}; i++)")
                .WriteOpeningBracket()
                .WriteLine($"target[i] = {mapMethodName}({parameterName}[i]);")
                .WriteClosingBracket();
        }
        else
        {
            writer
                .WriteLine("var i = 0;")
                .WriteLine($"foreach(var item in {parameterName})")
                .WriteOpeningBracket()
                .WriteLine($"target[i] = {mapMethodName}(item);")
                .WriteLine("i++;")
                .WriteClosingBracket();
        }

        return writer.WriteLine().WriteLine("return target;");
    }

    private static CodeWriter WriteExtensionBodyForCountableTypes(this CodeWriter writer, ProjectionMapping projection, string mapMethodName)
    {
        var (_, _, _, returnType, parameterType, parameterName) = projection;
        var lengthMethodName = parameterType.IsFixedSize ? "Length" : "Count";
        writer.WriteLine($"var target = new global::{KnownTypes.GenericList}<{returnType.ElementTypeName}>({parameterName}.{lengthMethodName});");

        if (parameterType.EnumerableType.HasIndexer())
        {
            writer
                .WriteLine($"for (var i = 0; i < {parameterName}.{lengthMethodName}; i++)")
                .WriteOpeningBracket()
                .WriteLine($"target.Add({mapMethodName}({parameterName}[i]));")
                .WriteClosingBracket();
        }
        else
        {
            writer
                .WriteLine("var i = 0;")
                .WriteLine($"foreach(var item in {parameterName})")
                .WriteOpeningBracket()
                .WriteLine($"target.Add({mapMethodName}(item));")
                .WriteLine("i++;")
                .WriteClosingBracket();
        }

        return writer.WriteLine().WriteLine("return target;");
    }
}