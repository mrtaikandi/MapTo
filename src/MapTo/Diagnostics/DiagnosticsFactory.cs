using MapTo.Mappings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.Diagnostics;

[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "To improve diagnostics messages readability.")]
internal static class DiagnosticsFactory
{
    /// <summary>
    /// Additional parameters are provided to the '{0}' property type converter, but the '{1}' method does not have a second parameter of type object[].
    /// </summary>
    internal static Diagnostic PropertyTypeConverterMethodAdditionalParametersIsMissingWarning(IPropertySymbol property, IMethodSymbol converterMethodSymbol) => Create(
        DiagnosticDescriptors.PropertyTypeConverterMethodAdditionalParametersIsMissingWarning,
        converterMethodSymbol.GetLocation(),
        property.ToDisplayString(),
        converterMethodSymbol.Name);

    /// <summary>
    /// Missing 'partial' keyword on '{0}' class.
    /// </summary>
    internal static Diagnostic MissingPartialKeywordOnTargetClassError(Location location, string classTypeDisplayName) => Create(
        DiagnosticDescriptors.MissingPartialKeywordOnTargetClassError,
        location,
        classTypeDisplayName);

    /// <summary>
    /// Missing constructor on '{0}' class.
    /// </summary>
    internal static Diagnostic MissingConstructorOnTargetClassError(Location location, string classTypeDisplayName) => Create(
        DiagnosticDescriptors.MissingConstructorOnTargetClassError,
        location,
        classTypeDisplayName);

    /// <summary>
    /// Cannot create a map for '{0}' property because source and destination types are not implicitly convertible.
    /// Consider using '{1}' to provide a type converter or ignore the property using '{2}'.
    /// </summary>
    internal static Diagnostic PropertyTypeConverterRequiredError(ISymbol property) => Create(
        DiagnosticDescriptors.PropertyTypeConverterRequiredError,
        property.GetLocation(),
        property.ToDisplayString());

    /// <summary>
    /// Unable to find '{0}' method. Make sure a matching static method exists in the '{1}' and it is accessible.
    /// </summary>
    internal static Diagnostic PropertyTypeConverterMethodNotFoundInTargetClassError(ISymbol property, AttributeData typeConverterAttribute) => Create(
        DiagnosticDescriptors.PropertyTypeConverterMethodNotFoundInTargetClassError,
        typeConverterAttribute.GetFirstConstructorLocation(),
        typeConverterAttribute.ConstructorArguments[0].Value,
        property.ContainingType.ToDisplayString());

    /// <summary>
    /// The '{0}' method must be static.
    /// </summary>
    internal static Diagnostic PropertyTypeConverterMethodIsNotStaticError(IMethodSymbol converterMethodSymbol) => Create(
        DiagnosticDescriptors.PropertyTypeConverterMethodIsNotStaticError,
        converterMethodSymbol.GetLocation(),
        converterMethodSymbol.Name);

    /// <summary>
    /// The '{0}' method return type '{1}' is not compatible with the '{2}' property type '{3}'.
    /// </summary>
    internal static Diagnostic PropertyTypeConverterMethodReturnTypeCompatibilityError(IPropertySymbol property, IMethodSymbol converterMethodSymbol) => Create(
        DiagnosticDescriptors.PropertyTypeConverterMethodReturnTypeCompatibilityError,
        converterMethodSymbol.GetLocation(),
        converterMethodSymbol.Name,
        converterMethodSymbol.ReturnType.ToDisplayString(),
        property.ToDisplayString(),
        property.Type.ToDisplayString());

    /// <summary>
    /// The input parameter type '{0}' of the '{1}' method is not compatible with the '{2}' property type '{3}'.
    /// </summary>
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

    /// <summary>
    /// The additional parameters type '{0}' of the '{1}' method must be an object[].
    /// </summary>
    internal static Diagnostic PropertyTypeConverterMethodAdditionalParametersTypeCompatibilityError(IMethodSymbol converterMethodSymbol) => Create(
        DiagnosticDescriptors.PropertyTypeConverterMethodAdditionalParametersTypeCompatibilityError,
        converterMethodSymbol.Parameters[1].GetLocation(),
        converterMethodSymbol.Parameters[1].Type.ToDisplayString(),
        converterMethodSymbol.Name);

