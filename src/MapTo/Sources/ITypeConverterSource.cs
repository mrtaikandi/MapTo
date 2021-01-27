using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using static MapTo.Sources.Constants;

namespace MapTo.Sources
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class ITypeConverterSource
    {
        internal const string InterfaceName = "ITypeConverter";
        internal const string FullyQualifiedName = RootNamespace + "." + InterfaceName + "`2";
        
        internal static SourceCode Generate(SourceGenerationOptions options)
        {
            using var builder = new SourceBuilder()
                .WriteLine(GeneratedFilesHeader)
                .WriteNullableContextOptionIf(options.SupportNullableReferenceTypes)
                .WriteLine()
                .WriteLine($"namespace {RootNamespace}")
                .WriteOpeningBracket();

            if (options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine("/// Converts the value of <typeparamref name=\"TSource\"/> <typeparamref name=\"TDestination\"/>.")
                    .WriteLine("/// </summary>")
                    .WriteLine("/// <typeparam name=\"TSource\">The type to convert from.</typeparam>")
                    .WriteLine("/// <typeparam name=\"TDestination\">The type to convert to.</typeparam>");
            }

            builder
                .WriteLine($"public interface {InterfaceName}<in TSource, out TDestination>")
                .WriteOpeningBracket();

            if (options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine("/// Converts the value of <paramref name=\"source\"/> object to <typeparamref name=\"TDestination\"/>.")
                    .WriteLine("/// </summary>")
                    .WriteLine("/// <param name=\"source\">The <see cref=\"TSource\"/> to convert.</param>")
                    .WriteLine($"/// <param name=\"converterParameters\">The parameter list passed to the <see cref=\"{MapTypeConverterAttributeSource.AttributeClassName}\"/></param>")
                    .WriteLine("/// <returns><typeparamref name=\"TDestination\"/> object.</returns>");
            }

            builder
                .WriteLine($"TDestination Convert(TSource source, object[]{options.NullableReferenceSyntax} converterParameters);")
                .WriteClosingBracket()
                .WriteClosingBracket();

            return new(builder.ToString(), $"{InterfaceName}.g.cs");
        }

        internal static string GetFullyQualifiedName(ITypeSymbol sourceType, ITypeSymbol destinationType) =>
            $"{RootNamespace}.{InterfaceName}<{sourceType.ToDisplayString()}, {destinationType.ToDisplayString()}>";
    }
}