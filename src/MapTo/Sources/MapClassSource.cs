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

                // Namespace declaration
                .WriteLine($"namespace {model.Namespace}")
                .WriteOpeningBracket()
                .WriteUsings(model.Usings)
                .WriteLine()

                // Class declaration
                .WriteLine($"partial class {model.ClassName}")
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
            
            return new(builder.ToString(), $"{model.ClassName}.g.cs");
        }

        private static SourceBuilder GenerateSecondaryConstructor(this SourceBuilder builder, MappingModel model)
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

            return builder
                .WriteLine($"{model.Options.ConstructorAccessModifier.ToLowercaseString()} {model.ClassName}({model.SourceClassName} {sourceClassParameterName})")
                .WriteLine($"    : this(new {MappingContextSource.ClassName}(), {sourceClassParameterName}) {{ }}");
        }
        
        private static SourceBuilder GeneratePrivateConstructor(this SourceBuilder builder, MappingModel model)
        {
            var sourceClassParameterName = model.SourceClassName.ToCamelCase();
            const string mappingContextParameterName = "context";

            var baseConstructor = model.HasMappedBaseClass ? $" : base({mappingContextParameterName}, {sourceClassParameterName})" : string.Empty;

            builder
                .WriteLine($"private protected {model.ClassName}({MappingContextSource.ClassName} {mappingContextParameterName}, {model.SourceClassName} {sourceClassParameterName}){baseConstructor}")
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
            var sourceClassParameterName = model.SourceClassName.ToCamelCase();

            return builder
                .GenerateConvertorMethodsXmlDocs(model, sourceClassParameterName)
                .WriteLineIf(model.Options.SupportNullableStaticAnalysis, $"[return: NotNullIfNotNull(\"{sourceClassParameterName}\")]")
                .WriteLine($"{model.Options.GeneratedMethodsAccessModifier.ToLowercaseString()} static {model.ClassName}{model.Options.NullableReferenceSyntax} From({model.SourceClassName}{model.Options.NullableReferenceSyntax} {sourceClassParameterName})")
                .WriteOpeningBracket()
                .WriteLine($"return {sourceClassParameterName} == null ? null : {MappingContextSource.ClassName}.{MappingContextSource.FactoryMethodName}<{model.SourceClassName}, {model.ClassName}>({sourceClassParameterName});")
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
                .WriteLine($"/// <param name=\"{sourceClassParameterName}\">The instance of <see cref=\"{model.SourceClassName}\"/> to use as source.</param>")
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
                .WriteLineIf(model.Options.SupportNullableStaticAnalysis, $"[return: NotNullIfNotNull(\"{sourceClassParameterName}\")]")
                .WriteLine($"{model.Options.GeneratedMethodsAccessModifier.ToLowercaseString()} static {model.ClassName}{model.Options.NullableReferenceSyntax} To{model.ClassName}(this {model.SourceClassName}{model.Options.NullableReferenceSyntax} {sourceClassParameterName})")
                .WriteOpeningBracket()
                .WriteLine($"return {sourceClassParameterName} == null ? null : new {model.ClassName}({sourceClassParameterName});")
                .WriteClosingBracket();
        }
    }
}