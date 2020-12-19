using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo
{
    internal class MapToSyntaxReceiver : ISyntaxReceiver
    {
        public List<(ClassDeclarationSyntax classDeclarationSyntax, AttributeSyntax attributeSyntax)> CandidateClasses { get; } = new();

        /// <inheritdoc />
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is not ClassDeclarationSyntax { AttributeLists: { Count: >= 1 } attributes } classDeclaration)
            {
                return;
            }

            var attributeSyntax = attributes
                .SelectMany(a => a.Attributes)
                .SingleOrDefault(a => a.Name is
                    IdentifierNameSyntax { Identifier: { ValueText: SourceProvider.MapFromAttributeName } } // For: [MapFrom] 
                    or
                    QualifiedNameSyntax // For: [MapTo.MapFrom]
                    {
                        Left: IdentifierNameSyntax { Identifier: { ValueText: SourceProvider.NamespaceName } },
                        Right: IdentifierNameSyntax { Identifier: { ValueText: SourceProvider.MapFromAttributeName } }
                    }
                );

            if (attributeSyntax is not null)
            {
                CandidateClasses.Add((classDeclaration, attributeSyntax));
            }
        }
    }
}