using MapTo.Models;
using static MapTo.Sources.Constants;

namespace MapTo.Sources
{
    internal static class MapPropertyAttributeSource
    {
        internal const string AttributeName = "MapProperty";

        internal static SourceCode Generate(SourceGenerationOptions options)
        {
            using var builder = new SourceBuilder()
                .WriteLine(GeneratedFilesHeader)
                .WriteLine("using System;")
                .WriteLine()
                .WriteLine($"namespace {RootNamespace}")
                .WriteOpeningBracket();

            if (options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine("/// Maps a property to property of another object.")
                    .WriteLine("/// </summary>");
            }

            builder
                .WriteLine("[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]")
                .WriteLine($"public sealed class {AttributeName}Attribute : Attribute")
                .WriteOpeningBracket();

            if (options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine("/// Initializes a new instance of <see cref=\"MapPropertyAttribute\"/>.")
                    .WriteLine("/// </summary>")
                    .WriteLine("/// <param name=\"converter\">The <see cref=\"ITypeConverter{TSource,TDestination}\" /> to convert the value of the annotated property.</param>");
            }

            builder
                .WriteLine($"public {AttributeName}Attribute(Type converter = null)")
                .WriteOpeningBracket()
                .WriteLine("Converter = converter;")
                .WriteClosingBracket()
                .WriteLine();

            if (options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine("/// Gets the <see cref=\"ITypeConverter{TSource,TDestination}\" /> to convert the value of the annotated property.")
                    .WriteLine("/// </summary>");
            }

            builder
                .WriteLine("public Type Converter { get; }")
                .WriteClosingBracket()
                .WriteClosingBracket();

            return new(builder.ToString(), $"{AttributeName}Attribute.g.cs");
        }
    }
}