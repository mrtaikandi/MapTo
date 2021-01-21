using System;
using static MapTo.Sources.Constants;

namespace MapTo.Sources
{
    internal static class MapTypeConverterAttributeSource
    {
        internal const string AttributeName = "MapTypeConverter";
        internal const string AttributeClassName = AttributeName + "Attribute";
        internal const string FullyQualifiedName = RootNamespace + "." + AttributeClassName;
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
                    .WriteLine("/// Specifies what type to use as a converter for the property this attribute is bound to.")
                    .WriteLine("/// </summary>");
            }

            builder
                .WriteLine("[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]")
                .WriteLine($"public sealed class {AttributeClassName} : Attribute")
                .WriteOpeningBracket();

            if (options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine($"/// Initializes a new instance of <see cref=\"{AttributeClassName}\"/>.")
                    .WriteLine("/// </summary>");
            }

            builder
                .WriteLine($"public {AttributeClassName}(Type converter)")
                .WriteOpeningBracket()
                .WriteLine($"{ConverterPropertyName} = converter;")
                .WriteClosingBracket()
                .WriteLine();

            if (options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine($"/// Gets or sets the <see cref=\"{TypeConverterSource.InterfaceName}{{TSource,TDestination}}\" /> to be used to convert the source type.")
                    .WriteLine("/// </summary>");
            }

            builder
                .WriteLine($"public Type {ConverterPropertyName} {{ get; }}")
                .WriteClosingBracket()
                .WriteClosingBracket();

            return new(builder.ToString(), $"{AttributeClassName}.g.cs");
        }
    }
}