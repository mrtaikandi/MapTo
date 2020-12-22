using System.Collections.Generic;
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

        internal MapModel(CompilationUnitSyntax root, ClassDeclarationSyntax classSyntax, ITypeSymbol classSymbol, ITypeSymbol sourceTypeSymbol)
            : this(
                root.GetNamespace(),
                classSyntax.Modifiers,
                classSyntax.GetClassName(),
                classSymbol.GetAllMembersOfType<IPropertySymbol>(),
                sourceTypeSymbol.ContainingNamespace.ToString(),
                sourceTypeSymbol.Name,
                sourceTypeSymbol.ToString(),
                sourceTypeSymbol.GetAllMembersOfType<IPropertySymbol>()) { }

        public string? Namespace { get; }

        public SyntaxTokenList ClassModifiers { get; }

        public string ClassName { get; }

        public IEnumerable<IPropertySymbol> Properties { get; }

        public string SourceNamespace { get; }

        public string SourceClassName { get; }

        public string SourceClassFullName { get; }

        public IEnumerable<IPropertySymbol> SourceTypeProperties { get; }
    }
}