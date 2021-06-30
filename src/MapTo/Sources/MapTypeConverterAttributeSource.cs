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
        internal const string ConverterParametersPropertyName = "ConverterParameters";

        internal static SourceCode Generate(SourceGenerationOptions options)
        {   
            using var builder = new SourceBuilder()
                .WriteLine(GeneratedFilesHeader)
                .WriteNullableContextOptionIf(options.SupportNullableReferenceTypes)
                .WriteLine()
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
                .WriteLine("[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]")
                .WriteLine($"public sealed class {AttributeClassName} : Attribute")
                .WriteOpeningBracket();

            if (options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine($"/// Initializes a new instance of <see cref=\"{AttributeClassName}\"/>.")
                    .WriteLine("/// </summary>")
                    .WriteLine($"/// <param name=\"converter\">The <see cref=\"{ITypeConverterSource.InterfaceName}{{TSource,TDestination}}\" /> to be used to convert the source type.</param>")
                    .WriteLine("/// <param name=\"converterParameters\">The list of parameters to pass to the <paramref name=\"converter\"/> during the type conversion.</param>");
            }

            builder
                .WriteLine($"public {AttributeClassName}(Type converter, object[]{options.NullableReferenceSyntax} converterParameters = null)")
                .WriteOpeningBracket()
                .WriteLine($"{ConverterPropertyName} = converter;")
                .WriteLine($"{ConverterParametersPropertyName} = converterParameters;")
                .WriteClosingBracket()
                .WriteLine();

            if (options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine($"/// Gets or sets the <see cref=\"{ITypeConverterSource.InterfaceName}{{TSource,TDestination}}\" /> to be used to convert the source type.")
                    .WriteLine("/// </summary>");
            }

            builder
                .WriteLine($"public Type {ConverterPropertyName} {{ get; }}")
                .WriteLine();

            if (options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine($"/// Gets the list of parameters to pass to the <see cref=\"{ConverterPropertyName}\"/> during the type conversion.")
                    .WriteLine("/// </summary>");
            }

            builder
                .WriteLine($"public object[]{options.NullableReferenceSyntax} {ConverterParametersPropertyName} {{ get; }}")
                .WriteClosingBracket()
                .WriteClosingBracket();

            return new(builder.ToString(), $"{AttributeClassName}.g.cs");
        }
    }
}