    /// <summary>
    /// The property '{0}' is nullable, but the '{1}' method parameter '{2}' is not.
    /// </summary>
    internal static Diagnostic PropertyTypeConverterMethodInputTypeNullCompatibilityError(string sourcePropertyName, IMethodSymbol converterMethodSymbol) => Create(
        DiagnosticDescriptors.PropertyTypeConverterMethodInputTypeNullCompatibilityError,
        converterMethodSymbol.Parameters[0].GetLocation(),
        sourcePropertyName,
        converterMethodSymbol.Name,
        converterMethodSymbol.Parameters[0].Name);

    /// <summary>
    /// Unable to find a suitable type to map '{0}' property. Consider annotating '{1}' using '{nameof(MapFromAttribute)}'
    /// or ignore the property using '{nameof(IgnorePropertyAttribute)}'.
    /// </summary>
    internal static Diagnostic SuitableMappingTypeInNestedPropertyNotFoundError(IPropertySymbol property, ITypeSymbol sourceType) => Create(
        DiagnosticDescriptors.SuitableMappingTypeInNestedPropertyNotFoundError,
        property.GetLocation(),
        property.ToDisplayString(),
        sourceType.ToDisplayString());

    /// <summary>
    /// Cannot create a map for '{0}' class because it has a self-referencing argument in the constructor.
    /// </summary>
    internal static Diagnostic SelfReferencingConstructorMappingError(Location location, string classTypeDisplayName) => Create(
        DiagnosticDescriptors.SelfReferencingConstructorMappingError,
        location,
        classTypeDisplayName);

    /// <summary>
    /// Unable to find '{0}' method. Make sure a matching static method exists and it is accessible.
    /// </summary>
    internal static Diagnostic BeforeMapMethodNotFoundError(MappingConfiguration mapPropertyAttribute) => Create(
        DiagnosticDescriptors.BeforeOrAfterMapMethodNotFoundError,
        mapPropertyAttribute.BeforeMapArgumentLocation,
        mapPropertyAttribute.BeforeMap?.ToString().Trim('\"'));

    /// <summary>
    /// Unable to find '{0}' method. Make sure a matching static method exists and it is accessible.
    /// </summary>
    internal static Diagnostic AfterMapMethodNotFoundError(MappingConfiguration mapPropertyAttribute) => Create(
        DiagnosticDescriptors.BeforeOrAfterMapMethodNotFoundError,
        mapPropertyAttribute.AfterMapArgumentLocation,
        mapPropertyAttribute.AfterMap?.ToString().Trim('\"'));

    /// <summary>
    /// The '{0}' method must have either no argument or a single argument assignable to the '{1}'.
    /// </summary>
    internal static Diagnostic AfterMapMethodInvalidParameterError(MappingConfiguration mapPropertyAttribute, ITypeSymbol sourceType) => Create(
        DiagnosticDescriptors.BeforeOrAfterMapMethodInvalidParameterError,
        mapPropertyAttribute.AfterMapArgumentLocation,
        mapPropertyAttribute.AfterMap?.ToString().Trim('\"'),
        sourceType.ToDisplayString());

    /// <summary>
    /// The '{0}' method must have either no argument or a single argument assignable to the '{1}'.
    /// </summary>
    internal static Diagnostic BeforeMapMethodInvalidParameterError(MappingConfiguration mapPropertyAttribute, ITypeSymbol sourceType) => Create(
        DiagnosticDescriptors.BeforeOrAfterMapMethodInvalidParameterError,
        mapPropertyAttribute.BeforeMapArgumentLocation,
        mapPropertyAttribute.BeforeMap?.ToString().Trim('\"'),
        sourceType.ToDisplayString());

