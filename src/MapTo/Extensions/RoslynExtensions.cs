using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol type, bool includeBaseTypeMembers = true)
        {
            return includeBaseTypeMembers
                ? type.GetBaseTypesAndThis().SelectMany(t => t.GetMembers())
                : type.GetMembers();
        }

        public static CompilationUnitSyntax GetCompilationUnit(this SyntaxNode syntaxNode) => syntaxNode.Ancestors().OfType<CompilationUnitSyntax>().Single();

        public static string GetIdentifierName(this TypeDeclarationSyntax typeSyntax) => typeSyntax.Identifier.Text;

        public static IEnumerable<AttributeSyntax> GetAttributes(this TypeDeclarationSyntax typeDeclarationSyntax, string attributeName)
        {
            return typeDeclarationSyntax.AttributeLists
                .SelectMany(al => al.Attributes)
                .Where(a =>
                    (a.Name as IdentifierNameSyntax)?.Identifier.ValueText == attributeName ||
                    ((a.Name as QualifiedNameSyntax)?.Right as IdentifierNameSyntax)?.Identifier.ValueText == attributeName);
        }

        public static bool HasAttribute(this ISymbol symbol, ITypeSymbol attributeSymbol) =>
            symbol.GetAttributes().Any(a => a.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) == true);

        public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, ITypeSymbol attributeSymbol) =>
            symbol.GetAttributes().Where(a => a.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) == true);

        public static AttributeData? GetAttribute(this ISymbol symbol, ITypeSymbol attributeSymbol) =>
            symbol.GetAttributes(attributeSymbol).FirstOrDefault();

        public static string? GetNamespace(this TypeDeclarationSyntax typeDeclarationSyntax) => typeDeclarationSyntax
            .Ancestors()
            .OfType<NamespaceDeclarationSyntax>()
            .FirstOrDefault()
            ?.Name
            .ToString();

        public static bool HasCompatibleTypes(this Compilation compilation, ISymbol source, ISymbol destination) =>
            source.TryGetTypeSymbol(out var sourceType) && destination.TryGetTypeSymbol(out var destinationType) &&
            (SymbolEqualityComparer.Default.Equals(destinationType, sourceType) || compilation.HasImplicitConversion(sourceType, destinationType));

        public static bool TryGetTypeSymbol(this ISymbol symbol, [NotNullWhen(true)] out ITypeSymbol? typeSymbol)
        {
            switch (symbol)
            {
                case IPropertySymbol propertySymbol:
                    typeSymbol = propertySymbol.Type;
                    return true;

                case IParameterSymbol parameterSymbol:
                    typeSymbol = parameterSymbol.Type;
                    return true;

                default:
                    typeSymbol = null;
                    return false;
            }
        }

        public static ITypeSymbol? GetTypeSymbol(this ISymbol symbol) => symbol.TryGetTypeSymbol(out var typeSymbol) ? typeSymbol : null;

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

        public static bool IsGenericEnumerable(this Compilation compilation, ITypeSymbol typeSymbol) =>
            typeSymbol is INamedTypeSymbol { IsGenericType: true } &&
            compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Equals(typeSymbol.OriginalDefinition, SymbolEqualityComparer.Default);

        public static bool IsArray(this Compilation compilation, ITypeSymbol typeSymbol) => typeSymbol is IArrayTypeSymbol;

        public static bool IsPrimitiveType(this ITypeSymbol type) => type.SpecialType is
            SpecialType.System_String or
            SpecialType.System_Boolean or
            SpecialType.System_SByte or
            SpecialType.System_Int16 or
            SpecialType.System_Int32 or
            SpecialType.System_Int64 or
            SpecialType.System_Byte or
            SpecialType.System_UInt16 or
            SpecialType.System_UInt32 or
            SpecialType.System_UInt64 or
            SpecialType.System_Single or
            SpecialType.System_Double or
            SpecialType.System_Char or
            SpecialType.System_Object;

        public static SyntaxNode? GetSyntaxNode(this ISymbol symbol) =>
            symbol.Locations.FirstOrDefault() is { } location ? location.SourceTree?.GetRoot().FindNode(location.SourceSpan) : null;

        public static IEnumerable<INamedTypeSymbol> GetTypesByMetadataName(this Compilation compilation, string typeMetadataName)
        {
            return compilation.References
                .Select(compilation.GetAssemblyOrModuleSymbol)
                .OfType<IAssemblySymbol>()
                .Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName(typeMetadataName))
                .Where(t => t != null)!;
        }

        public static bool TypeByMetadataNameExists(this Compilation compilation, string typeMetadataName) => GetTypesByMetadataName(compilation, typeMetadataName).Any();
    }
}