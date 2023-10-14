using MapTo.Configuration;
using MapTo.Extensions;
using MapTo.Mappings;

namespace MapTo.Generators;

internal readonly record struct ExtensionClassGenerator(
    CompilerOptions CompilerOptions,
    TargetMapping TargetMapping) : ICodeGenerator
{
    /// <inheritdoc />
    public void BeginWrite(CodeWriter writer)
    {
        writer
            .WriteGeneratedCodeAttribute()
            .WriteExtensionClassDefinition(TargetMapping)
            .WriteOpeningBracket() // Class opening bracket
            .WriteMapExtensionMethod(TargetMapping, CompilerOptions)
            .WriteMapWithReferenceHandlerMethod(TargetMapping, CompilerOptions)
            .WriteMapArrayMethods(TargetMapping)
            .WriteMapToPrimitiveArrayMethods(TargetMapping)
            .WriteClosingBracket(); // Class closing bracket
    }
}

internal static class ExtensionClassGeneratorExtensions
{
    internal static CodeWriter WriteExtensionClassDefinition(this CodeWriter writer, TargetMapping mapping) =>
        writer.WriteLine($"public static class {mapping.Source.Name}{mapping.Options.MapExtensionClassSuffix}");

    internal static CodeWriter WriteMapArrayMethods(this CodeWriter writer, TargetMapping mapping)
    {
        var referenceHandler = mapping.Options.ReferenceHandling == ReferenceHandling.Enabled ? "referenceHandler" : null;
        var properties = mapping.Properties
            .Where(p => p is { SourceType.IsArray: true, TypeConverter: { IsMapToExtensionMethod: true, Type.EnumerableType: EnumerableType.Array } });

        foreach (var property in properties)
        {
            const string ParameterName = "sourceArray";
            var returnType = $"{property.TypeName}[]";
            var methodName = property.GetMapArrayMethodName();
            var sourceType = property.SourceType.FullName;
            var typeConverter = property.TypeConverter;
            var propertyMap = typeConverter switch
            {
                { HasParameter: false, ReferenceHandling: false } => $"{typeConverter.ContainingType}.{typeConverter.MethodName}({ParameterName}[i])",
                { HasParameter: false, ReferenceHandling: true } => $"{typeConverter.ContainingType}.{typeConverter.MethodName}({ParameterName}[i], {referenceHandler})",
                _ => $"{ParameterName}[i]"
            };

            writer
                .WriteLine()
                .Write("private static ")
                .Write(returnType)
                .WriteWhitespace()
                .Write(methodName)
                .WriteOpenParenthesis()
                .Write(sourceType)
                .WriteWhitespace()
                .Write(ParameterName)
                .WriteIf(typeConverter.ReferenceHandling, ", global::System.Collections.Generic.Dictionary<int, object> referenceHandler")
                .WriteClosingParenthesis()
                .WriteOpeningBracket() // Method opening bracket
                .WriteLine($"var targetArray = new {property.TypeName}[{ParameterName}.Length];")
                .WriteLine($"for (var i = 0; i < {ParameterName}.Length; i++)")
                .WriteOpeningBracket()
                .WriteLine($"targetArray[i] = {propertyMap};")
                .WriteClosingBracket()
                .WriteLine()
                .WriteLine("return targetArray;")
                .WriteClosingBracket();
        }

        return writer;
    }

