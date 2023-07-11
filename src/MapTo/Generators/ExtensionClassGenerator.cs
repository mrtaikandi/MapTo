using MapTo.Configuration;
using MapTo.Extensions;
using MapTo.Mappings;

namespace MapTo.Generators;

internal readonly record struct ExtensionClassGenerator(
    CodeGeneratorOptions Configuration,
    CompilerOptions CompilerOptions,
    TargetMapping TargetMapping) : ICodeGenerator
{
    /// <inheritdoc />
    public void BeginWrite(CodeWriter writer)
    {
        writer
            .WriteCodeGeneratorAttribute()
            .WriteExtensionClassDefinition(TargetMapping)
            .WriteOpeningBracket() // Class opening bracket
            .WriteExtensionMethodAttribute(TargetMapping, CompilerOptions)
            .WriteExtensionMethodDefinition(TargetMapping, Configuration, CompilerOptions)
            .WriteOpeningBracket() // Method opening bracket
            .WriteExtensionMethodBody(TargetMapping)
            .WriteClosingBracket() // Method closing bracket
            .WriteClosingBracket(); // Class closing bracket
    }
}

internal static class ExtensionClassGeneratorExtensions
{
    public static CodeWriter WriteSourceParameterNullCheck(this CodeWriter writer, TargetMapping mapping)
    {
        var parameterName = mapping.Source.Name.ToParameterNameCasing();
        return writer
            .WriteLine($"if (ReferenceEquals({parameterName}, null))")
            .WriteOpeningBracket()
            .WriteLine("return null;")
            .WriteClosingBracket();
    }

    internal static CodeWriter WriteExtensionClassDefinition(this CodeWriter writer, TargetMapping mapping) =>
        writer.WriteLine($"public static class {mapping.Source.Name}MapToExtensions");

    internal static CodeWriter WriteExtensionMethodAttribute(this CodeWriter writer, TargetMapping mapping, CompilerOptions options) =>
        options.NullableStaticAnalysis || options.NullableReferenceTypes ? writer.WriteReturnNotNullIfNotNullAttribute(mapping.Source.Name.ToParameterNameCasing()) : writer;

    internal static CodeWriter WriteExtensionMethodBody(this CodeWriter writer, TargetMapping mapping) => writer
        .WriteSourceParameterNullCheck(mapping)
        .WriteLine()
        .Write("return ")
        .WriteConstructorInitializer(mapping)
        .WriteObjectInitializer(mapping);

    internal static CodeWriter WriteExtensionMethodDefinition(this CodeWriter writer, TargetMapping mapping, CodeGeneratorOptions options, CompilerOptions compilerOptions)
    {
        var returnType = mapping.GetReturnType();
        var methodName = $"{options.MapMethodPrefix}{mapping.Name}";
        var sourceType = mapping.GetSourceType();
        var parameterName = mapping.Source.Name.ToParameterNameCasing();

        return writer
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
            .WriteClosingParenthesis();
    }

    private static CodeWriter WriteConstructorInitializer(this CodeWriter writer, TargetMapping mapping)
    {
        var sourceType = mapping.Source.Name;
        var parameterName = sourceType.ToParameterNameCasing();
        var properties = mapping.Properties.Where(p => p.InitializationMode == PropertyInitializationMode.Constructor);

        if (mapping.Constructor is { IsGenerated: false, HasArguments: false })
        {
            return writer.WriteLine($"new {mapping.Name}");
        }

        return writer
            .Write($"new {mapping.Name}(")
            .WriteJoin(", ", properties.Select(p => $"{parameterName}.{p.Name}"))
            .WriteLine(")");
    }

    private static CodeWriter WriteObjectInitializer(this CodeWriter writer, TargetMapping mapping)
    {
        var sourceType = mapping.Source.Name;
        var parameterName = sourceType.ToParameterNameCasing();
        var properties = mapping.Properties.Where(p => p.InitializationMode == PropertyInitializationMode.ObjectInitializer);

        return writer
            .WriteOpeningBracket()
            .WriteLineJoin(",", properties.Select(p => $"{p.Name} = {parameterName}.{p.Name}"))
            .WriteClosingBracket(false).WriteLine(";");
    }
}