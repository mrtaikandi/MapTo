using System.Collections.Generic;
using System.Linq;
using MapTo.Extensions;
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
            context.AddMapToAttribute();

            if (!(context.SyntaxReceiver is MapToSyntaxReceiver receiver) || !receiver.CandidateClasses.Any())
            {
                return;
            }

            foreach (var classDeclarationSyntax in receiver.CandidateClasses)
            {
                var (model, diagnostic) = GetModel(context.Compilation, classDeclarationSyntax);
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

        private static (MapModel? model, Diagnostic? diagnostic) GetModel(Compilation compilation, ClassDeclarationSyntax classSyntax)
        {
            var root = classSyntax.GetCompilationUnit();
            var classSemanticModel = compilation.GetSemanticModel(classSyntax.SyntaxTree);

            if (!(classSemanticModel.GetDeclaredSymbol(classSyntax) is INamedTypeSymbol classSymbol))
            {
                return (default, Diagnostics.SymbolNotFound(classSyntax.GetLocation(), classSyntax.Identifier.ValueText));
            }

            var sourceTypeSymbol = GetSourceTypeSymbol(classSyntax, classSemanticModel);
            if (sourceTypeSymbol is null)
            {
                return (default, Diagnostics.SymbolNotFound(classSyntax.GetLocation(), classSyntax.Identifier.ValueText));
            }

            return (MapModel.Create(root, classSyntax, classSymbol, sourceTypeSymbol), default);
        }

        private static ITypeSymbol? GetSourceTypeSymbol(ClassDeclarationSyntax classSyntax, SemanticModel model)
        {
            var sourceTypeExpressionSyntax = classSyntax
                .GetAttribute(SourceBuilder.MapFromAttributeName)
                ?.DescendantNodes()
                .OfType<TypeOfExpressionSyntax>()
                .SingleOrDefault();

            return sourceTypeExpressionSyntax is not null ? model.GetTypeInfo(sourceTypeExpressionSyntax.Type).Type : null;
        }
    }
}