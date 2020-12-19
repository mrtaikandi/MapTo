using System.Collections.Generic;
using System.Linq;
using MapTo.Extensions;
using MapTo.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo
{
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

            foreach (var (classDeclarationSyntax, attributeSyntax) in receiver.CandidateClasses)
            {
                var model = GetModel(context.Compilation, classDeclarationSyntax);
                if (model is null)
                {
                    // TODO: Emit diagnostic info.
                    continue;
                }
  
                var (source, hintName) = SourceProvider.GenerateSource(model);
                
                context.AddSource(hintName, source);
            }
        }

        private static MapModel? GetModel(Compilation compilation, ClassDeclarationSyntax classSyntax)
        {
            var root = classSyntax.GetCompilationUnit();
            if (root is null)
            {
                return null;
            }
            
            var classSemanticModel = compilation.GetSemanticModel(classSyntax.SyntaxTree);

            if (!(classSemanticModel.GetDeclaredSymbol(classSyntax) is INamedTypeSymbol classSymbol))
            {
                return null;
            }

            var destinationTypeSymbol = GetDestinationTypeSymbol(classSyntax, classSemanticModel);
            if (destinationTypeSymbol is null)
            {
                return null;
            }
            
            return new MapModel(
                ns: root.GetNamespace(),
                classModifiers: classSyntax.GetClassModifier(),  
                className: classSyntax.GetClassName(),
                properties: classSymbol.GetAllMembersOfType<IPropertySymbol>(),
                sourceNamespace: destinationTypeSymbol.ContainingNamespace.Name,
                sourceClassName: destinationTypeSymbol.Name,
                sourceTypeProperties: destinationTypeSymbol.GetAllMembersOfType<IPropertySymbol>());
        }

        private static ITypeSymbol? GetDestinationTypeSymbol(ClassDeclarationSyntax classSyntax, SemanticModel model)
        {
            var destinationTypeExpressionSyntax = classSyntax
                .GetAttribute(SourceProvider.MapFromAttributeName)
                ?.DescendantNodes()
                .OfType<TypeOfExpressionSyntax>()
                .SingleOrDefault();

            return destinationTypeExpressionSyntax is not null ? model.GetTypeInfo(destinationTypeExpressionSyntax.Type).Type : null;
        }
    }
}