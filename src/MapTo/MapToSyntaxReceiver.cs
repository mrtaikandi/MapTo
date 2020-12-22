using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo
{
    internal class MapToSyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> CandidateClasses { get; } = new();

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
                    IdentifierNameSyntax { Identifier: { ValueText: SourceBuilder.MapFromAttributeName } } // For: [MapFrom] 
                    or
                    QualifiedNameSyntax // For: [MapTo.MapFrom]
                    {
                        Left: IdentifierNameSyntax { Identifier: { ValueText: SourceBuilder.NamespaceName } },
                        Right: IdentifierNameSyntax { Identifier: { ValueText: SourceBuilder.MapFromAttributeName } }
                    }
                );

            if (attributeSyntax is not null)
            {
                CandidateClasses.Add(classDeclaration);
            }
        }
    }
}