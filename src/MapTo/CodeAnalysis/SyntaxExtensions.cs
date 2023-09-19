using MapTo.Configuration;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.CodeAnalysis;

internal static class SyntaxExtensions
{
    public static AccessModifier GetAccessModifier(this ClassDeclarationSyntax syntax) => syntax.Modifiers.FirstOrDefault().Kind() switch
    {
        SyntaxKind.PublicKeyword => AccessModifier.Public,
        SyntaxKind.PrivateKeyword => AccessModifier.Private,
        _ => AccessModifier.Internal
    };

    public static AttributeSyntax? GetAttribute(this TypeDeclarationSyntax typeDeclarationSyntax, string attributeName)
    {
        return typeDeclarationSyntax.AttributeLists
            .SelectMany(al => al.Attributes)
            .SingleOrDefault(a =>
                (a.Name as IdentifierNameSyntax)?.Identifier.ValueText == attributeName ||
                ((a.Name as QualifiedNameSyntax)?.Right as IdentifierNameSyntax)?.Identifier.ValueText == attributeName);
    }

    public static string? GetNamespace(this TypeDeclarationSyntax typeDeclarationSyntax) => typeDeclarationSyntax
        .Ancestors()
        .OfType<NamespaceDeclarationSyntax>()
        .FirstOrDefault()
        ?.Name
        .ToString();

    public static bool IsPartial(this TypeDeclarationSyntax syntax) => syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
}