    /// <summary>
    /// The '{0}' method must return void or a type that is assignable to the type '{1}'.
    /// </summary>
    internal static Diagnostic AfterMapMethodInvalidReturnTypeError(MappingConfiguration mapPropertyAttribute, ITypeSymbol sourceType) => Create(
        DiagnosticDescriptors.BeforeOrAfterMapMethodInvalidReturnTypeError,
        mapPropertyAttribute.AfterMapArgumentLocation,
        mapPropertyAttribute.AfterMap?.ToString().Trim('\"'),
        sourceType.ToDisplayString());

    /// <summary>
    /// The '{0}' method must return void or a type that is assignable to the type '{1}'.
    /// </summary>
    internal static Diagnostic BeforeMapMethodInvalidReturnTypeError(MappingConfiguration mapPropertyAttribute,  ITypeSymbol sourceType) => Create(
        DiagnosticDescriptors.BeforeOrAfterMapMethodInvalidReturnTypeError,
        mapPropertyAttribute.BeforeMapArgumentLocation,
        mapPropertyAttribute.BeforeMap?.ToString().Trim('\"'),
        sourceType.ToDisplayString());

    /// <summary>
    /// The '{0}' method must return void or a single argument assignable to the type '{1}'.
    /// </summary>
    internal static Diagnostic AfterMapMethodMissingParameterError(MappingConfiguration mapPropertyAttribute, ITypeSymbol sourceType) => Create(
        DiagnosticDescriptors.BeforeOrAfterMapMethodMissingParameterError,
        mapPropertyAttribute.AfterMapArgumentLocation,
        mapPropertyAttribute.AfterMap?.ToString().Trim('\"'),
        sourceType.ToDisplayString());

    /// <summary>
    /// The '{0}' method must return void or a single argument assignable to the type '{1}'.
    /// </summary>
    internal static Diagnostic BeforeMapMethodMissingParameterError(MappingConfiguration mapPropertyAttribute, ITypeSymbol sourceType) => Create(
        DiagnosticDescriptors.BeforeOrAfterMapMethodMissingParameterError,
        mapPropertyAttribute.BeforeMapArgumentLocation,
        mapPropertyAttribute.BeforeMap?.ToString().Trim('\"'),
        sourceType.ToDisplayString());

    /// <summary>
    /// The argument passed to the '{0}' method might be null but it is not annotated with nullability annotation.
    /// </summary>
    internal static Diagnostic AfterMapMethodMissingParameterNullabilityAnnotationError(MappingConfiguration mapPropertyAttribute) => Create(
        DiagnosticDescriptors.BeforeOrAfterMapMethodMissingParameterNullabilityAnnotationError,
        mapPropertyAttribute.AfterMapArgumentLocation,
        mapPropertyAttribute.AfterMap?.ToString().Trim('\"'));

    /// <summary>
    /// The argument passed to the '{0}' method might be null but it is not annotated with nullability annotation.
    /// </summary>
    internal static Diagnostic BeforeMapMethodMissingParameterNullabilityAnnotationError(MappingConfiguration mapPropertyAttribute) => Create(
        DiagnosticDescriptors.BeforeOrAfterMapMethodMissingParameterNullabilityAnnotationError,
        mapPropertyAttribute.BeforeMapArgumentLocation,
        mapPropertyAttribute.BeforeMap?.ToString().Trim('\"'));

    /// <summary>
    /// The '{0}' method return type might be null but it is not annotated with nullability annotation.
    /// </summary>
    internal static Diagnostic AfterMapMethodMissingReturnTypeNullabilityAnnotationError(MappingConfiguration mapPropertyAttribute) => Create(
        DiagnosticDescriptors.BeforeOrAfterMapMethodMissingReturnTypeNullabilityAnnotationError,
        mapPropertyAttribute.AfterMapArgumentLocation,
        mapPropertyAttribute.AfterMap?.ToString().Trim('\"'));

