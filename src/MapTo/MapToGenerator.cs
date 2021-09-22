using System;
using System.Collections.Generic;
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
            try
            {
                var options = SourceGenerationOptions.From(context);
                var compilation = context.Compilation
                    .AddSource(ref context, MapFromAttributeSource.Generate(options))
                    .AddSource(ref context, IgnorePropertyAttributeSource.Generate(options))
                    .AddSource(ref context, ITypeConverterSource.Generate(options))
                    .AddSource(ref context, MapTypeConverterAttributeSource.Generate(options))
                    .AddSource(ref context, MapPropertyAttributeSource.Generate(options))
                    .AddSource(ref context, MappingContextSource.Generate(options));

                if (context.SyntaxReceiver is MapToSyntaxReceiver receiver && receiver.CandidateTypes.Any())
                {
                    AddGeneratedMappingsClasses(context, compilation, receiver.CandidateTypes, options);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        private static void AddGeneratedMappingsClasses(GeneratorExecutionContext context, Compilation compilation, IEnumerable<TypeDeclarationSyntax> candidateTypes, SourceGenerationOptions options)
        {
            foreach (var typeDeclarationSyntax in candidateTypes)
            {
                var mappingContext = MappingContext.Create(compilation, options, typeDeclarationSyntax);
                mappingContext.Diagnostics.ForEach(context.ReportDiagnostic);

                if (mappingContext.Models.IsEmpty())
                {
                    continue;
                }

                foreach (var model in mappingContext.Models)
                {
                    var (source, hintName) = typeDeclarationSyntax switch
                    {
                        ClassDeclarationSyntax => MapClassSource.Generate(model),
                        RecordDeclarationSyntax => MapRecordSource.Generate(model),
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    context.AddSource(hintName, source);
                }
            }
        }
    }
}