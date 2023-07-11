using MapTo.Configuration;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.CodeAnalysis;

internal static class RoslynExtensions
{
    public static IPropertySymbol? FindProperty(this IEnumerable<IPropertySymbol> properties, IPropertySymbol targetProperty)
    {
        return properties.SingleOrDefault(p =>
            p.Name == targetProperty.Name &&
            (p.NullableAnnotation != NullableAnnotation.Annotated ||
             (p.NullableAnnotation == NullableAnnotation.Annotated &&
             targetProperty.NullableAnnotation == NullableAnnotation.Annotated)));
    }

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

    public static SyntaxNode? GetSyntaxNode(this ISymbol symbol) =>
        symbol.Locations.FirstOrDefault() is { } location ? location.SourceTree?.GetRoot().FindNode(location.SourceSpan) : null;

    public static IEnumerable<INamedTypeSymbol> GetTypesByMetadataName(this Compilation? compilation, string typeMetadataName)
    {
        if (compilation is null)
        {
            return Enumerable.Empty<INamedTypeSymbol>();
        }

        return compilation.References
            .Select(compilation.GetAssemblyOrModuleSymbol)
            .OfType<IAssemblySymbol>()
            .Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName(typeMetadataName))
            .Where(t => t != null)!;
    }

    public static bool IsPartial(this TypeDeclarationSyntax syntax) => syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

    public static bool TypeByMetadataNameExists(this Compilation? compilation, string typeMetadataName) => GetTypesByMetadataName(compilation, typeMetadataName).Any();
}