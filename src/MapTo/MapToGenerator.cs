using System.Collections.Generic;
using System.Linq;
using MapTo.Extensions;
using MapTo.Models;
using MapTo.Sources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo
{
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
            
            context.AddSource(MapFromAttributeSource.Generate(options));
            context.AddSource(IgnorePropertyAttributeSource.Generate(options));
            context.AddSource(TypeConverterSource.Generate(options));
            context.AddSource(MapPropertyAttributeSource.Generate(options));
            
            if (context.SyntaxReceiver is MapToSyntaxReceiver receiver && receiver.CandidateClasses.Any())
            {
                AddGeneratedMappingsClasses(context, receiver.CandidateClasses, options);
            }
        }

        private static void AddGeneratedMappingsClasses(GeneratorExecutionContext context, IEnumerable<ClassDeclarationSyntax> candidateClasses, SourceGenerationOptions options)
        {
            foreach (var classSyntax in candidateClasses)
            {
                var classSemanticModel = context.Compilation.GetSemanticModel(classSyntax.SyntaxTree);
                var (model, diagnostics) = MapModel.CreateModel(classSemanticModel, classSyntax, options);
                
                diagnostics.ForEach(context.ReportDiagnostic);

                if (model is not null)
                {
                    context.AddSource(MapClassSource.Generate(model));
                }
            }
        }
    }
}