using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.CodeAnalysis;

internal static class SyntaxExtensions
{
    public static Accessibility GetAccessibility(this BaseTypeDeclarationSyntax syntax) => syntax.Modifiers.FirstOrDefault().Kind() switch
    {
        SyntaxKind.PublicKeyword => Accessibility.Public,
        SyntaxKind.PrivateKeyword => Accessibility.Private,
        _ => Accessibility.Internal
    };

    public static AttributeSyntax? GetAttribute(this TypeDeclarationSyntax typeDeclarationSyntax, string attributeName)
    {
        return typeDeclarationSyntax.AttributeLists
            .SelectMany(al => al.Attributes)
            .SingleOrDefault(a =>
                (a.Name as IdentifierNameSyntax)?.Identifier.ValueText == attributeName ||
                ((a.Name as QualifiedNameSyntax)?.Right as IdentifierNameSyntax)?.Identifier.ValueText == attributeName ||
                ((a.Name as GenericNameSyntax)?.Identifier.ValueText == attributeName));
    }

    public static bool HasAnyAttributes(this BaseTypeDeclarationSyntax typeDeclarationSyntax, params string[] attributeNames) =>
        attributeNames.Any(typeDeclarationSyntax.HasAttribute);

    public static bool HasAttribute(this BaseTypeDeclarationSyntax typeDeclarationSyntax, string attributeName) =>
        typeDeclarationSyntax.AttributeLists.HasAttribute(attributeName);

    public static bool HasAttribute(this SyntaxList<AttributeListSyntax> attributeLists, string attributeName) => attributeLists
        .SelectMany(al => al.Attributes)
        .Any(a => a.Name switch
        {
            QualifiedNameSyntax q => q.Right.Identifier.ValueText == attributeName,
            SimpleNameSyntax s => s.Identifier.ValueText == attributeName,
            _ => false
        });

    public static bool HasAttribute(this AttributeListSyntax attributeLists, string attributeName) => attributeLists
        .Attributes
        .Any(a => a.Name switch
        {
            QualifiedNameSyntax q => q.Right.Identifier.ValueText == attributeName,
            SimpleNameSyntax s => s.Identifier.ValueText == attributeName,
            _ => false
        });

    public static string? GetNamespace(this TypeDeclarationSyntax typeDeclarationSyntax) => typeDeclarationSyntax
        .Ancestors()
        .OfType<NamespaceDeclarationSyntax>()
        .FirstOrDefault()
        ?.Name
        .ToString();

    public static bool IsPartial(this BaseTypeDeclarationSyntax syntax) => syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

    public static string GetKeywordText(this BaseTypeDeclarationSyntax syntax) => syntax switch
    {
        TypeDeclarationSyntax t => t.Keyword.Text,
        EnumDeclarationSyntax e => e.EnumKeyword.Text,
        _ => string.Empty
    };
}