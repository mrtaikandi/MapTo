using MapTo.Configuration;
using MapTo.Extensions;
using MapTo.Mappings;

namespace MapTo.Generators;

internal readonly record struct ExtensionClassGenerator(
    CodeGeneratorOptions GeneratorOptions,
    CompilerOptions CompilerOptions,
    TargetMapping TargetMapping) : ICodeGenerator
{
    /// <inheritdoc />
    public void BeginWrite(CodeWriter writer)
    {
        writer
            .WriteCodeGeneratorAttribute()
            .WriteExtensionClassDefinition(TargetMapping, GeneratorOptions)
            .WriteOpeningBracket() // Class opening bracket
            .WriteMapExtensionMethod(TargetMapping, GeneratorOptions, CompilerOptions)
            .WriteMapWithReferenceHandlerMethod(TargetMapping, GeneratorOptions, CompilerOptions)
            .WriteClosingBracket(); // Class closing bracket
    }
}

internal static class ExtensionClassGeneratorExtensions
{
    internal static CodeWriter WriteExtensionClassDefinition(this CodeWriter writer, TargetMapping mapping, CodeGeneratorOptions options) =>
        writer.WriteLine($"public static class {mapping.Source.Name}{options.MapExtensionClassSuffix}");

    internal static CodeWriter WriteMapExtensionMethod(this CodeWriter writer, TargetMapping mapping, CodeGeneratorOptions options, CompilerOptions compilerOptions)
    {
        var returnType = mapping.GetReturnType();
        var methodName = $"{options.MapMethodPrefix}{mapping.Name}";
        var sourceType = mapping.GetSourceType();
        var parameterName = mapping.Source.Name.ToParameterNameCasing();

        writer
            .WriteReturnNotNullIfNotNullAttributeIfRequired(mapping, compilerOptions)
            .Write(mapping.Modifier.ToLowercaseString())
            .Write(" static ")
            .Write(returnType)
            .Write(compilerOptions.NullableReferenceSyntax)
            .WriteWhitespace()
            .Write(methodName)
            .WriteOpenParenthesis()
            .WriteAllowNullAttributeIf(compilerOptions is { NullableStaticAnalysis: true, NullableReferenceTypes: false })
            .Write("this ")
            .Write(sourceType)
            .Write(compilerOptions.NullableReferenceSyntax)
            .WriteWhitespace()
            .Write(parameterName)
            .WriteClosingParenthesis()
            .WriteOpeningBracket(); // Method opening bracket

        if (mapping.UseReferenceHandling)
        {
            writer.WriteLine("var referenceHandler = new global::System.Collections.Generic.Dictionary<int, object>();");
            writer.WriteLine($"return {methodName}({parameterName}, referenceHandler);");
        }
        else
        {
            writer
                .WriteParameterNullCheck(mapping.Source.Name.ToParameterNameCasing())
                .WriteLine()
                .Write("return ")
                .WriteConstructorInitializer(mapping)
                .WriteObjectInitializer(mapping);
        }

        return writer.WriteClosingBracket(); // Method closing bracket;
    }

    internal static CodeWriter WriteMapWithReferenceHandlerMethod(this CodeWriter writer, TargetMapping mapping, CodeGeneratorOptions options, CompilerOptions compilerOptions)
    {
        if (!mapping.UseReferenceHandling)
        {
            return writer;
        }

        var returnType = mapping.GetReturnType();
        var methodName = $"{options.MapMethodPrefix}{mapping.Name}";
        var sourceType = mapping.GetSourceType();
        var parameterName = mapping.Source.Name.ToParameterNameCasing();

        return writer
            .WriteLine()
            .WriteReturnNotNullIfNotNullAttributeIfRequired(mapping, compilerOptions)
            .Write("internal static ")
            .Write(returnType)
            .Write(compilerOptions.NullableReferenceSyntax)
            .WriteWhitespace()
            .Write(methodName)
            .WriteOpenParenthesis()
            .Write(sourceType)
            .Write(compilerOptions.NullableReferenceSyntax)
            .WriteWhitespace()
            .Write(parameterName)
            .Write(", global::System.Collections.Generic.Dictionary<int, object> referenceHandler")
            .WriteClosingParenthesis()
            .WriteOpeningBracket()
            .WriteParameterNullCheck(parameterName)
            .WriteLine()
            .WriteLine($"if (referenceHandler.TryGetValue({parameterName}.GetHashCode(), out var cachedTarget))")
            .WriteOpeningBracket()
            .WriteLine($"return ({returnType})cachedTarget;")
            .WriteClosingBracket()
            .WriteLine()
            .Write("var target = ")
            .WriteConstructorInitializer(mapping)
            .WriteObjectInitializer(mapping)
            .WriteLine()
            .WriteLine($"referenceHandler.Add({parameterName}.GetHashCode(), target);")
            .WriteLine()
            .WritPropertySetters(mapping, "target", "referenceHandler")
            .WriteLine()
            .WriteLine("return target;")
            .WriteClosingBracket();
    }

