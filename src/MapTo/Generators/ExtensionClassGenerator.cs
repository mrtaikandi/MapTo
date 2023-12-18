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
            .WriteMapEnumMethods(TargetMapping)
            .WriteMapToPrimitiveArrayMethods(TargetMapping)
            .WriteProjectionExtensionMethods(TargetMapping, CompilerOptions)
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

    internal static CodeWriter WriteProjectionExtensionMethods(this CodeWriter writer, TargetMapping mapping, CompilerOptions compilerOptions)
    {
        var projectionType = mapping.Options.ProjectionType;
        if (projectionType is ProjectionType.None)
        {
            return writer;
        }

        string returnType;
        string sourceType;
        const string ParameterName = "source";
        var methodName = $"{mapping.Options.MapMethodPrefix}{mapping.Name}".Pluralize();
        var mapMethodName = $"{mapping.Options.MapMethodPrefix}{mapping.Name}";

        switch (projectionType)
        {
            case ProjectionType.Array:
                sourceType = $"{mapping.GetSourceType()}[]";
                returnType = $"{mapping.GetReturnType()}[]";
                break;

            case ProjectionType.Enumerable:
                sourceType = $"global::{KnownTypes.GenericIEnumerable}<{mapping.GetSourceType()}>";
                returnType = $"global::{KnownTypes.GenericIEnumerable}<{mapping.GetReturnType()}>";
                break;

            case ProjectionType.ICollection:
                sourceType = $"global::{KnownTypes.GenericICollection}<{mapping.GetSourceType()}>";
                returnType = $"global::{KnownTypes.GenericICollection}<{mapping.GetReturnType()}>";
                break;

            case ProjectionType.IReadOnlyCollection:
                sourceType = $"global::{KnownTypes.GenericIReadOnlyCollection}<{mapping.GetSourceType()}>";
                returnType = $"global::{KnownTypes.GenericIReadOnlyCollection}<{mapping.GetReturnType()}>";
                break;

            case ProjectionType.Collection:
                sourceType = $"global::{KnownTypes.ObjectModelCollection}<{mapping.GetSourceType()}>";
                returnType = $"global::{KnownTypes.ObjectModelCollection}<{mapping.GetReturnType()}>";
                break;

            case ProjectionType.IList:
                sourceType = $"global::{KnownTypes.GenericIList}<{mapping.GetSourceType()}>";
                returnType = $"global::{KnownTypes.GenericIList}<{mapping.GetReturnType()}>";
                break;

            case ProjectionType.IReadOnlyList:
                sourceType = $"global::{KnownTypes.GenericIReadOnlyList}<{mapping.GetSourceType()}>";
                returnType = $"global::{KnownTypes.GenericIReadOnlyList}<{mapping.GetReturnType()}>";
                break;

            case ProjectionType.List:
                sourceType = $"global::{KnownTypes.GenericList}<{mapping.GetSourceType()}>";
                returnType = $"global::{KnownTypes.GenericList}<{mapping.GetReturnType()}>";
                break;

            case ProjectionType.Span:
                sourceType = $"global::{KnownTypes.SystemSpan}<{mapping.GetSourceType()}>";
                returnType = $"global::{KnownTypes.SystemSpan}<{mapping.GetReturnType()}>";
                compilerOptions = compilerOptions with { NullableReferenceTypes = false, NullableStaticAnalysis = false };
                break;

            case ProjectionType.Memory:
                sourceType = $"global::{KnownTypes.SystemMemory}<{mapping.GetSourceType()}>";
                returnType = $"global::{KnownTypes.SystemMemory}<{mapping.GetReturnType()}>";
                break;

            case ProjectionType.ReadOnlySpan:
                sourceType = $"global::{KnownTypes.SystemReadOnlySpan}<{mapping.GetSourceType()}>";
                returnType = $"global::{KnownTypes.SystemReadOnlySpan}<{mapping.GetReturnType()}>";
                compilerOptions = compilerOptions with { NullableReferenceTypes = false, NullableStaticAnalysis = false };
                break;

            case ProjectionType.ReadOnlyMemory:
                sourceType = $"global::{KnownTypes.SystemReadOnlyMemory}<{mapping.GetSourceType()}>";
                returnType = $"global::{KnownTypes.SystemReadOnlyMemory}<{mapping.GetReturnType()}>";
                break;

            default:
                // ReSharper disable once NotResolvedInText
                throw new ArgumentOutOfRangeException("ProjectTo", $"Unknown projection type '{projectionType}'.");
        }

        writer
            .WriteLine()
            .WriteReturnNotNullIfNotNullAttributeIfRequired(mapping, compilerOptions, parameterName: ParameterName)
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
            .Write(ParameterName)
            .WriteClosingParenthesis()
            .WriteOpeningBracket(); // Method opening bracket

        switch (projectionType)
        {
            case ProjectionType.Array:
            case ProjectionType.Span:
            case ProjectionType.ReadOnlySpan:
                writer
                    .WriteParameterNullCheckIf(compilerOptions.NullableReferenceTypes, ParameterName)
                    .WriteLineIf(compilerOptions.NullableReferenceTypes)
                    .WriteLine($"var target = new {mapping.GetReturnType()}[{ParameterName}.Length];")
                    .WriteLine("for (var i = 0; i < target.Length; i++)")
                    .WriteOpeningBracket()
                    .WriteLine($"target[i] = {mapMethodName}({ParameterName}[i]);")
                    .WriteClosingBracket()
                    .WriteLine()
                    .WriteLine("return target;");

                break;

            case ProjectionType.Enumerable:
                writer
                    .Write("return ")
                    .WriteTernaryIsNullCheck(ParameterName, "null", $"global::{KnownTypes.LinqEnumerable}.Select({ParameterName}, x => {mapMethodName}(x))").WriteLine(";");

                break;

            case ProjectionType.ICollection:
            case ProjectionType.IReadOnlyCollection:
            case ProjectionType.IList:
            case ProjectionType.IReadOnlyList:
            case ProjectionType.List:
                writer
                    .WriteParameterNullCheck(ParameterName)
                    .WriteLine()
                    .WriteLine($"var target = new global::{KnownTypes.GenericList}<{mapping.GetReturnType()}>(source.Count);")
                    .WriteLine("foreach (var item in source)")
                    .WriteOpeningBracket()
                    .WriteLine($"target.Add({mapMethodName}(item));")
                    .WriteClosingBracket()
                    .WriteLine()
                    .WriteLine("return target;");

                break;

            case ProjectionType.Collection:
                writer
                    .WriteParameterNullCheck(ParameterName)
                    .WriteLine()
                    .WriteLine($"var target = new global::{KnownTypes.GenericList}<{mapping.GetReturnType()}>(source.Count);")
                    .WriteLine("foreach (var item in source)")
                    .WriteOpeningBracket()
                    .WriteLine($"target.Add({mapMethodName}(item));")
                    .WriteClosingBracket()
                    .WriteLine()
                    .WriteLine($"return new global::{KnownTypes.ObjectModelCollection}<{mapping.GetReturnType()}>(target);");

                break;

            case ProjectionType.Memory:
            case ProjectionType.ReadOnlyMemory:
                var nullableValue = compilerOptions.NullableReferenceTypes ? ".Value" : string.Empty;
                writer
                    .WriteParameterNullCheckIf(compilerOptions.NullableReferenceTypes, ParameterName)
                    .WriteLineIf(compilerOptions.NullableReferenceTypes)
                    .WriteLine($"var sourceSpan = source{nullableValue}.Span;")
                    .WriteLine($"var target = new {mapping.GetReturnType()}[sourceSpan.Length];")
                    .WriteLine()
                    .WriteLine("for (var i = 0; i < target.Length; i++)")
                    .WriteOpeningBracket()
                    .WriteLine($"target[i] = {mapMethodName}(sourceSpan[i]);")
                    .WriteClosingBracket()
                    .WriteLine()
                    .WriteLine("return target;");

                break;

            case ProjectionType.Queryable:
                throw new NotImplementedException("Queryable projection is not implemented yet.");

            default:
                // ReSharper disable once NotResolvedInText
                throw new ArgumentOutOfRangeException("ProjectTo", $"Unknown projection type '{projectionType}'.");
        }

        writer.WriteClosingBracket(); // Method closing bracket

        return writer;
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
            { Parameters.Length: 2 } when mapping.AfterMapMethod.Parameters[0] == mapping.Source.ToFullyQualifiedName() => writer
                .WriteLine($"{mapping.AfterMapMethod.MethodFullName}({mapping.Source.Name.ToParameterNameCasing()}, target);").WriteLine(),
            { Parameters.Length: 2 } when mapping.AfterMapMethod.Parameters[1] == mapping.Source.ToFullyQualifiedName() => writer
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

            if (mapping.Constructor.HasParameterWithDefaultValue)
            {
                writer.Write(parameter.Name).Write(": ");
            }

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

    private static CodeWriter WriteParameterNullCheckIf(this CodeWriter writer, bool condition, string parameterName)
    {
        if (condition)
        {
            writer
                .Write("if (").WriteIsNullCheck(parameterName).WriteLine(")")
                .WriteOpeningBracket()
                .WriteLine("return null;")
                .WriteClosingBracket();
        }

        return writer;
    }

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