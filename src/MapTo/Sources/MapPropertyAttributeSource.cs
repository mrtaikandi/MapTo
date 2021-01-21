using System;
using static MapTo.Sources.Constants;

namespace MapTo.Sources
{
    internal static class MapPropertyAttributeSource
    {
        internal const string AttributeName = "MapProperty";
        internal const string FullyQualifiedName = RootNamespace + "." + AttributeName + "Attribute";
        internal const string ConverterPropertyName = "Converter";

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
                    .WriteLine("/// </summary>");
            }

            builder
                .WriteLine($"public {AttributeName}Attribute() {{ }}")
                .WriteLine();

            if (options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine("/// Gets or sets the <see cref=\"ITypeConverter{TSource,TDestination}\" /> to be used to convert the source type.")
                    .WriteLine("/// </summary>");
            }

            builder
                .WriteLine($"public Type {ConverterPropertyName} {{ get; set; }}")
                .WriteClosingBracket()
                .WriteClosingBracket();

            return new(builder.ToString(), $"{AttributeName}Attribute.g.cs");
        }
    }
}