    internal static CodeWriter WriteMapExtensionMethod(this CodeWriter writer, TargetMapping mapping, CompilerOptions compilerOptions)
    {
        var returnType = mapping.GetReturnType();
        var methodName = $"{mapping.Options.MapMethodPrefix}{mapping.Name}";
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

        if (mapping.Options.ReferenceHandling == ReferenceHandling.Enabled)
        {
            writer
                .WriteBeforeMapMethodCall(mapping)
                .WriteLine("var referenceHandler = new global::System.Collections.Generic.Dictionary<int, object>();")
                .WriteLine($"return {methodName}({parameterName}, referenceHandler);");
        }
        else
        {
            var hasSetterProperties = mapping.Properties.Any(p => p.InitializationMode == PropertyInitializationMode.Setter);
            var hasTargetVariable = hasSetterProperties || mapping.AfterMapMethod != default;

            writer
                .WriteBeforeMapMethodCall(mapping)
                .WriteParameterNullCheck(mapping.Source.Name.ToParameterNameCasing())
                .WriteLine()
                .WriteIf(hasTargetVariable, "var target = ", "return ")
                .WriteConstructorInitializer(mapping)
                .WriteObjectInitializer(mapping);

            if (hasSetterProperties)
            {
                writer.WriteLine().WritePropertySetters(mapping, "target", "referenceHandler");
            }

            if (hasTargetVariable)
            {
                writer
                    .WriteAfterMapMethodCall(mapping)
                    .WriteLine("return target;");
            }
        }

        return writer.WriteClosingBracket(); // Method closing bracket;
    }

    internal static CodeWriter WriteMapToPrimitiveArrayMethods(this CodeWriter writer, TargetMapping mapping)
    {
        if (!mapping.Options.CopyPrimitiveArrays)
        {
            return writer;
        }

        var properties = mapping.Properties
            .Where(p => p is { TypeConverter: { IsMapToExtensionMethod: false, Type.EnumerableType: EnumerableType.Array } });

        foreach (var property in properties)
        {
            const string ParameterName = "sourceArray";
            var returnType = $"{property.Type.FullName}[]";
            var methodName = property.GetMapArrayMethodName();
            var sourceType = property.SourceType.FullName;

            writer
                .WriteLine()
                .Write("private static ")
                .Write(returnType)
                .WriteWhitespace()
                .Write(methodName)
                .WriteOpenParenthesis()
                .Write(sourceType)
                .WriteWhitespace()
                .Write(ParameterName)
                .WriteClosingParenthesis()
                .WriteOpeningBracket() // Method opening bracket
                .WriteLine($"var targetArray = new {property.Type.FullName}[{ParameterName}.Length];")
                .WriteLine("global::System.Array.Copy(sourceArray, targetArray, sourceArray.Length);")
                .WriteLine()
                .WriteLine("return targetArray;")
                .WriteClosingBracket();
        }

        return writer;
    }

    internal static CodeWriter WriteMapWithReferenceHandlerMethod(this CodeWriter writer, TargetMapping mapping, CompilerOptions compilerOptions)
    {
        if (mapping.Options.ReferenceHandling != ReferenceHandling.Enabled)
        {
            return writer;
        }

        var returnType = mapping.GetReturnType();
        var methodName = $"{mapping.Options.MapMethodPrefix}{mapping.Name}";
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
            .WritePropertySetters(mapping, "target", "referenceHandler")
            .WriteAfterMapMethodCall(mapping)
            .WriteLine("return target;")
            .WriteClosingBracket();
    }

    private static string GetMapArrayMethodName(this PropertyMapping property) =>
        $"{property.TypeConverter.MethodName}Array";

    private static CodeWriter WriteAfterMapMethodCall(this CodeWriter writer, TargetMapping mapping) => mapping.AfterMapMethod switch
    {
        { MethodName: null } => writer,
        { Parameter.IsDefaultOrEmpty: true } => writer.WriteLine().Write(mapping.AfterMapMethod.MethodFullName).WriteLine("();").WriteLine(),
        { Parameter.IsDefaultOrEmpty: false } => writer.WriteLine().WriteLine($"{mapping.AfterMapMethod.MethodFullName}(target);").WriteLine()
    };

