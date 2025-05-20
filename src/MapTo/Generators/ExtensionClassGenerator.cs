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
        if (TargetMapping.TypeKeyword.Equals("enum", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        writer
            .WriteGeneratedCodeAttribute()
            .WriteExtensionClassDefinition(TargetMapping)
            .WriteOpeningBracket() // Class opening bracket
            .WriteMapExtensionMethod(TargetMapping, CompilerOptions)
            .WriteMapWithReferenceHandlerMethod(TargetMapping, CompilerOptions)
            .WriteMapArrayMethods(TargetMapping)
            .WriteMapEnumMethods(TargetMapping)
            .WriteGeneratedTypeConverters(TargetMapping)
            .WriteMapToPrimitiveArrayMethods(TargetMapping)
            .WriteProjectionExtensionMethods(TargetMapping, CompilerOptions)
            .WriteClosingBracket(); // Class closing bracket
    }
}

static file class ExtensionClassGeneratorExtensions
{
    internal static CodeWriter WriteExtensionClassDefinition(this CodeWriter writer, TargetMapping mapping) =>
        writer.WriteLine($"public static partial class {mapping.ExtensionClassName}");

    internal static CodeWriter WriteMapArrayMethods(this CodeWriter writer, TargetMapping mapping)
    {
        const string ParameterName = "sourceArray";
        var referenceHandler = mapping.Options.ReferenceHandling == ReferenceHandling.Enabled ? "referenceHandler" : null;

        var properties = (!mapping.Properties.IsDefaultOrEmpty ? mapping.Properties : mapping.Constructor.Parameters.Select(p => p.Property))
            .Where(p => p is { SourceType.IsArray: true, TypeConverter: { IsMapToExtensionMethod: true, Type.EnumerableType: EnumerableType.Array } })
            .Select(p => (
                ReturnType: $"{p.Type.ElementTypeName}[]",
                MethodName: p.GetMapArrayMethodName(),
                SourceType: p.SourceType.FullName,
                TypeName: p.Type.ElementTypeName,
                TypeConverter: p.TypeConverter))
            .Distinct();

        foreach (var (returnType, methodName, sourceType, typeName, typeConverter) in properties)
        {
            var propertyMap = typeConverter switch
            {
                { HasParameter: false, ReferenceHandling: false } => $"{typeConverter.MethodFullName}({ParameterName}[i])",
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
                .WriteLine($"var targetArray = new {typeName}[{ParameterName}.Length];")
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

    internal static CodeWriter WriteMapEnumMethods(this CodeWriter writer, TargetMapping mapping)
    {
        var properties = mapping.Properties
            .Union(mapping.Constructor.Parameters.Select(p => p.Property))
            .Where(p => p is { SourceType.IsEnum: true, TypeConverter: { IsMapToExtensionMethod: false, EnumMapping.Strategy: not EnumMappingStrategy.ByValue } })
            .GroupBy(p => p.SourceType)
            .Select(g => g.First());

        foreach (var property in properties)
        {
            var sourceType = property.SourceType.FullName;
            var propertyType = property.TypeName;

            writer
                .WriteLine()
                .Write("private static ")
                .Write(propertyType)
                .WriteWhitespace()
                .Write(property.TypeConverter.MethodName)
                .WriteOpenParenthesis()
                .Write(sourceType)
                .WriteWhitespace()
                .Write("source")
                .WriteClosingParenthesis()
                .WriteOpeningBracket() // Method opening bracket
                .WriteLine("return source switch")
                .WriteOpeningBracket();

            foreach (var member in property.TypeConverter.EnumMapping.Mappings)
            {
                writer.Write("global::").Write(member.Source).Write(" => ").Write("global::").Write(member.Target).WriteLine(",");
            }

            if (property.TypeConverter.EnumMapping.FallBackValue is not null)
            {
                writer.Write("_ => ").Write("global::").WriteLine(property.TypeConverter.EnumMapping.FallBackValue);
            }
            else
            {
                writer
                    .Write("_ => ")
                    .WriteThrowArgumentOutOfRangeException("source", $"\"Unable to map enum value '{property.SourceType.QualifiedName}' to '{property.Type.QualifiedName}'.\"")
                    .WriteLineIndented();
            }

            writer
                .WriteClosingBracket(false)
                .WriteLine(";")
                .WriteClosingBracket(); // Method closing bracket
        }

        return writer;
    }

    internal static CodeWriter WriteGeneratedTypeConverters(this CodeWriter writer, TargetMapping targetMapping)
    {
        var generatedTypeConverters = targetMapping.Properties
            .Union(targetMapping.Constructor.Parameters.Select(p => p.Property))
            .Where(p => p.TypeConverter is { Explicit: true, IsMapToExtensionMethod: true, ContainingType: "" })
            .Select(p => p.TypeConverter);

        foreach (var typeConverter in generatedTypeConverters)
        {
            var method = typeConverter.Method;

            writer
                .WriteLine()
                .Write("private static ").Write(method.ReturnType.FullName).Write(" ").Write(method.MethodName).WriteOpenParenthesis()
                .WriteJoin(", ", method.Parameters.Select(p => p.ToDisplayString()))
                .Write(")");

            if (method.Body.Length == 1)
            {
                writer.Write(" => ").Write(method.Body[0]).WriteLine(";");
            }
            else
            {
                writer
                    .WriteLine()
                    .WriteOpeningBracket()
                    .WriteLineJoin(string.Empty, method.Body)
                    .WriteClosingBracket();
            }
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
            .Write(parameterName);

        // If there are any required properties with ignored attribute, we should add them as parameters
        foreach (var property in mapping.Properties.Where(p => p is { IsRequired: true, InitializationMode: PropertyInitializationMode.None }))
        {
            var parameterType = property.TypeName;
            var parameter = property.Name.ToParameterNameCasing();
            writer.Write(", ").Write(parameterType).WriteWhitespace().Write(parameter);
        }

        writer
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

        const string ParameterName = "sourceArray";
        var properties = mapping.Properties
            .Where(p => p is { TypeConverter: { Explicit: false, IsMapToExtensionMethod: false, Type.EnumerableType: EnumerableType.Array } })
            .Select(p =>
            (
                ReturnType: $"{p.Type.ElementTypeName}[]",
                MethodName: p.GetMapArrayMethodName(),
                SourceType: p.SourceType.FullName,
                TypeFullName: p.Type.ElementTypeName))
            .Distinct();

        foreach (var (returnType, methodName, sourceType, typeFullName) in properties)
        {
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
                .WriteLine($"var targetArray = new {typeFullName}[{ParameterName}.Length];")
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

    private static CodeWriter WriteAfterMapMethodCall(this CodeWriter writer, TargetMapping mapping)
    {
        if (mapping.AfterMapMethod == default)
        {
            return writer;
        }

        writer
            .WriteLine()
            .WriteIf(!mapping.AfterMapMethod.ReturnsVoid, "target = ");

        return mapping.AfterMapMethod switch
        {
            { Parameters.IsDefaultOrEmpty: true } => writer.Write(mapping.AfterMapMethod.MethodFullName).WriteLine("();").WriteLine(),
            { Parameters.Length: 1 } => writer.WriteLine($"{mapping.AfterMapMethod.MethodFullName}(target);").WriteLine(),
            { Parameters.Length: 2 } when mapping.AfterMapMethod.Parameters[0].Type == mapping.Source.ToFullyQualifiedName() => writer
                .WriteLine($"{mapping.AfterMapMethod.MethodFullName}({mapping.Source.Name.ToParameterNameCasing()}, target);").WriteLine(),
            { Parameters.Length: 2 } when mapping.AfterMapMethod.Parameters[1].Type == mapping.Source.ToFullyQualifiedName() => writer
                .WriteLine($"{mapping.AfterMapMethod.MethodFullName}(target, {mapping.Source.Name.ToParameterNameCasing()});").WriteLine(),
            _ => writer
        };
    }

    private static CodeWriter WriteBeforeMapMethodCall(this CodeWriter writer, TargetMapping mapping)
    {
        var beforeMapMethod = mapping.BeforeMapMethod;
        var parameterName = mapping.Source.Name.ToParameterNameCasing();

        return beforeMapMethod switch
        {
            { MethodName: null } => writer,
            { Parameters.IsDefaultOrEmpty: true } => writer.Write(beforeMapMethod.MethodFullName).WriteLine("();").WriteLine(),
            { Parameters.IsDefaultOrEmpty: false, ReturnsVoid: true } => writer.WriteLine($"{beforeMapMethod.MethodFullName}({parameterName});").WriteLine(),
            { Parameters.IsDefaultOrEmpty: false, ReturnsVoid: false } => writer.WriteLine($"{parameterName} = {beforeMapMethod.MethodFullName}({parameterName});").WriteLine()
        };
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

        foreach (var property in properties)
        {
            PropertyGenerator.Instance.Generate(new(writer, property, sourceParameterName, instanceName, copyPrimitiveArrays, referenceHandlerInstanceName));
        }

        return writer.WriteLine();
    }
}