    private static string GetMappedProperty(this PropertyMapping property, string parameterName, string? referenceHandlerInstanceName = null)
    {
        if (!property.HasTypeConverter)
        {
            return $"{parameterName}.{property.Name}";
        }

        var typeConverter = property.TypeConverter;
        var converterParameter = typeConverter.Parameter ?? (typeConverter.UsingReferenceHandler ? referenceHandlerInstanceName : null);

        if (typeConverter.IsEnumerable)
        {
            var selector = converterParameter is null
                ? $"{typeConverter.ContainingType}.{typeConverter.MethodName}"
                : $"{property.ParameterName[0]} => {typeConverter.ContainingType}.{typeConverter.MethodName}({property.ParameterName[0]}, {referenceHandlerInstanceName})";

            return $"{parameterName}.{property.Name}.Select({selector}).To{typeConverter.EnumerableType}()";
        }

        return converterParameter is not null
            ? $"{typeConverter.ContainingType}.{typeConverter.MethodName}({parameterName}.{property.Name}, {converterParameter})"
            : $"{typeConverter.ContainingType}.{typeConverter.MethodName}({parameterName}.{property.Name})";
    }

    private static CodeWriter WriteConstructorInitializer(this CodeWriter writer, TargetMapping mapping)
    {
        var sourceType = mapping.Source.Name;
        var parameterName = sourceType.ToParameterNameCasing();
        var properties = mapping.Properties.Where(p => p.InitializationMode == PropertyInitializationMode.Constructor);
        var hasObjectInitializer = mapping.Properties.Any(p => p.InitializationMode == PropertyInitializationMode.ObjectInitializer);

        if (mapping.Constructor is { IsGenerated: false, HasArguments: false })
        {
            writer.Write($"new {mapping.Name}");
            return hasObjectInitializer ? writer.WriteNewLine() : writer.WriteLine("();");
        }

        writer
            .Write($"new {mapping.Name}(")
            .WriteJoin(", ", properties.Select(p => p.GetMappedProperty(parameterName)));

        return hasObjectInitializer
            ? writer.WriteLine(")")
            : writer.WriteLine(");");
    }

    private static CodeWriter WriteObjectInitializer(this CodeWriter writer, TargetMapping mapping)
    {
        var properties = mapping.Properties.Where(p =>
                p.InitializationMode == PropertyInitializationMode.ObjectInitializer ||
                (!mapping.UseReferenceHandling && p.InitializationMode == PropertyInitializationMode.Setter))
            .ToArray();

        if (properties.Length == 0)
        {
            return writer;
        }

        var sourceType = mapping.Source.Name;
        var parameterName = sourceType.ToParameterNameCasing();

        return writer
            .WriteOpeningBracket()
            .WriteLineJoin(",", properties.Select(p => $"{p.Name} = {p.GetMappedProperty(parameterName)}"))
            .WriteClosingBracket(false).WriteLine(";");
    }

    private static CodeWriter WritPropertySetters(this CodeWriter writer, TargetMapping mapping, string instanceName, string? referenceHandlerInstanceName = null)
    {
        var properties = mapping.Properties.Where(p => p.InitializationMode == PropertyInitializationMode.Setter).ToArray();
        if (properties.Length == 0)
        {
            return writer;
        }

        var sourceType = mapping.Source.Name;
        var parameterName = sourceType.ToParameterNameCasing();

        foreach (var property in properties)
        {
            writer.WriteLine($"{instanceName}.{property.Name} = {property.GetMappedProperty(parameterName, referenceHandlerInstanceName)};");
        }

        return writer;
    }

    private static CodeWriter WriteReturnNotNullIfNotNullAttributeIfRequired(this CodeWriter writer, TargetMapping mapping, CompilerOptions options) =>
        options.NullableStaticAnalysis || options.NullableReferenceTypes ? writer.WriteReturnNotNullIfNotNullAttribute(mapping.Source.Name.ToParameterNameCasing()) : writer;

    private static CodeWriter WriteParameterNullCheck(this CodeWriter writer, string parameterName) => writer
        .WriteLine($"if (ReferenceEquals({parameterName}, null))")
        .WriteOpeningBracket()
        .WriteLine("return null;")
        .WriteClosingBracket();
}