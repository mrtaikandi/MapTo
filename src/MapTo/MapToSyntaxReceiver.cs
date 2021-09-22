using System.Collections.Generic;
using System.Linq;
using MapTo.Sources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo
{
    internal class MapToSyntaxReceiver : ISyntaxReceiver
    {
        public List<TypeDeclarationSyntax> CandidateTypes { get; } = new();

        /// <inheritdoc />
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is not TypeDeclarationSyntax { AttributeLists: { Count: >= 1 } attributes } typeDeclarationSyntax)
            {
                return;
            }

            var attributeSyntax = attributes
                .SelectMany(a => a.Attributes)
                .Where(a => a.Name is
                    IdentifierNameSyntax { Identifier: { ValueText: MapFromAttributeSource.AttributeName } } // For: [MapFrom]
                    or
                    QualifiedNameSyntax // For: [MapTo.MapFrom]
                    {
                        Left: IdentifierNameSyntax { Identifier: { ValueText: Constants.RootNamespace } },
                        Right: IdentifierNameSyntax { Identifier: { ValueText: MapFromAttributeSource.AttributeName } }
                    }
                );

            if (attributeSyntax is not null && attributeSyntax.Any())
            {
                CandidateTypes.Add(typeDeclarationSyntax);
            }
        }
    }
}