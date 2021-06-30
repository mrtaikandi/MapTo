using System.Linq;
using MapTo.Extensions;
using MapTo.Sources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static MapTo.Sources.Constants;

namespace MapTo
{
    internal static class DiagnosticsFactory
    {
        private const string UsageCategory = "Usage";
        private const string CodePrefix = "MT";
        private const string ErrorId = CodePrefix + "0";
        private const string InfoId = CodePrefix + "1";
        private const string WarningId = CodePrefix + "2";

        internal static Diagnostic TypeNotFoundError(Location location, string syntaxName) =>
            Create($"{ErrorId}010", location, $"Unable to find '{syntaxName}' type.");

        internal static Diagnostic MapFromAttributeNotFoundError(Location location) =>
            Create($"{ErrorId}020", location, $"Unable to find {MapFromAttributeSource.AttributeName} type.");

        internal static Diagnostic NoMatchingPropertyFoundError(Location location, INamedTypeSymbol classType, INamedTypeSymbol sourceType) =>
            Create($"{ErrorId}030", location, $"No matching properties found between '{classType.ToDisplayString()}' and '{sourceType.ToDisplayString()}' types.");

        internal static Diagnostic NoMatchingPropertyTypeFoundError(ISymbol property) =>
            Create($"{ErrorId}031", property.Locations.FirstOrDefault(), $"Cannot create a map for '{property.ToDisplayString()}' property because source and destination types are not implicitly convertible. Consider using '{MapTypeConverterAttributeSource.FullyQualifiedName}' to provide a type converter or ignore the property using '{IgnorePropertyAttributeSource.FullyQualifiedName}'.");

        internal static Diagnostic InvalidTypeConverterGenericTypesError(ISymbol property, IPropertySymbol sourceProperty) =>
            Create($"{ErrorId}032", property.Locations.FirstOrDefault(), $"Cannot map '{property.ToDisplayString()}' property because the annotated converter does not implement '{RootNamespace}.{ITypeConverterSource.InterfaceName}<{sourceProperty.Type.ToDisplayString()}, {property.GetTypeSymbol()?.ToDisplayString()}>'.");

        internal static Diagnostic ConfigurationParseError(string error) =>
            Create($"{ErrorId}040", Location.None, error);

        internal static Diagnostic MissingConstructorArgument(ConstructorDeclarationSyntax constructorSyntax) =>
            Create($"{ErrorId}050", constructorSyntax.GetLocation(), "There are no argument given that corresponds to the required formal parameter.");

        private static Diagnostic Create(string id, Location? location, string message, DiagnosticSeverity severity = DiagnosticSeverity.Error) =>
            Diagnostic.Create(new DiagnosticDescriptor(id, string.Empty, message, UsageCategory, severity, true), location ?? Location.None);
    }
}