    private static CodeWriter WriteBeforeMapMethodCall(this CodeWriter writer, TargetMapping mapping)
    {
        var beforeMapMethod = mapping.BeforeMapMethod;
        var parameterName = mapping.Source.Name.ToParameterNameCasing();

        return beforeMapMethod switch
        {
            { MethodName: null } => writer,
            { Parameter.IsDefaultOrEmpty: true } => writer.Write(beforeMapMethod.MethodFullName).WriteLine("();").WriteLine(),
            { Parameter.IsDefaultOrEmpty: false, ReturnsVoid: true } => writer.WriteLine($"{beforeMapMethod.MethodFullName}({parameterName});").WriteLine(),
            { Parameter.IsDefaultOrEmpty: false, ReturnsVoid: false } => writer.WriteLine($"{parameterName} = {beforeMapMethod.MethodFullName}({parameterName});").WriteLine()
        };
    }

    private static CodeWriter WriteConstructorInitializer(this CodeWriter writer, TargetMapping mapping)
    {
        var sourceType = mapping.Source.Name;
        var parameterName = sourceType.ToParameterNameCasing();
        var hasObjectInitializer = mapping.Properties.Any(p => p.InitializationMode == PropertyInitializationMode.ObjectInitializer);

        if (!mapping.Constructor.HasParameters)
        {
            writer.Write($"new {mapping.Name}");
            return hasObjectInitializer ? writer.WriteNewLine() : writer.WriteLine("();");
        }

        writer.Write($"new {mapping.Name}(");

        for (var i = 0; i < mapping.Constructor.Parameters.Length; i++)
        {
            var parameter = mapping.Constructor.Parameters[i];

            PropertyGenerator.Instance.Generate(new(writer, parameter.Property, parameterName, null, mapping.Options.CopyPrimitiveArrays, null));
            writer.WriteIf(i < mapping.Constructor.Parameters.Length - 1, ", ");
        }

        return hasObjectInitializer ? writer.WriteLine(")") : writer.WriteLine(");");
    }

    private static CodeWriter WriteObjectInitializer(this CodeWriter writer, TargetMapping mapping)
    {
        var properties = mapping.Properties.Where(p => p.InitializationMode == PropertyInitializationMode.ObjectInitializer).ToArray();
        if (properties.Length == 0)
        {
            return writer;
        }

        var sourceType = mapping.Source.Name;
        var parameterName = sourceType.ToParameterNameCasing();

        writer.WriteOpeningBracket();

        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];

            writer.Write($"{property.Name} = ");
            PropertyGenerator.Instance.Generate(new(writer, property, parameterName, null, mapping.Options.CopyPrimitiveArrays, null));
            writer.WriteIf(i < properties.Length - 1, ",").WriteLineIndented();
        }

        return writer.WriteClosingBracket(false).WriteLine(";");
    }

    private static CodeWriter WriteParameterNullCheck(this CodeWriter writer, string parameterName) => writer
        .Write("if (").WriteIsNullCheck(parameterName).WriteLine(")")
        .WriteOpeningBracket()
        .WriteLine("return null;")
        .WriteClosingBracket();

    private static CodeWriter WritePropertySetters(this CodeWriter writer, TargetMapping mapping, string instanceName, string? referenceHandlerInstanceName = null)
    {
        var sourceType = mapping.Source.Name;
        var sourceParameterName = sourceType.ToParameterNameCasing();
        var properties = mapping.Properties.Where(p => p.InitializationMode is PropertyInitializationMode.Setter);
        var copyPrimitiveArrays = mapping.Options.CopyPrimitiveArrays;

        var newline = false;
        foreach (var property in properties)
        {
            // writer
            //     .WriteLineIf(newline = propertyMap.StartsWith("if") && !newline)
            //     .WriteLine(propertyMap)
            //     .WriteLineIf(propertyMap.EndsWith("}"));
            PropertyGenerator.Instance.Generate(new(writer, property, sourceParameterName, instanceName, copyPrimitiveArrays, referenceHandlerInstanceName));
        }

        return writer.WriteLineIf(!newline);
    }
}