using System;
using System.Collections.Generic;
using System.Linq;
using MapTo.Sources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.Extensions
{
    internal static class RoslynExtensions
    {
        public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
        {
            var current = type;
            while (current != null)
            {
                yield return current;

                current = current.BaseType;
            }
        }

        public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol type)
        {
            return type.GetBaseTypesAndThis().SelectMany(n => n.GetMembers());
        }

        public static CompilationUnitSyntax GetCompilationUnit(this SyntaxNode syntaxNode) => syntaxNode.Ancestors().OfType<CompilationUnitSyntax>().Single();

        public static string GetClassName(this ClassDeclarationSyntax classSyntax) => classSyntax.Identifier.Text;

        public static AttributeSyntax? GetAttribute(this ClassDeclarationSyntax classSyntax, string attributeName)
        {
            return classSyntax.AttributeLists
                .SelectMany(al => al.Attributes)
                .SingleOrDefault(a =>
                    (a.Name as IdentifierNameSyntax)?.Identifier.ValueText == attributeName ||
                    ((a.Name as QualifiedNameSyntax)?.Right as IdentifierNameSyntax)?.Identifier.ValueText == attributeName);
        }

        public static bool HasAttribute(this ISymbol symbol, ITypeSymbol attributeSymbol) =>
            symbol.GetAttributes().Any(a => a.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) == true);

        public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, ITypeSymbol attributeSymbol) =>
            symbol.GetAttributes().Where(a => a.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) == true);

        public static AttributeData? GetAttribute(this ISymbol symbol, ITypeSymbol attributeSymbol) =>
            symbol.GetAttributes(attributeSymbol).FirstOrDefault();

        public static string? GetNamespace(this ClassDeclarationSyntax classDeclarationSyntax) =>
            classDeclarationSyntax.Ancestors()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault()
                ?.Name
                .ToString();

        public static bool HasCompatibleTypes(this Compilation compilation, IPropertySymbol sourceProperty, IPropertySymbol destinationProperty) =>
            SymbolEqualityComparer.Default.Equals(destinationProperty.Type, sourceProperty.Type) || compilation.HasImplicitConversion(sourceProperty.Type, destinationProperty.Type);

        public static IPropertySymbol? FindProperty(this IEnumerable<IPropertySymbol> properties, IPropertySymbol targetProperty)
        {
            return properties.SingleOrDefault(p =>
                p.Name == targetProperty.Name &&
                (p.NullableAnnotation != NullableAnnotation.Annotated ||
                 p.NullableAnnotation == NullableAnnotation.Annotated &&
                 targetProperty.NullableAnnotation == NullableAnnotation.Annotated));
        }

        public static INamedTypeSymbol GetTypeByMetadataNameOrThrow(this Compilation compilation, string fullyQualifiedMetadataName) => 
            compilation.GetTypeByMetadataName(fullyQualifiedMetadataName) ?? throw new TypeLoadException($"Unable to find '{fullyQualifiedMetadataName}' type.");
    }
}