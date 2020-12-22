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
            AddMapFromAttribute(context);

            if (context.SyntaxReceiver is MapToSyntaxReceiver receiver && receiver.CandidateClasses.Any())
            {
                AddGeneratedMappingsClasses(context, receiver.CandidateClasses);
            }
        }

        private static void AddGeneratedMappingsClasses(GeneratorExecutionContext context, IEnumerable<ClassDeclarationSyntax> candidateClasses)
        {
            foreach (var classSyntax in candidateClasses)
            {
                var root = classSyntax.GetCompilationUnit();
                var classSemanticModel = context.Compilation.GetSemanticModel(classSyntax.SyntaxTree);
                var classSymbol = classSemanticModel.GetDeclaredSymbol(classSyntax) as INamedTypeSymbol;
                var sourceTypeSymbol = GetSourceTypeSymbol(classSyntax, classSemanticModel);

                var (isValid, diagnostics) = Verify(root, classSyntax, classSemanticModel, classSymbol, sourceTypeSymbol);
                if (!isValid)
                {
                    diagnostics.ForEach(context.ReportDiagnostic);
                    continue;
                }
                
                var model = new MapModel(root, classSyntax, classSymbol!, sourceTypeSymbol!);

                var (source, hintName) = SourceBuilder.GenerateSource(model);
                context.AddSource(hintName, source);
                context.ReportDiagnostic(Diagnostics.ClassMappingsGenerated(classSyntax.GetLocation(), model.ClassName));
            }
        }

        private static void AddMapFromAttribute(GeneratorExecutionContext context)
        {
            var (source, hintName) = SourceBuilder.GenerateMapFromAttribute();
            context.AddSource(hintName, source);
        }

        private static INamedTypeSymbol? GetSourceTypeSymbol(ClassDeclarationSyntax classSyntax, SemanticModel model)
        {
            var sourceTypeExpressionSyntax = classSyntax
                .GetAttribute(SourceBuilder.MapFromAttributeName)
                ?.DescendantNodes()
                .OfType<TypeOfExpressionSyntax>()
                .SingleOrDefault();

            return sourceTypeExpressionSyntax is not null ? model.GetTypeInfo(sourceTypeExpressionSyntax.Type).Type as INamedTypeSymbol : null;
        }

        private static (bool isValid, IEnumerable<Diagnostic> diagnostics) Verify(CompilationUnitSyntax root, ClassDeclarationSyntax classSyntax, SemanticModel classSemanticModel, INamedTypeSymbol? classSymbol, INamedTypeSymbol? sourceTypeSymbol)
        {
            var diagnostics = new List<Diagnostic>();

            if (classSymbol is null)
            {
                diagnostics.Add(Diagnostics.SymbolNotFound(classSyntax.GetLocation(), classSyntax.Identifier.ValueText));
            }

            if (sourceTypeSymbol is null)
            {
                diagnostics.Add(Diagnostics.SymbolNotFound(classSyntax.GetLocation(), classSyntax.Identifier.ValueText));
            }

            return (!diagnostics.Any(), diagnostics);
        }
    }
}