using System.Collections.Generic;
using System.Linq;
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

        public static IEnumerable<T> GetAllMembersOfType<T>(this ITypeSymbol type) where T : ISymbol
        {
            return type.GetAllMembers().OfType<T>();
        }

        public static CompilationUnitSyntax? GetCompilationUnit(this SyntaxNode syntaxNode)
        {
            return syntaxNode.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault();
        }

        public static string GetClassName(this ClassDeclarationSyntax classSyntax)
        {
            return classSyntax.Identifier.Text;
        }

        public static string GetClassModifier(this ClassDeclarationSyntax classSyntax)
        {
            return classSyntax.Modifiers.ToFullString().Trim();
        }

        public static bool HaveAttribute(this ClassDeclarationSyntax classSyntax, string attributeName)
        {
            return classSyntax.AttributeLists.Count > 0 &&
                   classSyntax.AttributeLists.SelectMany(al => al.Attributes
                           .Where(a =>
                               (a.Name as IdentifierNameSyntax)?.Identifier.Text == attributeName ||
                               ((a.Name as QualifiedNameSyntax)?.Right as IdentifierNameSyntax)?.Identifier.ValueText == attributeName))
                       .Any();
        }

        public static AttributeSyntax? GetAttribute(this ClassDeclarationSyntax classSyntax, string attributeName)
        {
            return classSyntax.AttributeLists
                .SelectMany(al => al.Attributes)
                .SingleOrDefault(a =>
                    (a.Name as IdentifierNameSyntax)?.Identifier.ValueText == attributeName ||
                    ((a.Name as QualifiedNameSyntax)?.Right as IdentifierNameSyntax)?.Identifier.ValueText == attributeName);
        }

        public static string? GetNamespace(this CompilationUnitSyntax root)
        {
            return root.ChildNodes()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault()
                ?.Name
                .ToString();
        }

        public static List<string> GetUsings(this CompilationUnitSyntax root)
        {
            return root.ChildNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(n => n.Name.ToString())
                .ToList();
        }
    }
}