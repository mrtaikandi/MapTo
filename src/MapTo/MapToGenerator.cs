﻿using System.Collections.Generic;
using System.Linq;
using MapTo.Extensions;
using MapTo.Sources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo
{
    /// <summary>
    /// MapTo source generator.
    /// </summary>
    [Generator]
    public class MapToGenerator : ISourceGenerator
    {
        /// <inheritdoc />
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new MapToSyntaxReceiver());
        }

        /// <inheritdoc />
        public void Execute(GeneratorExecutionContext context)
        {
            var options = SourceGenerationOptions.From(context);

            var compilation = context.Compilation
                .AddSource(ref context, MapFromAttributeSource.Generate(options))
                .AddSource(ref context, IgnorePropertyAttributeSource.Generate(options))
                .AddSource(ref context, ITypeConverterSource.Generate(options))
                .AddSource(ref context, MapTypeConverterAttributeSource.Generate(options))
                .AddSource(ref context, MapPropertyAttributeSource.Generate(options));

            if (context.SyntaxReceiver is MapToSyntaxReceiver receiver && receiver.CandidateClasses.Any())
            {
                AddGeneratedMappingsClasses(context, compilation, receiver.CandidateClasses, options);
            }
        }

        private static void AddGeneratedMappingsClasses(GeneratorExecutionContext context, Compilation compilation, IEnumerable<ClassDeclarationSyntax> candidateClasses, SourceGenerationOptions options)
        {
            foreach (var classSyntax in candidateClasses)
            {
                var mappingContext = new MappingContext(compilation, options, classSyntax);
                mappingContext.Diagnostics.ForEach(context.ReportDiagnostic);
                
                if (mappingContext.Model is not null)
                {
                    var (source, hintName) = MapClassSource.Generate(mappingContext.Model);
                    context.AddSource(hintName, source);
                }
            }
        }
    }
}