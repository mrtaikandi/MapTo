using System.Diagnostics;
using MapTo.Extensions;
using MapTo.Mappings;

namespace MapTo.Generators;

internal readonly record struct ExtensionEnumGenerator(
    CompilerOptions CompilerOptions,
    TargetMapping TargetMapping) : ICodeGenerator
{
    public void BeginWrite(CodeWriter writer)
    {
        if (TargetMapping.TypeKind is not TypeKind.Enum || TargetMapping.TypeConverter is null)
        {
            return;
        }

        writer
            .WriteGeneratedCodeAttribute()
            .WriteExtensionClassDefinition(TargetMapping)
            .WriteOpeningBracket()
            .WriteMapExtensionMethod(TargetMapping, CompilerOptions)
            .WriteNonNullableMapExtensionMethod(TargetMapping, CompilerOptions)
            .WriteClosingBracket();
    }
}

static file class ExtensionClassGeneratorExtensions
{
    internal static CodeWriter WriteExtensionClassDefinition(this CodeWriter writer, TargetMapping mapping) =>
        writer.WriteLine($"public static partial class {mapping.ExtensionClassName}");

    internal static CodeWriter WriteMapExtensionMethod(this CodeWriter writer, TargetMapping mapping, CompilerOptions compilerOptions)
    {
        Debug.Assert(mapping.TypeConverter != null, "mapping.TypeConverter != null");

        var parameterName = mapping.Source.Name.ToParameterNameCasing();
        var typeConverter = mapping.TypeConverter!.Value;

        writer
            .WriteReturnNotNullIfNotNullAttributeIfRequired(mapping, compilerOptions)
            .Write(mapping.Modifier.ToLowercaseString())
            .Write(" static ")
            .Write(mapping.GetReturnType())
            .Write(compilerOptions.NullableReferenceSyntax)
            .WriteWhitespace()
            .Write($"{mapping.Options.MapMethodPrefix}{mapping.Name}")
            .WriteOpenParenthesis()
            .WriteAllowNullAttributeIf(compilerOptions is { NullableStaticAnalysis: true, NullableReferenceTypes: false })
            .Write("this ")
            .Write(mapping.GetSourceType())
            .Write(compilerOptions.NullableReferenceSyntax)
            .WriteWhitespace()
            .Write(parameterName)
            .WriteClosingParenthesis()
            .WriteOpeningBracket(); // Method opening bracket

        if (typeConverter.EnumMapping.Strategy is EnumMappingStrategy.ByValue)
        {
            writer
                .Write("return ")
                .WriteTernaryIsNullCheck(parameterName, typeConverter.EnumMapping.FallBackValue ?? "null", $"({mapping.GetReturnType()}){parameterName}")
                .WriteLine(";");
        }
        else
        {
            writer
                .WriteParameterNullCheck(mapping.Source.Name.ToParameterNameCasing())
                .WriteLine()
                .WriteLine($"return {parameterName} switch")
                .WriteOpeningBracket();

            foreach (var member in typeConverter.EnumMapping.Mappings)
            {
                writer.Write("global::").Write(member.Source).Write(" => ").Write("global::").Write(member.Target).WriteLine(",");
            }

            if (typeConverter.EnumMapping.FallBackValue is not null)
            {
                writer.Write("_ => ").Write("global::").WriteLine(typeConverter.EnumMapping.FallBackValue);
            }
            else
            {
                writer
                    .Write("_ => ")
                    .WriteThrowArgumentOutOfRangeException(parameterName, $"\"Unable to map enum '{mapping.Source.ToFullName()}' to '{mapping.GetFullName()}'.\"")
                    .WriteLineIndented();
            }

            writer.WriteClosingBracket(false).WriteLine(";"); // Switch expression closing bracket
        }

        return writer
            .WriteClosingBracket() // Method closing bracket
            .WriteLine();
    }

    internal static CodeWriter WriteNonNullableMapExtensionMethod(this CodeWriter writer, TargetMapping mapping, CompilerOptions compilerOptions)
    {
        Debug.Assert(mapping.TypeConverter != null, "mapping.TypeConverter != null");

        var parameterName = mapping.Source.Name.ToParameterNameCasing();
        var typeConverter = mapping.TypeConverter!.Value;

        writer
            .Write(mapping.Modifier.ToLowercaseString())
            .Write(" static ")
            .Write(mapping.GetReturnType())
            .WriteWhitespace()
            .Write($"{mapping.Options.MapMethodPrefix}{mapping.Name}")
            .WriteOpenParenthesis()
            .Write("this ")
            .Write(mapping.GetSourceType())
            .WriteWhitespace()
            .Write(parameterName)
            .WriteClosingParenthesis()
            .WriteOpeningBracket(); // Method opening bracket

        if (typeConverter.EnumMapping.Strategy is EnumMappingStrategy.ByValue)
        {
            writer.Write($"return ({mapping.GetReturnType()}){parameterName};");
        }
        else
        {
            writer
                .WriteLine($"return {parameterName} switch")
                .WriteOpeningBracket();

            foreach (var member in typeConverter.EnumMapping.Mappings)
            {
                writer.Write("global::").Write(member.Source).Write(" => ").Write("global::").Write(member.Target).WriteLine(",");
            }

            if (typeConverter.EnumMapping.FallBackValue is not null)
            {
                writer.Write("_ => ").Write("global::").WriteLine(typeConverter.EnumMapping.FallBackValue);
            }
            else
            {
                writer
                    .Write("_ => ")
                    .WriteThrowArgumentOutOfRangeException(parameterName, $"\"Unable to map enum '{mapping.Source.ToFullName()}' to '{mapping.GetFullName()}'.\"")
                    .WriteLineIndented();
            }

            writer.WriteClosingBracket(false).WriteLine(";"); // Switch expression closing bracket
        }

        return writer.WriteClosingBracket(); // Method closing bracket
    }

    private static CodeWriter WriteParameterNullCheck(this CodeWriter writer, string parameterName) => writer
        .Write("if (").WriteIsNullCheck(parameterName).WriteLine(")")
        .WriteOpeningBracket()
        .WriteLine("return null;")
        .WriteClosingBracket();
}