using System.Collections.Generic;
using System.Linq;
using MapTo.Models;
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
            AddMapFromAttribute(context);

            if (context.SyntaxReceiver is MapToSyntaxReceiver receiver && receiver.CandidateClasses.Any())
            {
                AddGeneratedMappingsClasses(context, receiver.CandidateClasses);
            }
        }

        private static void AddGeneratedMappingsClasses(GeneratorExecutionContext context, IEnumerable<ClassDeclarationSyntax> candidateClasses)
        {
            foreach (var classDeclarationSyntax in candidateClasses)
            {
                var (model, diagnostic) = MapModel.Create(context.Compilation, classDeclarationSyntax);
                if (model is null)
                {
                    context.ReportDiagnostic(diagnostic!);
                    continue;
                }

                var (source, hintName) = SourceBuilder.GenerateSource(model);

                context.AddSource(hintName, source);
                context.ReportDiagnostic(Diagnostics.ClassMappingsGenerated(classDeclarationSyntax.GetLocation(), model.ClassName));
            }
        }

        private static void AddMapFromAttribute(GeneratorExecutionContext context)
        {
            var (source, hintName) = SourceBuilder.GenerateMapFromAttribute();
            context.AddSource(hintName, source);
        }
    }
}