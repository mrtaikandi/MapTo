using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MapTo.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.Models
{
    internal record MapModel (
        SourceGenerationOptions Options,
        string? Namespace,
        SyntaxTokenList ClassModifiers,
        string ClassName,
        string SourceNamespace,
        string SourceClassName,
        string SourceClassFullName,
        ImmutableArray<string> MappedProperties
    )
    {
        internal static (MapModel? model, IEnumerable<Diagnostic> diagnostics) CreateModel(
            SemanticModel classSemanticModel,
            ClassDeclarationSyntax classSyntax,
            SourceGenerationOptions sourceGenerationOptions)
        {
            var diagnostics = new List<Diagnostic>();
            var root = classSyntax.GetCompilationUnit();

            if (!(classSemanticModel.GetDeclaredSymbol(classSyntax) is INamedTypeSymbol classSymbol))
            {
                diagnostics.Add(Diagnostics.SymbolNotFoundError(classSyntax.GetLocation(), classSyntax.Identifier.ValueText));
                return (default, diagnostics);
            }

            var sourceTypeSymbol = GetSourceTypeSymbol(classSyntax, classSemanticModel);
            if (sourceTypeSymbol is null)
            {
                diagnostics.Add(Diagnostics.MapFromAttributeNotFoundError(classSyntax.GetLocation()));
                return (default, diagnostics);
            }

            var className = classSyntax.GetClassName();
            var sourceClassName = sourceTypeSymbol.Name;

            var mappedProperties = GetMappedProperties(classSymbol, sourceTypeSymbol);
            if (!mappedProperties.Any())
            {
                diagnostics.Add(Diagnostics.NoMatchingPropertyFoundError(classSyntax.GetLocation(), className, sourceClassName));
                return (default, diagnostics);
            }

            var model = new MapModel(
                sourceGenerationOptions,
                root.GetNamespace(),
                classSyntax.Modifiers,
                className,
                sourceTypeSymbol.ContainingNamespace.ToString(),
                sourceClassName,
                sourceTypeSymbol.ToString(),
                mappedProperties);

            return (model, diagnostics);
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