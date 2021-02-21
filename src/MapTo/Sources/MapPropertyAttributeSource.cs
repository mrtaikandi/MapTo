using static MapTo.Sources.Constants;

namespace MapTo.Sources
{
    internal static class MapPropertyAttributeSource
    {
        internal const string AttributeName = "MapProperty";
        internal const string AttributeClassName = AttributeName + "Attribute";
        internal const string FullyQualifiedName = RootNamespace + "." + AttributeClassName;
        internal const string SourcePropertyNamePropertyName = "SourcePropertyName";

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
                    .WriteLine("/// Specifies the mapping behavior of the annotated property.")
                    .WriteLine("/// </summary>")
                    .WriteLine("/// <remarks>")
                    .WriteLine($"/// {AttributeClassName} has a number of uses:")
                    .WriteLine("/// <list type=\"bullet\">")
                    .WriteLine("/// <item><description>By default properties with same name will get mapped. This attribute allows the names to be different.</description></item>")
                    .WriteLine("/// <item><description>Indicates that a property should be mapped when member serialization is set to opt-in.</description></item>")
                    .WriteLine("/// </list>")
                    .WriteLine("/// </remarks>");
            }
            
            builder
                .WriteLine("[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]")
                .WriteLine($"public sealed class {AttributeClassName} : Attribute")
                .WriteOpeningBracket();
         
            if (options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine("/// Gets or sets the property name of the object to mapping from.")
                    .WriteLine("/// </summary>");
            }

            builder
                .WriteLine($"public string{options.NullableReferenceSyntax} {SourcePropertyNamePropertyName} {{ get; set; }}")
                .WriteClosingBracket() // class
                .WriteClosingBracket(); // namespace

            
            return new(builder.ToString(), $"{AttributeClassName}.g.cs");
        }
    }
}