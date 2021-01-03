using System.Collections.Generic;
using System.Collections.Immutable;
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
            var options = SourceGenerationOptions.From(context);
            
            AddAttribute(context, SourceBuilder.GenerateMapFromAttribute(options));
            AddAttribute(context, SourceBuilder.GenerateIgnorePropertyAttribute(options));
            
            if (context.SyntaxReceiver is MapToSyntaxReceiver receiver && receiver.CandidateClasses.Any())
            {
                AddGeneratedMappingsClasses(context, receiver.CandidateClasses, options);
            }
        }

        private static void AddGeneratedMappingsClasses(GeneratorExecutionContext context, IEnumerable<ClassDeclarationSyntax> candidateClasses, SourceGenerationOptions options)
        {
            foreach (var classSyntax in candidateClasses)
            {
                var model = CreateModel(context, classSyntax, options);
                if (model is null)
                {
                    continue;
                }
                
                var (source, hintName) = SourceBuilder.GenerateSource(model);

                context.AddSource(hintName, source);
            }
        }

        private static void AddAttribute(GeneratorExecutionContext context, (string source, string hintName) attribute) 
            => context.AddSource(attribute.hintName, attribute.source);

        private static INamedTypeSymbol? GetSourceTypeSymbol(ClassDeclarationSyntax classSyntax, SemanticModel model)
        {
            var sourceTypeExpressionSyntax = classSyntax
                .GetAttribute(SourceBuilder.MapFromAttributeName)
                ?.DescendantNodes()
                .OfType<TypeOfExpressionSyntax>()
                .SingleOrDefault();

            return sourceTypeExpressionSyntax is not null ? model.GetTypeInfo(sourceTypeExpressionSyntax.Type).Type as INamedTypeSymbol : null;
        }
        
        private static MapModel? CreateModel(GeneratorExecutionContext context, ClassDeclarationSyntax classSyntax, SourceGenerationOptions sourceGenerationOptions)
        {
            var root = classSyntax.GetCompilationUnit();
            var classSemanticModel = context.Compilation.GetSemanticModel(classSyntax.SyntaxTree);
            
            if (!(classSemanticModel.GetDeclaredSymbol(classSyntax) is INamedTypeSymbol classSymbol))
            {
                context.ReportDiagnostic(Diagnostics.SymbolNotFoundError(classSyntax.GetLocation(), classSyntax.Identifier.ValueText));
                return null;
            }
            
            var sourceTypeSymbol = GetSourceTypeSymbol(classSyntax, classSemanticModel);
            if (sourceTypeSymbol is null)
            {
                context.ReportDiagnostic(Diagnostics.MapFromAttributeNotFoundError(classSyntax.GetLocation()));
                return null;
            }

            var className = classSyntax.GetClassName();
            var sourceClassName = sourceTypeSymbol.Name;
            
            var mappedProperties = GetMappedProperties(classSymbol, sourceTypeSymbol);
            if (!mappedProperties.Any())
            {
                context.ReportDiagnostic(Diagnostics.NoMatchingPropertyFoundError(classSyntax.GetLocation(), className, sourceClassName));
                return null;
            }
            
            return new MapModel(
                sourceGenerationOptions,
                root.GetNamespace(),
                classSyntax.Modifiers,
                className,
                sourceTypeSymbol.ContainingNamespace.ToString(),
                sourceClassName,
                sourceTypeSymbol.ToString(),
                mappedProperties);
        }
        
        private static ImmutableArray<string> GetMappedProperties(ITypeSymbol classSymbol, ITypeSymbol sourceTypeSymbol)
        {
            return sourceTypeSymbol
                .GetAllMembersOfType<IPropertySymbol>()
                .Select(p => (p.Name, p.Type.ToString()))
                .Intersect(classSymbol
                    .GetAllMembersOfType<IPropertySymbol>()
                    .Where(p => p.GetAttributes().All(a => a.AttributeClass?.Name != SourceBuilder.IgnorePropertyAttributeName))
                    .Select(p => (p.Name, p.Type.ToString())))
                .Select(p => p.Name)
                .ToImmutableArray();
        }
    }
}