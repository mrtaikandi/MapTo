using MapTo.Extensions;
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
                .WriteUsings(model.Usings)
                .WriteLine()

                // Namespace declaration
                .WriteLine($"namespace {model.Namespace}")
                .WriteOpeningBracket()

                // Class declaration
                .WriteLine($"partial class {model.TypeIdentifierName}")
                .WriteOpeningBracket();

                // Class body
                if (model.GenerateSecondaryConstructor)
                {
                    builder
                        .GenerateSecondaryConstructor(model)
                        .WriteLine();
                }

                builder
                    .GeneratePrivateConstructor(model)
                    .WriteLine()
                    .GenerateFactoryMethod(model)

                    // End class declaration
                    .WriteClosingBracket()
                    .WriteLine()

                    // Extension class declaration
                    .GenerateSourceTypeExtensionClass(model)

                    // End namespace declaration
                    .WriteClosingBracket();
            
            return new(builder.ToString(), $"{model.Namespace}.{model.TypeIdentifierName}.g.cs");
        }

        private static SourceBuilder GenerateSecondaryConstructor(this SourceBuilder builder, MappingModel model)
        {
            var sourceClassParameterName = model.SourceTypeIdentifierName.ToCamelCase();

            if (model.Options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine($"/// Initializes a new instance of the <see cref=\"{model.TypeIdentifierName}\"/> class")
                    .WriteLine($"/// using the property values from the specified <paramref name=\"{sourceClassParameterName}\"/>.")
                    .WriteLine("/// </summary>")
                    .WriteLine($"/// <exception cref=\"ArgumentNullException\">{sourceClassParameterName} is null</exception>");
            }

            return builder
                .WriteLine($"{model.Options.ConstructorAccessModifier.ToLowercaseString()} {model.TypeIdentifierName}({model.SourceType} {sourceClassParameterName})")
                .WriteLine($"    : this(new {MappingContextSource.ClassName}(), {sourceClassParameterName}) {{ }}");
        }
        
        private static SourceBuilder GeneratePrivateConstructor(this SourceBuilder builder, MappingModel model)
        {
            var sourceClassParameterName = model.SourceTypeIdentifierName.ToCamelCase();
            const string mappingContextParameterName = "context";

            var baseConstructor = model.HasMappedBaseClass ? $" : base({mappingContextParameterName}, {sourceClassParameterName})" : string.Empty;

            builder
                .WriteLine($"private protected {model.TypeIdentifierName}({MappingContextSource.ClassName} {mappingContextParameterName}, {model.SourceType} {sourceClassParameterName}){baseConstructor}")
                .WriteOpeningBracket()
                .WriteLine($"if ({mappingContextParameterName} == null) throw new ArgumentNullException(nameof({mappingContextParameterName}));")
                .WriteLine($"if ({sourceClassParameterName} == null) throw new ArgumentNullException(nameof({sourceClassParameterName}));")
                .WriteLine()
                .WriteLine($"{mappingContextParameterName}.{MappingContextSource.RegisterMethodName}({sourceClassParameterName}, this);")
                .WriteLine();

            foreach (var property in model.MappedProperties)
            {
                if (property.TypeConverter is null)
                {
                    if (property.IsEnumerable)
                    {
                        builder.WriteLine($"{property.Name} = {sourceClassParameterName}.{property.SourcePropertyName}.Select({mappingContextParameterName}.{MappingContextSource.MapMethodName}<{property.MappedSourcePropertyTypeName}, {property.EnumerableTypeArgument}>).ToList();");
                    }
                    else
                    {
                        builder.WriteLine(property.MappedSourcePropertyTypeName is null 
                            ? $"{property.Name} = {sourceClassParameterName}.{property.SourcePropertyName};"
                            : $"{property.Name} = {mappingContextParameterName}.{MappingContextSource.MapMethodName}<{property.MappedSourcePropertyTypeName}, {property.Type}>({sourceClassParameterName}.{property.SourcePropertyName});");
                    }
                }
                else
                {
                    var parameters = property.TypeConverterParameters.IsEmpty
                        ? "null"
                        : $"new object[] {{ {string.Join(", ", property.TypeConverterParameters)} }}";

                    builder.WriteLine($"{property.Name} = new {property.TypeConverter}().Convert({sourceClassParameterName}.{property.SourcePropertyName}, {parameters});");
                }
            }

            // End constructor declaration
            return builder.WriteClosingBracket();
        }
        
        private static SourceBuilder GenerateFactoryMethod(this SourceBuilder builder, MappingModel model)
        {
            var sourceClassParameterName = model.SourceTypeIdentifierName.ToCamelCase();

            return builder
                .GenerateConvertorMethodsXmlDocs(model, sourceClassParameterName)
                .WriteLineIf(model.Options.SupportNullableStaticAnalysis, $"[return: NotNullIfNotNull(\"{sourceClassParameterName}\")]")
                .WriteLine($"{model.Options.GeneratedMethodsAccessModifier.ToLowercaseString()} static {model.TypeIdentifierName}{model.Options.NullableReferenceSyntax} From({model.SourceType}{model.Options.NullableReferenceSyntax} {sourceClassParameterName})")
                .WriteOpeningBracket()
                .WriteLine($"return {sourceClassParameterName} == null ? null : {MappingContextSource.ClassName}.{MappingContextSource.FactoryMethodName}<{model.SourceType}, {model.TypeIdentifierName}>({sourceClassParameterName});")
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
                .WriteLine($"/// Creates a new instance of <see cref=\"{model.TypeIdentifierName}\"/> and sets its participating properties")
                .WriteLine($"/// using the property values from <paramref name=\"{sourceClassParameterName}\"/>.")
                .WriteLine("/// </summary>")
                .WriteLine($"/// <param name=\"{sourceClassParameterName}\">The instance of <see cref=\"{model.SourceType}\"/> to use as source.</param>")
                .WriteLine($"/// <returns>A new instance of <see cred=\"{model.TypeIdentifierName}\"/> -or- <c>null</c> if <paramref name=\"{sourceClassParameterName}\"/> is <c>null</c>.</returns>");
        }

        private static SourceBuilder GenerateSourceTypeExtensionClass(this SourceBuilder builder, MappingModel model)
        {
            return builder
                .WriteLine($"{model.Options.GeneratedMethodsAccessModifier.ToLowercaseString()} static partial class {model.SourceTypeIdentifierName}To{model.TypeIdentifierName}Extensions")
                .WriteOpeningBracket()
                .GenerateSourceTypeExtensionMethod(model)
                .WriteClosingBracket();
        }
        
        private static SourceBuilder GenerateSourceTypeExtensionMethod(this SourceBuilder builder, MappingModel model)
        {
            var sourceClassParameterName = model.SourceTypeIdentifierName.ToCamelCase();

            return builder
                .GenerateConvertorMethodsXmlDocs(model, sourceClassParameterName)
                .WriteLineIf(model.Options.SupportNullableStaticAnalysis, $"[return: NotNullIfNotNull(\"{sourceClassParameterName}\")]")
                .WriteLine($"{model.Options.GeneratedMethodsAccessModifier.ToLowercaseString()} static {model.TypeIdentifierName}{model.Options.NullableReferenceSyntax} To{model.TypeIdentifierName}(this {model.SourceType}{model.Options.NullableReferenceSyntax} {sourceClassParameterName})")
                .WriteOpeningBracket()
                .WriteLine($"return {sourceClassParameterName} == null ? null : new {model.TypeIdentifierName}({sourceClassParameterName});")
                .WriteClosingBracket();
        }
    }
}