using System.Collections.Generic;
using System.Linq;
using MapTo.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.Models
{
    public class MapModel
    {
        private MapModel(
            string? ns,
            SyntaxTokenList classModifiers,
            string className,
            IEnumerable<IPropertySymbol> properties,
            string sourceNamespace,
            string sourceClassName,
            string sourceClassFullName,
            IEnumerable<IPropertySymbol> sourceTypeProperties)
        {
            Namespace = ns;
            ClassModifiers = classModifiers;
            ClassName = className;
            Properties = properties;
            SourceNamespace = sourceNamespace;
            SourceClassName = sourceClassName;
            SourceClassFullName = sourceClassFullName;
            SourceTypeProperties = sourceTypeProperties;
        }

        public string? Namespace { get; }

        public SyntaxTokenList ClassModifiers { get; }

        public string ClassName { get; }

        public IEnumerable<IPropertySymbol> Properties { get; }

        public string SourceNamespace { get; }

        public string SourceClassName { get; }

        public string SourceClassFullName { get; }

        public IEnumerable<IPropertySymbol> SourceTypeProperties { get; }

        internal static (MapModel? model, Diagnostic? diagnostic) Create(Compilation compilation, ClassDeclarationSyntax classSyntax)
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

            var model = new MapModel(
                root.GetNamespace(),
                classSyntax.Modifiers,
                classSyntax.GetClassName(),
                classSymbol.GetAllMembersOfType<IPropertySymbol>(),
                sourceTypeSymbol.ContainingNamespace.ToString(),
                sourceTypeSymbol.Name,
                sourceTypeSymbol.ToString(),
                sourceTypeSymbol.GetAllMembersOfType<IPropertySymbol>());

            return (model, default);
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