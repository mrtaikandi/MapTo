using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo;

[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "To improve diagnostics messages readability.")]
internal static class DiagnosticsFactory
{
    private const string CodePrefix = "MT";
    private const string UsageCategory = "Usage";
    private const string InfoId = $"{CodePrefix}1";
    private const string WarningId = $"{CodePrefix}2";
    private const string ErrorId = $"{CodePrefix}3";

    internal static Diagnostic MissingPartialKeywordOnTargetClassError(Location location, string classTypeDisplayName) =>
        Create($"{ErrorId}01", location, $"Missing 'partial' keyword on '{classTypeDisplayName}' class.");

    internal static Diagnostic MissingConstructorOnTargetClassError(Location location, string classTypeDisplayName) =>
        Create($"{ErrorId}02", location, $"Missing constructor on '{classTypeDisplayName}' class.");

    internal static Diagnostic PropertyTypeConverterRequiredError(ISymbol property) => Create(
        $"{ErrorId}031",
        property.Locations.FirstOrDefault(),
        $"Cannot create a map for '{property.ToDisplayString()}' property because source and destination types are not implicitly convertible. Consider using '{nameof(PropertyTypeConverterAttribute)}' to provide a type converter or ignore the property using '{nameof(IgnorePropertyAttribute)}'.");

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
        $"Unable to find a suitable type to map '{property.ToDisplayString()}' property. Consider annotating '{sourceType.ToDisplayString()}' using '{nameof(MapFromAttribute)}' or ignore the property using '{nameof(IgnorePropertyAttribute)}'.");

    internal static Diagnostic SelfReferencingConstructorMappingError(Location location, string classTypeDisplayName) =>
        Create($"{ErrorId}040", location, $"Cannot create a map for '{classTypeDisplayName}' class because it has a self-referencing argument in the constructor.");

    internal static Diagnostic BeforeOrAfterMapMethodNotFoundError(AttributeData mapPropertyAttribute, string argumentName) => Create(
        $"{ErrorId}041",
        mapPropertyAttribute.GetNamedArgumentLocation(argumentName),
        $"Unable to find '{mapPropertyAttribute.GetNamedArgument(argumentName)}' method. " +
        $"Make sure a matching static method exists it is accessible.");

    internal static Diagnostic BeforeOrAfterMapMethodInvalidParameterError(AttributeData mapPropertyAttribute, string argumentName, ITypeSymbol sourceType) => Create(
        $"{ErrorId}042",
        mapPropertyAttribute.GetNamedArgumentLocation(argumentName),
        $"The '{mapPropertyAttribute.GetNamedArgument(argumentName)}' method must have either no argument or a single argument assignable to the '{sourceType.ToDisplayString()}'.");

    internal static Diagnostic BeforeOrAfterMapMethodInvalidReturnTypeError(AttributeData mapPropertyAttribute, string argumentName, ITypeSymbol sourceType) => Create(
        $"{ErrorId}043",
        mapPropertyAttribute.GetNamedArgumentLocation(argumentName),
        $"The '{mapPropertyAttribute.GetNamedArgument(argumentName)}' method must return void or a type that is assignable to the type '{sourceType.ToDisplayString()}'.");

    internal static Diagnostic BeforeOrAfterMapMethodMissingParameterError(AttributeData mapPropertyAttribute, string argumentName, ITypeSymbol sourceType) => Create(
        $"{ErrorId}044",
        mapPropertyAttribute.GetNamedArgumentLocation(argumentName),
        $"The '{mapPropertyAttribute.GetNamedArgument(argumentName)}' method must return void or a single argument assignable to the type '{sourceType.ToDisplayString()}'.");

    internal static Diagnostic BeforeOrAfterMapMethodMissingParameterNullabilityAnnotationError(AttributeData mapPropertyAttribute, string argumentName) => Create(
        $"{ErrorId}044",
        mapPropertyAttribute.GetNamedArgumentLocation(argumentName),
        $"The argument passed to the '{mapPropertyAttribute.GetNamedArgument(argumentName)}' method might be null but it is not annotated with nullability annotation. " +
        $"Consider annotating the argument with nullability annotation or disable nullable reference types.");

    internal static Diagnostic BeforeOrAfterMapMethodMissingReturnTypeNullabilityAnnotationError(AttributeData mapPropertyAttribute, string argumentName) => Create(
        $"{ErrorId}045",
        mapPropertyAttribute.GetNamedArgumentLocation(argumentName),
        $"The '{mapPropertyAttribute.GetNamedArgument(argumentName)}' method return type might be null but it is not annotated with nullability annotation. " +
        $"Consider annotating the argument with nullability annotation or disable nullable reference types.");

    private static Diagnostic Create(string id, Location? location, string message, DiagnosticSeverity severity = DiagnosticSeverity.Error) =>
        Diagnostic.Create(new DiagnosticDescriptor(id, string.Empty, message, UsageCategory, severity, true), location ?? Location.None);

    private static Location GetFirstConstructorLocation(this AttributeData attribute) =>
        attribute.ApplicationSyntaxReference?.GetSyntax().DescendantNodes().OfType<AttributeArgumentSyntax>().FirstOrDefault()?.GetLocation() ?? Location.None;

    private static Location GetNamedArgumentLocation(this AttributeData attribute, string name) => attribute.ApplicationSyntaxReference?.GetSyntax()
        .DescendantNodes()
        .OfType<AttributeArgumentSyntax>()
        .FirstOrDefault(a => a.NameEquals?.Name.Identifier.ValueText == name)?.GetLocation() ?? Location.None;
}