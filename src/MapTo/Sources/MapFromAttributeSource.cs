using MapTo.Models;
using static MapTo.Sources.Constants;

namespace MapTo.Sources
{
    internal static class MapFromAttributeSource
    {
        internal const string AttributeName = "MapFrom";
        
        internal static Source Generate(SourceGenerationOptions options)
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
                    .WriteLine("/// Specifies that the annotated class can be mapped from the provided <see cref=\"SourceType\"/>.")
                    .WriteLine("/// </summary>");
            }

            builder
                .WriteLine("[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]")
                .WriteLine($"public sealed class {AttributeName}Attribute : Attribute")
                .WriteOpeningBracket();

            if (options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine($"/// Initializes a new instance of the <see cref=\"{AttributeName}Attribute\"/> class with the specified <paramref name=\"sourceType\"/>.")
                    .WriteLine("/// </summary>");
            }

            builder
                .WriteLine($"public {AttributeName}Attribute(Type sourceType)")
                .WriteOpeningBracket()
                .WriteLine("SourceType = sourceType;")
                .WriteClosingBracket()
                .WriteLine();

            if (options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine("/// Gets the type of the class that the annotated class should be able to map from.")
                    .WriteLine("/// </summary>");
            }

            builder
                .WriteLine("public Type SourceType { get; }")
                .WriteClosingBracket() // class
                .WriteClosingBracket(); // namespace

            return new(builder.ToString(), $"{AttributeName}Attribute.g.cs");
        }
    }
}