namespace MapTo.Diagnostics;

[SuppressMessage(category: "Style", checkId: "IDE0090:Use \'new(...)\'", Justification = "Is not supported. https://github.com/dotnet/roslyn-analyzers/issues/5828")]
[SuppressMessage(category: "StyleCop.CSharp.OrderingRules", checkId: "SA1202:Elements should be ordered by access", Justification = "To improve diagnostics messages readability.")]
[SuppressMessage(category: "StyleCop.CSharp.ReadabilityRules", checkId: "SA1118:Parameter should not span multiple lines", Justification ="To improve diagnostics messages readability.")]
internal static class DiagnosticDescriptors
{
    private const string CodePrefix = "MT";
    private const string UsageCategory = "Mapping";
    private const string InfoId = $"{CodePrefix}1";
    private const string WarningId = $"{CodePrefix}2";
    private const string ErrorId = $"{CodePrefix}3";

    internal static readonly DiagnosticDescriptor PropertyTypeConverterMethodAdditionalParametersIsMissingWarning = new DiagnosticDescriptor(
        id: $"{WarningId}001",
        title: string.Empty,
        messageFormat: "Additional parameters are provided to the '{0}' property type converter, but the '{1}' method does not have a second parameter of type object[]",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor MissingPartialKeywordOnTargetClassError = new DiagnosticDescriptor(
        id: $"{ErrorId}001",
        title: string.Empty,
        messageFormat: "Missing 'partial' keyword on '{0}' class",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor MissingConstructorOnTargetClassError = new DiagnosticDescriptor(
        id: $"{ErrorId}002",
        title: string.Empty,
        messageFormat: "Missing constructor on '{0}' class",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor PropertyTypeConverterRequiredError = new DiagnosticDescriptor(
        id: $"{ErrorId}003",
        title: string.Empty,
        messageFormat: "Cannot create a map for '{0}' property because source and destination types are not implicitly convertible. " +
                       $"Consider using '{nameof(PropertyTypeConverterAttribute)}' to provide a type converter or ignore the property using '{nameof(IgnorePropertyAttribute)}'.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor PropertyTypeConverterMethodNotFoundInTargetClassError = new DiagnosticDescriptor(
        id: $"{ErrorId}004",
        title: string.Empty,
        messageFormat: "Unable to find '{0}' method. Make sure a matching static method exists in the '{1}' and it is accessible.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor PropertyTypeConverterMethodIsNotStaticError = new DiagnosticDescriptor(
        id: $"{ErrorId}005",
        title: string.Empty,
        messageFormat: "The '{0}' method must be static",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor PropertyTypeConverterMethodReturnTypeCompatibilityError = new DiagnosticDescriptor(
        id: $"{ErrorId}006",
        title: string.Empty,
        messageFormat: "The '{0}' method return type '{1}' is not compatible with the '{2}' property type '{3}'",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor PropertyTypeConverterMethodInputTypeCompatibilityError = new DiagnosticDescriptor(
        id: $"{ErrorId}007",
        title: string.Empty,
        messageFormat: "The input parameter type '{0}' of the '{1}' method is not compatible with the '{2}' property type '{3}'",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor PropertyTypeConverterMethodAdditionalParametersTypeCompatibilityError = new DiagnosticDescriptor(
        id: $"{ErrorId}008",
        title: string.Empty,
        messageFormat: "The additional parameters type '{0}' of the '{1}' method must be an object[]",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor PropertyTypeConverterMethodInputTypeNullCompatibilityError = new DiagnosticDescriptor(
        id: $"{ErrorId}009",
        title: string.Empty,
        messageFormat: "The property '{0}' is nullable, but the '{1}' method parameter '{2}' is not",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor SuitableMappingTypeInNestedPropertyNotFoundError = new DiagnosticDescriptor(
        id: $"{ErrorId}010",
        title: string.Empty,
        messageFormat: "Unable to find a suitable type to map '{0}' property. Consider annotating '{1}' " +
                       $"using '{nameof(MapFromAttribute)}' or ignore the property using '{nameof(IgnorePropertyAttribute)}'.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor SelfReferencingConstructorMappingError = new DiagnosticDescriptor(
        id: $"{ErrorId}011",
        title: string.Empty,
        messageFormat: "Cannot create a map for '{0}' class because it has a self-referencing argument in the constructor",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor BeforeOrAfterMapMethodNotFoundError = new DiagnosticDescriptor(
        id: $"{ErrorId}012",
        title: string.Empty,
        messageFormat: "Unable to find '{0}' method. Make sure a matching static method exists it is accessible.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor BeforeOrAfterMapMethodInvalidParameterError = new DiagnosticDescriptor(
        id: $"{ErrorId}013",
        title: string.Empty,
        messageFormat: "The '{0}' method must have either no argument or a single argument assignable to the '{1}'",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor BeforeOrAfterMapMethodInvalidReturnTypeError = new DiagnosticDescriptor(
        id: $"{ErrorId}014",
        title: string.Empty,
        messageFormat: "The '{0}' method must return void or a type that is assignable to the type '{1}'",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor BeforeOrAfterMapMethodMissingParameterError = new DiagnosticDescriptor(
        id: $"{ErrorId}015",
        title: string.Empty,
        messageFormat: "The '{0}' method must return void or a single argument assignable to the type '{1}'",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor BeforeOrAfterMapMethodMissingParameterNullabilityAnnotationError = new DiagnosticDescriptor(
        id: $"{ErrorId}016",
        title: string.Empty,
        messageFormat: "The argument passed to the '{0}' method might be null but it is not annotated with nullability annotation. " +
                       "Consider annotating the argument with nullability annotation or disable nullable reference types.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor BeforeOrAfterMapMethodMissingReturnTypeNullabilityAnnotationError = new DiagnosticDescriptor(
        id: $"{ErrorId}017",
        title: string.Empty,
        messageFormat: "The '{0}' method return type might be null but it is not annotated with nullability annotation. " +
                       "Consider annotating the argument with nullability annotation or disable nullable reference types.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}