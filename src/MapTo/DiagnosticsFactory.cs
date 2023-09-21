using Microsoft.CodeAnalysis.CSharp.Syntax;
using static MapTo.KnownTypes;

namespace MapTo;

internal static class DiagnosticsFactory
{
    private const string CodePrefix = "MT";
    private const string ErrorId = $"{CodePrefix}3";
    private const string InfoId = $"{CodePrefix}1";
    private const string UsageCategory = "Usage";
    private const string WarningId = $"{CodePrefix}2";

    internal static Diagnostic MissingPartialKeywordOnTargetClassError(Location location, string classTypeDisplayName) =>
        Create($"{ErrorId}01", location, $"Missing 'partial' keyword on '{classTypeDisplayName}' class.");

    internal static Diagnostic MissingConstructorOnTargetClassError(Location location, string classTypeDisplayName) =>
        Create($"{ErrorId}02", location, $"Missing constructor on '{classTypeDisplayName}' class.");

    internal static Diagnostic PropertyTypeConverterRequiredError(ISymbol property) => Create(
        $"{ErrorId}031",
        property.Locations.FirstOrDefault(),
        $"Cannot create a map for '{property.ToDisplayString()}' property because source and destination types are not implicitly convertible. Consider using '{PropertyTypeConverterAttributeName}' to provide a type converter or ignore the property using '{IgnorePropertyAttributeName}'.");

    internal static Diagnostic PropertyTypeConverterMethodNotFoundInTargetClassError(ISymbol property, AttributeData typeConverterAttribute) => Create(
        $"{ErrorId}032",
        typeConverterAttribute.GetFirstConstructorLocation(),
        $"Unable to find '{typeConverterAttribute.ConstructorArguments[0].Value}' method. Make sure a matching static method exists in the '{property.ContainingType.ToDisplayString()}' and it is accessible.");

    internal static Diagnostic PropertyTypeConverterMethodIsNotStaticError(IMethodSymbol converterMethodSymbol) => Create(
        $"{ErrorId}033",
        converterMethodSymbol.Locations.FirstOrDefault(),
        $"The '{converterMethodSymbol.Name}' method must be static.");

    internal static Diagnostic PropertyTypeConverterMethodReturnTypeCompatibilityError(IPropertySymbol property, IMethodSymbol converterMethodSymbol) => Create(
        $"{ErrorId}034",
        converterMethodSymbol.Locations.FirstOrDefault(),
        $"The '{converterMethodSymbol.Name}' method return type '{converterMethodSymbol.ReturnType.ToDisplayString()}' is not compatible with the '{property.ToDisplayString()}' property type '{property.Type.ToDisplayString()}'.");

    internal static Diagnostic PropertyTypeConverterMethodInputTypeCompatibilityError(IPropertySymbol sourceProperty, IMethodSymbol converterMethodSymbol) => Create(
        $"{ErrorId}035",
        converterMethodSymbol.Parameters[0].Locations.FirstOrDefault(),
        $"The input parameter type '{converterMethodSymbol.Parameters[0].Type.ToDisplayString()}' of the '{converterMethodSymbol.Name}' method is not compatible with the '{sourceProperty.ToDisplayString()}' property type '{sourceProperty.Type.ToDisplayString()}'.");

    internal static Diagnostic PropertyTypeConverterMethodAdditionalParametersTypeCompatibilityError(IMethodSymbol converterMethodSymbol) => Create(
        $"{ErrorId}036",
        converterMethodSymbol.Parameters[1].Locations.FirstOrDefault(),
        $"The additional parameters type '{converterMethodSymbol.Parameters[1].Type.ToDisplayString()}' of the '{converterMethodSymbol.Name}' method must be an object[].");

    internal static Diagnostic PropertyTypeConverterMethodAdditionalParametersIsMissingWarning(IPropertySymbol property, IMethodSymbol converterMethodSymbol) => Create(
        $"{WarningId}037",
        converterMethodSymbol.Locations.FirstOrDefault(),
        $"Additional parameters are provided to the '{property.ToDisplayString()}' property type converter, but the '{converterMethodSymbol.Name}' method does not have a second parameter of type object[].",
        DiagnosticSeverity.Warning);

    internal static Diagnostic PropertyTypeConverterMethodInputTypeNullCompatibilityError(IPropertySymbol sourceProperty, IMethodSymbol converterMethodSymbol) => Create(
        $"{ErrorId}038",
        converterMethodSymbol.Parameters[0].Locations.FirstOrDefault(),
        $"The property '{sourceProperty.ToDisplayString()}' is nullable, but the '{converterMethodSymbol.Name}' method parameter '{converterMethodSymbol.Parameters[0].Name}' is not.");

    internal static Diagnostic SuitableMappingTypeInNestedPropertyNotFoundError(IPropertySymbol property, ITypeSymbol sourceType) => Create(
        $"{ErrorId}039",
        property.Locations.FirstOrDefault(),
        $"Unable to find a suitable type to map '{property.ToDisplayString()}' property. Consider annotating '{sourceType.ToDisplayString()}' using '{MapFromAttributeName}' or ignore the property using '{IgnorePropertyAttributeName}'.");

    internal static Diagnostic SelfReferencingConstructorMappingError(Location location, string classTypeDisplayName) =>
        Create($"{ErrorId}040", location, $"Cannot create a map for '{classTypeDisplayName}' class because it has a self-referencing argument in the constructor.");

    private static Diagnostic Create(string id, Location? location, string message, DiagnosticSeverity severity = DiagnosticSeverity.Error) =>
        Diagnostic.Create(new DiagnosticDescriptor(id, string.Empty, message, UsageCategory, severity, true), location ?? Location.None);

    private static Location GetFirstConstructorLocation(this AttributeData attribute) =>
        attribute.ApplicationSyntaxReference?.GetSyntax().DescendantNodes().OfType<AttributeArgumentSyntax>().FirstOrDefault()?.GetLocation() ?? Location.None;
}