using MapTo.Models;
using static MapTo.Sources.Constants;

namespace MapTo.Sources
{
    internal class TypeConverterSource
    {
        internal const string InterfaceName = "ITypeConverter";

        internal static Source Generate(SourceGenerationOptions options)
        {
            using var builder = new SourceBuilder()
                .WriteLine(GeneratedFilesHeader)
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
                    .WriteLine("/// Converts the value of <paramref name=\"value\"/> object to <typeparamref name=\"TDestination\"/>.")
                    .WriteLine("/// </summary>")
                    .WriteLine("/// <param name=\"value\">The object to convert.</param>")
                    .WriteLine("/// <returns><typeparamref name=\"TDestination\"/> object.</returns>");
            }

            builder
                .WriteLine("TDestination Convert(TSource source);")
                .WriteClosingBracket()
                .WriteClosingBracket();

            return new(builder.ToString(), $"{InterfaceName}.g.cs");
        }
    }
}