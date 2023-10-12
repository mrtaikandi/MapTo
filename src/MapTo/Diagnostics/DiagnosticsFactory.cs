using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.Diagnostics;

[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "To improve diagnostics messages readability.")]
internal static class DiagnosticsFactory
{
    internal static Diagnostic PropertyTypeConverterMethodAdditionalParametersIsMissingWarning(IPropertySymbol property, IMethodSymbol converterMethodSymbol) => Create(
        DiagnosticDescriptors.PropertyTypeConverterMethodAdditionalParametersIsMissingWarning,
        converterMethodSymbol.GetLocation(),
        property.ToDisplayString(),
        converterMethodSymbol.Name);

    internal static Diagnostic MissingPartialKeywordOnTargetClassError(Location location, string classTypeDisplayName) => Create(
        DiagnosticDescriptors.MissingPartialKeywordOnTargetClassError,
        location,
        classTypeDisplayName);

    internal static Diagnostic MissingConstructorOnTargetClassError(TypeDeclarationSyntax targetType, string classTypeDisplayName) => Create(
        DiagnosticDescriptors.MissingConstructorOnTargetClassError,
        targetType.GetLocation(),
        classTypeDisplayName);

    internal static Diagnostic PropertyTypeConverterRequiredError(ISymbol property) => Create(
        DiagnosticDescriptors.PropertyTypeConverterRequiredError,
        property.GetLocation(),
        property.ToDisplayString());

    internal static Diagnostic PropertyTypeConverterMethodNotFoundInTargetClassError(ISymbol property, AttributeData typeConverterAttribute) => Create(
        DiagnosticDescriptors.PropertyTypeConverterMethodNotFoundInTargetClassError,
        typeConverterAttribute.GetFirstConstructorLocation(),
        typeConverterAttribute.ConstructorArguments[0].Value,
        property.ContainingType.ToDisplayString());

    internal static Diagnostic PropertyTypeConverterMethodIsNotStaticError(IMethodSymbol converterMethodSymbol) => Create(
        DiagnosticDescriptors.PropertyTypeConverterMethodIsNotStaticError,
        converterMethodSymbol.GetLocation(),
        converterMethodSymbol.Name);

    internal static Diagnostic PropertyTypeConverterMethodReturnTypeCompatibilityError(IPropertySymbol property, IMethodSymbol converterMethodSymbol) => Create(
        DiagnosticDescriptors.PropertyTypeConverterMethodReturnTypeCompatibilityError,
        converterMethodSymbol.GetLocation(),
        converterMethodSymbol.Name,
        converterMethodSymbol.ReturnType.ToDisplayString(),
        property.ToDisplayString(),
        property.Type.ToDisplayString());

    internal static Diagnostic PropertyTypeConverterMethodInputTypeCompatibilityError(
        string sourcePropertyName,
        ITypeSymbol sourcePropertyType,
        IMethodSymbol converterMethodSymbol) =>
        Create(
            DiagnosticDescriptors.PropertyTypeConverterMethodInputTypeCompatibilityError,
            converterMethodSymbol.Parameters[0].GetLocation(),
            converterMethodSymbol.Parameters[0].Type.ToDisplayString(),
            converterMethodSymbol.Name,
            sourcePropertyName,
            sourcePropertyType.ToDisplayString());

    internal static Diagnostic PropertyTypeConverterMethodAdditionalParametersTypeCompatibilityError(IMethodSymbol converterMethodSymbol) => Create(
        DiagnosticDescriptors.PropertyTypeConverterMethodAdditionalParametersTypeCompatibilityError,
        converterMethodSymbol.Parameters[1].GetLocation(),
        converterMethodSymbol.Parameters[1].Type.ToDisplayString(),
        converterMethodSymbol.Name);

    internal static Diagnostic PropertyTypeConverterMethodInputTypeNullCompatibilityError(string sourcePropertyName, IMethodSymbol converterMethodSymbol) => Create(
        DiagnosticDescriptors.PropertyTypeConverterMethodInputTypeNullCompatibilityError,
        converterMethodSymbol.Parameters[0].GetLocation(),
        sourcePropertyName,
        converterMethodSymbol.Name,
        converterMethodSymbol.Parameters[0].Name);

