﻿using MapTo.Extensions;
using static MapTo.Sources.Constants;

namespace MapTo.Sources
{
    internal static class MapClassSource
    {
        internal static SourceCode Generate(MappingModel model)
        {
            using var builder = new SourceBuilder()
                .WriteLine(GeneratedFilesHeader)
                .WriteNullableContextOptionIf(model.Options.SupportNullableReferenceTypes)
                .WriteLine()
                .WriteUsings(model)
                .WriteLine()

                // Namespace declaration
                .WriteLine($"namespace {model.Namespace}")
                .WriteOpeningBracket()

                // Class declaration
                .WriteLine($"partial class {model.ClassName}")
                .WriteOpeningBracket()

                // Class body
                .GenerateConstructor(model)
                .WriteLine()
                .GenerateFactoryMethod(model)

                // End class declaration
                .WriteClosingBracket()
                .WriteLine()

                // Extension class declaration
                .GenerateSourceTypeExtensionClass(model)

                // End namespace declaration
                .WriteClosingBracket();
            
            return new(builder.ToString(), $"{model.ClassName}.g.cs");
        }

        private static SourceBuilder WriteUsings(this SourceBuilder builder, MappingModel model)
        {
            return builder
                .WriteLine("using System;");
        }
        
        private static SourceBuilder GenerateConstructor(this SourceBuilder builder, MappingModel model)
        {
            var sourceClassParameterName = model.SourceClassName.ToCamelCase();

            if (model.Options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine($"/// Initializes a new instance of the <see cref=\"{model.ClassName}\"/> class")
                    .WriteLine($"/// using the property values from the specified <paramref name=\"{sourceClassParameterName}\"/>.")
                    .WriteLine("/// </summary>")
                    .WriteLine($"/// <exception cref=\"ArgumentNullException\">{sourceClassParameterName} is null</exception>");
            }

            builder
                .WriteLine($"{model.Options.ConstructorAccessModifier.ToLowercaseString()} {model.ClassName}({model.SourceClassFullName} {sourceClassParameterName})")
                .WriteOpeningBracket()
                .WriteLine($"if ({sourceClassParameterName} == null) throw new ArgumentNullException(nameof({sourceClassParameterName}));")
                .WriteLine();

            foreach (var property in model.MappedProperties)
            {
                if (property.TypeConverter is null)
                {
                    builder.WriteLine(property.MappedSourcePropertyTypeName is null 
                        ? $"{property.Name} = {sourceClassParameterName}.{property.SourcePropertyName};" 
                        : $"{property.Name} = new {property.Type}({sourceClassParameterName}.{property.SourcePropertyName});");
                }
                else
                {
                    var parameters = property.TypeConverterParameters.IsEmpty
                        ? "null"
                        : $"new object[] {{ {string.Join(", ", property.TypeConverterParameters)} }}";

                    builder.WriteLine($"{property.Name} = new {property.TypeConverter}().Convert({sourceClassParameterName}.{property.Name}, {parameters});");
                }
            }

            // End constructor declaration
            return builder.WriteClosingBracket();
        }
        
        private static SourceBuilder GenerateFactoryMethod(this SourceBuilder builder, MappingModel model)
        {
            var sourceClassParameterName = model.SourceClassName.ToCamelCase();

            return builder
                .GenerateConvertorMethodsXmlDocs(model, sourceClassParameterName)
                .WriteLine($"{model.Options.GeneratedMethodsAccessModifier.ToLowercaseString()} static {model.ClassName} From({model.SourceClassFullName}{model.Options.NullableReferenceSyntax} {sourceClassParameterName})")
                .WriteOpeningBracket()
                .WriteLine($"return {sourceClassParameterName} == null ? null : new {model.ClassName}({sourceClassParameterName});")
                .WriteClosingBracket();
        }
        
        private static SourceBuilder GenerateConvertorMethodsXmlDocs(this SourceBuilder builder, MappingModel model, string sourceClassParameterName)
        {
            if (!model.Options.GenerateXmlDocument)
            {
                return builder;
            }

            return builder
                .WriteLine("/// <summary>")
                .WriteLine($"/// Creates a new instance of <see cref=\"{model.ClassName}\"/> and sets its participating properties")
                .WriteLine($"/// using the property values from <paramref name=\"{sourceClassParameterName}\"/>.")
                .WriteLine("/// </summary>")
                .WriteLine($"/// <param name=\"{sourceClassParameterName}\">Instance of <see cref=\"{model.SourceClassName}\"/> to use as source.</param>")
                .WriteLine($"/// <returns>A new instance of <see cred=\"{model.ClassName}\"/> -or- <c>null</c> if <paramref name=\"{sourceClassParameterName}\"/> is <c>null</c>.</returns>");
        }

        private static SourceBuilder GenerateSourceTypeExtensionClass(this SourceBuilder builder, MappingModel model)
        {
            return builder
                .WriteLine($"{model.Options.GeneratedMethodsAccessModifier.ToLowercaseString()} static partial class {model.SourceClassName}To{model.ClassName}Extensions")
                .WriteOpeningBracket()
                .GenerateSourceTypeExtensionMethod(model)
                .WriteClosingBracket();
        }
        
        private static SourceBuilder GenerateSourceTypeExtensionMethod(this SourceBuilder builder, MappingModel model)
        {
            var sourceClassParameterName = model.SourceClassName.ToCamelCase();

            return builder
                .GenerateConvertorMethodsXmlDocs(model, sourceClassParameterName)
                .WriteLine($"{model.Options.GeneratedMethodsAccessModifier.ToLowercaseString()} static {model.ClassName} To{model.ClassName}(this {model.SourceClassFullName}{model.Options.NullableReferenceSyntax} {sourceClassParameterName})")
                .WriteOpeningBracket()
                .WriteLine($"return {sourceClassParameterName} == null ? null : new {model.ClassName}({sourceClassParameterName});")
                .WriteClosingBracket();
        }
    }
}