    /// <summary>
    /// The '{0}' method return type might be null but it is not annotated with nullability annotation.
    /// </summary>
    internal static Diagnostic BeforeMapMethodMissingReturnTypeNullabilityAnnotationError(MappingConfiguration mapPropertyAttribute) => Create(
        DiagnosticDescriptors.BeforeOrAfterMapMethodMissingReturnTypeNullabilityAnnotationError,
        mapPropertyAttribute.BeforeMapArgumentLocation,
        mapPropertyAttribute.BeforeMap?.ToString().Trim('\"'));

    /// <summary>
    /// The '{0}' method must have either no argument, a single argument assignable to the '{1}' or two arguments assignable to the '{1}' and '{2}'.
    /// </summary>
    internal static Diagnostic AfterMapMethodInvalidParametersError(MappingConfiguration mapPropertyAttribute, ITypeSymbol sourceType, ITypeSymbol targetType) =>
        Create(
            DiagnosticDescriptors.AfterMapMethodInvalidParametersError,
            mapPropertyAttribute.AfterMapArgumentLocation,
            mapPropertyAttribute.AfterMap?.ToString().Trim('\"'),
            targetType.ToDisplayString(),
            sourceType.ToDisplayString());

    /// <summary>
    /// The '{0}' enum member is not defined in the target enum '{1}'.
    /// </summary>
    internal static Diagnostic StringEnumMappingSourceOnlyError(IFieldSymbol missingEnumMember, ITypeSymbol targetEnumType) => Create(
        DiagnosticDescriptors.StringEnumMappingSourceOnlyError,
        targetEnumType.GetLocation(),
        missingEnumMember.ToDisplayString(),
        targetEnumType.ToDisplayString());

    /// <summary>
    /// The '{0}' enum member is not defined in the source enum '{1}'.
    /// </summary>
    internal static Diagnostic StringEnumMappingTargetOnlyError(IFieldSymbol missingEnumMember, ITypeSymbol sourceEnumType) => Create(
        DiagnosticDescriptors.StringEnumMappingTargetOnlyError,
        missingEnumMember.GetLocation(),
        missingEnumMember.ToDisplayString(),
        sourceEnumType.ToDisplayString());

    /// <summary>
    /// The '{0}' cannot have argument when applied to an enum member.
    /// </summary>
    internal static Diagnostic IgnoreEnumMemberWithParameterOnMemberError(AttributeData enumMemberAttribute, ITypeSymbol ignoreEnumMemberAttributeSymbol) => Create(
        DiagnosticDescriptors.IgnoreEnumMemberWithParameterOnMemberError,
        enumMemberAttribute.GetLocation(),
        ignoreEnumMemberAttributeSymbol.ToDisplayString());

    /// <summary>
    /// The '{0}' must have an argument when applied to an enum or class.
    /// </summary>
    internal static Diagnostic IgnoreEnumMemberWithoutParameterTypeError(AttributeData enumMemberAttribute, ITypeSymbol ignoreEnumMemberAttributeSymbol) => Create(
        DiagnosticDescriptors.IgnoreEnumMemberWithoutParameterTypeError,
        enumMemberAttribute.GetLocation(),
        ignoreEnumMemberAttributeSymbol.ToDisplayString());

    /// <summary>
    /// The mapping configuration method must have one parameter of type '{0}&lt;{1}, {2}&gt;'.
    /// </summary>
    internal static Diagnostic IncorrectConfigurationMethodParameters(IMethodSymbol configurationMethod, ITypeSymbol sourceType, ITypeSymbol targetType) => Create(
        DiagnosticDescriptors.IncorrectConfigurationMethodParameters,
        configurationMethod.GetLocation(),
        KnownTypes.MappingConfigurationFullName,
        sourceType.ToDisplayString(),
        targetType.ToDisplayString());

    private static Diagnostic Create(DiagnosticDescriptor descriptor, Location? location, params object?[] messageArgs) =>
        Diagnostic.Create(descriptor, location ?? Location.None, messageArgs);

    private static Location? GetFirstConstructorLocation(this AttributeData attribute) =>
        attribute.ApplicationSyntaxReference?.GetSyntax().DescendantNodes().OfType<AttributeArgumentSyntax>().FirstOrDefault()?.GetLocation();
}