    internal static Diagnostic SuitableMappingTypeInNestedPropertyNotFoundError(IPropertySymbol property, ITypeSymbol sourceType) => Create(
        DiagnosticDescriptors.SuitableMappingTypeInNestedPropertyNotFoundError,
        property.GetLocation(),
        property.ToDisplayString(),
        sourceType.ToDisplayString());

    internal static Diagnostic SelfReferencingConstructorMappingError(Location location, string classTypeDisplayName) => Create(
        DiagnosticDescriptors.SelfReferencingConstructorMappingError,
        location,
        classTypeDisplayName);

    internal static Diagnostic BeforeOrAfterMapMethodNotFoundError(AttributeData mapPropertyAttribute, string argumentName) => Create(
        DiagnosticDescriptors.BeforeOrAfterMapMethodNotFoundError,
        mapPropertyAttribute.GetNamedArgumentLocation(argumentName),
        mapPropertyAttribute.GetNamedArgument(argumentName));

    internal static Diagnostic BeforeOrAfterMapMethodInvalidParameterError(AttributeData mapPropertyAttribute, string argumentName, ITypeSymbol sourceType) => Create(
        DiagnosticDescriptors.BeforeOrAfterMapMethodInvalidParameterError,
        mapPropertyAttribute.GetNamedArgumentLocation(argumentName),
        mapPropertyAttribute.GetNamedArgument(argumentName),
        sourceType.ToDisplayString());

    internal static Diagnostic BeforeOrAfterMapMethodInvalidReturnTypeError(AttributeData mapPropertyAttribute, string argumentName, ITypeSymbol sourceType) => Create(
        DiagnosticDescriptors.BeforeOrAfterMapMethodInvalidReturnTypeError,
        mapPropertyAttribute.GetNamedArgumentLocation(argumentName),
        mapPropertyAttribute.GetNamedArgument(argumentName),
        sourceType.ToDisplayString());

    internal static Diagnostic BeforeOrAfterMapMethodMissingParameterError(AttributeData mapPropertyAttribute, string argumentName, ITypeSymbol sourceType) => Create(
        DiagnosticDescriptors.BeforeOrAfterMapMethodMissingParameterError,
        mapPropertyAttribute.GetNamedArgumentLocation(argumentName),
        mapPropertyAttribute.GetNamedArgument(argumentName),
        sourceType.ToDisplayString());

    internal static Diagnostic BeforeOrAfterMapMethodMissingParameterNullabilityAnnotationError(AttributeData mapPropertyAttribute, string argumentName) => Create(
        DiagnosticDescriptors.BeforeOrAfterMapMethodMissingParameterNullabilityAnnotationError,
        mapPropertyAttribute.GetNamedArgumentLocation(argumentName),
        mapPropertyAttribute.GetNamedArgument(argumentName));

    internal static Diagnostic BeforeOrAfterMapMethodMissingReturnTypeNullabilityAnnotationError(AttributeData mapPropertyAttribute, string argumentName) => Create(
        DiagnosticDescriptors.BeforeOrAfterMapMethodMissingReturnTypeNullabilityAnnotationError,
        mapPropertyAttribute.GetNamedArgumentLocation(argumentName),
        mapPropertyAttribute.GetNamedArgument(argumentName));

    private static Diagnostic Create(DiagnosticDescriptor descriptor, Location? location, params object?[] messageArgs) =>
        Diagnostic.Create(descriptor, location ?? Location.None, messageArgs);

    private static Location? GetFirstConstructorLocation(this AttributeData attribute) =>
        attribute.ApplicationSyntaxReference?.GetSyntax().DescendantNodes().OfType<AttributeArgumentSyntax>().FirstOrDefault()?.GetLocation();

    private static Location GetNamedArgumentLocation(this AttributeData attribute, string name) => attribute.ApplicationSyntaxReference?.GetSyntax()
        .DescendantNodes()
        .OfType<AttributeArgumentSyntax>()
        .FirstOrDefault(a => a.NameEquals?.Name.Identifier.ValueText == name)?.GetLocation() ?? Location.None;
}