using static MapTo.Sources.Constants;

namespace MapTo.Sources
{
    internal static class IgnorePropertyAttributeSource
    {
        internal const string AttributeName = "IgnoreProperty";
        internal const string AttributeClassName = AttributeName + "Attribute";
        internal const string FullyQualifiedName = RootNamespace + "." + AttributeClassName;
        internal const string SourceTypeName = "SourceTypeName";

        internal static SourceCode Generate(SourceGenerationOptions options)
        {
            var builder = new SourceBuilder()
                .WriteLine(GeneratedFilesHeader)
                .WriteNullableContextOptionIf(options.SupportNullableReferenceTypes)
                .WriteLine("using System;")
                .WriteLine()
                .WriteLine($"namespace {RootNamespace}")
                .WriteOpeningBracket();

            if (options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine("/// Specifies that the annotated property should be excluded.")
                    .WriteLine("/// </summary>");
            }

            builder
                .WriteLine("[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]")
                .WriteLine($"public sealed class {AttributeClassName} : Attribute")
                .WriteOpeningBracket();

            if (options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine("/// Gets or sets the Type name of the object to ignore.")
                    .WriteLine("/// </summary>");
            }

            builder
                .WriteLine($"public Type{options.NullableReferenceSyntax} {SourceTypeName} {{ get; set; }}")
                .WriteClosingBracket() // class
                .WriteClosingBracket(); //namespace

            return new(builder.ToString(), $"{AttributeClassName}.g.cs");
        }
    }
}