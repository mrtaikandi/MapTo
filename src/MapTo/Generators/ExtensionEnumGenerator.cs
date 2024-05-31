// using MapTo.Extensions;
// using MapTo.Mappings;
//
// namespace MapTo.Generators;
//
// internal readonly record struct ExtensionEnumGenerator(
//     CompilerOptions CompilerOptions,
//     TargetMapping TargetMapping) : ICodeGenerator
// {
//     public void BeginWrite(CodeWriter writer)
//     {
//         if (!TargetMapping.TypeKeyword.Equals("enum", StringComparison.OrdinalIgnoreCase))
//         {
//             return;
//         }
//
//         writer
//             .WriteGeneratedCodeAttribute()
//             .WriteExtensionClassDefinition(TargetMapping)
//             .WriteOpeningBracket()
//             .WriteMapExtensionMethod(TargetMapping, CompilerOptions)
//             .WriteClosingBracket();
//     }
// }
//
// static file class ExtensionClassGeneratorExtensions
// {
//     internal static CodeWriter WriteExtensionClassDefinition(this CodeWriter writer, TargetMapping mapping) =>
//         writer.WriteLine($"public static partial class {mapping.ExtensionClassName}");
//
//     internal static CodeWriter WriteMapExtensionMethod(this CodeWriter writer, TargetMapping mapping, CompilerOptions compilerOptions)
//     {
//         var parameterName = mapping.Source.Name.ToParameterNameCasing();
//         writer
//             .WriteReturnNotNullIfNotNullAttributeIfRequired(mapping, compilerOptions)
//             .Write(mapping.Modifier.ToLowercaseString())
//             .Write(" static ")
//             .Write(mapping.GetReturnType())
//             .Write(compilerOptions.NullableReferenceSyntax)
//             .WriteWhitespace()
//             .Write($"{mapping.Options.MapMethodPrefix}{mapping.Name}")
//             .WriteOpenParenthesis()
//             .WriteAllowNullAttributeIf(compilerOptions is { NullableStaticAnalysis: true, NullableReferenceTypes: false })
//             .Write("this ")
//             .Write(mapping.GetSourceType())
//             .Write(compilerOptions.NullableReferenceSyntax)
//             .WriteWhitespace()
//             .Write(parameterName)
//             .WriteClosingParenthesis()
//             .WriteOpeningBracket() // Method opening bracket
//             .WriteParameterNullCheck(mapping.Source.Name.ToParameterNameCasing())
//             .WriteLine()
//             .WriteLine($"return {parameterName} switch")
//             .WriteOpeningBracket();
//
//         foreach (var property in mapping.Properties)
//         {
//             var sourceType = property.SourceType.FullName;
//             var propertyType = property.TypeName;
//
//             foreach (var member in property.TypeConverter.EnumMapping.Mappings)
//             {
//                 writer.Write("global::").Write(member.Source).Write(" => ").Write("global::").Write(member.Target).WriteLine(",");
//             }
//
//             if (property.TypeConverter.EnumMapping.FallBackValue is not null)
//             {
//                 writer.Write("_ => ").Write("global::").WriteLine(property.TypeConverter.EnumMapping.FallBackValue);
//             }
//             else
//             {
//                 writer
//                     .Write("_ => ")
//                     .WriteThrowArgumentOutOfRangeException("source", $"\"Unable to map enum value '{property.SourceType.QualifiedName}' to '{property.Type.QualifiedName}'.\"")
//                     .WriteLineIndented();
//             }
//         }
//
//         return writer
//             .WriteClosingBracket(false).WriteLine(";")
//             .WriteClosingBracket(); // Method closing bracket
//     }
//
//     private static CodeWriter WriteParameterNullCheck(this CodeWriter writer, string parameterName) => writer
//         .Write("if (").WriteIsNullCheck(parameterName).WriteLine(")")
//         .WriteOpeningBracket()
//         .WriteLine("return null;")
//         .WriteClosingBracket();
// }