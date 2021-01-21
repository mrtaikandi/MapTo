using System.Collections.Immutable;
using System.Linq;
using MapTo.Sources;
using Microsoft.CodeAnalysis;
using static MapTo.Sources.Constants;

namespace MapTo
{
    internal static class DiagnosticProvider
    {
        private const string UsageCategory = "Usage";
        private const string ErrorId = "MT0";
        private const string InfoId = "MT1";
        private const string WarningId = "MT2";

        internal static Diagnostic TypeNotFoundError(Location location, string syntaxName) =>
            Create($"{ErrorId}010", location, "Type not found.", $"Unable to find '{syntaxName}' type.");

        internal static Diagnostic MapFromAttributeNotFoundError(Location location) =>
            Create($"{ErrorId}020", location, "Attribute Not Available", $"Unable to find {MapFromAttributeSource.AttributeName} type.");

        internal static Diagnostic NoMatchingPropertyFoundError(Location location, INamedTypeSymbol classType, INamedTypeSymbol sourceType) =>
            Create($"{ErrorId}030", location, "Type Mismatch", $"No matching properties found between '{classType.ToDisplayString()}' and '{sourceType.ToDisplayString()}' types.");

        internal static Diagnostic NoMatchingPropertyTypeFoundError(IPropertySymbol property) =>
            Create($"{ErrorId}031", property.Locations.FirstOrDefault(), "Type Mismatch", $"Cannot create a map for '{property.ToDisplayString()}' property because source and destination types are not implicitly convertible. Consider using '{MapTypeConverterAttributeSource.FullyQualifiedName}' to provide a type converter or ignore the property using '{RootNamespace}.{IgnorePropertyAttributeSource.AttributeName}Attribute'.");

        internal static Diagnostic InvalidTypeConverterGenericTypesError(IPropertySymbol property, IPropertySymbol sourceProperty) =>
            Create($"{ErrorId}032", property.Locations.FirstOrDefault(), "Type Mismatch", $"Cannot map '{property.ToDisplayString()}' property because the annotated converter does not implement '{RootNamespace}.{TypeConverterSource.InterfaceName}<{sourceProperty.Type.ToDisplayString()}, {property.Type.ToDisplayString()}>'.");

        internal static Diagnostic ConfigurationParseError(string error) =>
            Create($"{ErrorId}040", Location.None, "Incorrect Configuration", error);

        private static Diagnostic Create(string id, Location? location, string title, string message, DiagnosticSeverity severity = DiagnosticSeverity.Error) =>
            Diagnostic.Create(new DiagnosticDescriptor(id, title, message, UsageCategory, severity, true), location ?? Location.None);
    }
}