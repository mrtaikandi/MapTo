﻿using static MapTo.Sources.Constants;

namespace MapTo.Sources
{
    internal static class IgnorePropertyAttributeSource
    {
        internal const string AttributeName = "IgnoreProperty";
        internal const string AttributeClassName = AttributeName + "Attribute";
        internal const string FullyQualifiedName = RootNamespace + "." + AttributeClassName;

        internal static SourceCode Generate(SourceGenerationOptions options)
        {
            var builder = new SourceBuilder()
                .WriteLine(GeneratedFilesHeader)
                .WriteLine("using System;")
                .WriteLine()
                .WriteLine($"namespace {RootNamespace}")
                .WriteOpeningBracket();

            if (options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine("/// Specified that the annotated property should not be included in the generated mappings.")
                    .WriteLine("/// </summary>");
            }

            builder
                .WriteLine("[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]")
                .WriteLine($"public sealed class {AttributeClassName} : Attribute {{ }}")
                .WriteClosingBracket();

            return new(builder.ToString(), $"{AttributeClassName}.g.cs");
        }
    }
}