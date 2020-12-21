using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapTo.Extensions;
using MapTo.Models;
using Microsoft.CodeAnalysis;

namespace MapTo
{
    internal static class SourceBuilder
    {
        internal const string NamespaceName = "MapTo";
        internal const string MapFromAttributeName = "MapFrom";
        private const int Indent1 = 4; //"    ";
        private const int Indent2 = Indent1 * 2; // "        ";
        private const int Indent3 = Indent1 * 3; // "            ";

        internal static void AddMapToAttribute(this GeneratorExecutionContext context)
        {
            const string source = @"
using System;

namespace MapTo
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class MapFromAttribute : Attribute
    {
        public MapFromAttribute(Type sourceType)
        {
            SourceType = sourceType;
        }

        public Type SourceType { get; }
    }
}
";

            context.AddSource("MapFromAttribute.g.cs", source);
        }

        internal static (string source, string hintName) GenerateSource(MapModel model)
        {
            var builder = new StringBuilder();

            builder
                .AppendFileHeader()
                .GenerateUsings(model)

                // Namespace declaration
                .AppendFormat("namespace {0}", model.Namespace)
                .AppendOpeningBracket()

                // Class declaration
                .PadLeft(Indent1)
                .AppendFormat("{0} class {1}", model.ClassModifiers.ToFullString().Trim(), model.ClassName)
                .AppendOpeningBracket(Indent1)

                // Class body
                .GenerateConstructor(model, out var mappedProperties)
                .AppendLine()
                .GenerateFactoryMethod(model)

                // End class declaration
                .AppendClosingBracket(Indent1)

                // Extensions Class declaration
                .AppendLine()
                .AppendLine()
                .PadLeft(Indent1)
                .AppendFormat("{0} static partial class {1}Extensions", model.ClassModifiers.FirstOrDefault().ToFullString().Trim(), model.SourceClassName)
                .AppendOpeningBracket(Indent1)
                
                // Extension class body
                .GenerateSourceTypeExtensionMethod(model)
                
                // End extensions class declaration
                .AppendClosingBracket(Indent1)
                
                // End namespace declaration
                .AppendClosingBracket();

            return (builder.ToString(), $"{model.ClassName}.g.cs");
        }

        private static StringBuilder GenerateUsings(this StringBuilder builder, MapModel model)
        {
            builder.AppendLine("using System;");

            // NB: If class names are the same, we're going to use fully qualified names instead.
            if (model.Namespace != model.SourceNamespace)
            {
                builder.AppendFormat("using {0};", model.SourceNamespace).AppendLine();
            }

            return builder.AppendLine();
        }

        private static StringBuilder GenerateConstructor(this StringBuilder builder, MapModel model, out List<IPropertySymbol> mappedProperties)
        {
            var sourceClassParameterName = model.SourceClassName.ToCamelCase();

            builder
                .PadLeft(Indent2)
                .AppendFormat("public {0}({1} {2})", model.ClassName, model.SourceClassFullName, sourceClassParameterName)
                .AppendOpeningBracket(Indent2)
                .PadLeft(Indent3)
                .AppendFormat("if ({0} == null) throw new ArgumentNullException(nameof({0}));", sourceClassParameterName)
                .AppendLine();

            mappedProperties = new List<IPropertySymbol>();
            foreach (var propertySymbol in model.SourceTypeProperties)
            {
                if (model.Properties.Any(p => p.Name == propertySymbol.Name))
                {
                    mappedProperties.Add(propertySymbol);
                    builder
                        .PadLeft(Indent3)
                        .AppendFormat("{0} = {1}.{2};{3}", propertySymbol.Name, sourceClassParameterName, propertySymbol.Name, Environment.NewLine);
                }
            }

            // End constructor declaration
            return builder.AppendClosingBracket(Indent2, false);
        }

        private static StringBuilder GenerateFactoryMethod(this StringBuilder builder, MapModel model)
        {
            var sourceClassParameterName = model.SourceClassName.ToCamelCase();

            return builder
                .AppendLine()
                .PadLeft(Indent2)
                .AppendFormat("public static {0} From({1} {2})", model.ClassName, model.SourceClassFullName, sourceClassParameterName)
                .AppendOpeningBracket(Indent2)
                .PadLeft(Indent3)
                .AppendFormat("return {0} == null ? null : new {1}({0});", sourceClassParameterName, model.ClassName)
                .AppendClosingBracket(Indent2);
        }

        private static StringBuilder GenerateSourceTypeExtensionMethod(this StringBuilder builder, MapModel model)
        {
            var sourceClassParameterName = model.SourceClassName.ToCamelCase();

            return builder
                .PadLeft(Indent2)
                .AppendFormat("public static {0} To{0}(this {1} {2})", model.ClassName, model.SourceClassFullName, sourceClassParameterName)
                .AppendOpeningBracket(Indent2)
                .PadLeft(Indent3)
                .AppendFormat("return {0} == null ? null : new {1}({0});", sourceClassParameterName, model.ClassName)
                .AppendClosingBracket(Indent2);
        }
        
        private static StringBuilder AppendFileHeader(this StringBuilder builder)
        {
            return builder
                .AppendLine("// <auto-generated />");
        }
    }
}