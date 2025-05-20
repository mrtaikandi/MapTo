namespace MapTo.Mappings;

internal readonly record struct TargetMapping(
    Accessibility Modifier,
    string Name,
    NamespaceMapping Namespace,
    bool IsPartial,
    string TypeKeyword,
    TypeKind TypeKind,
    string ExtensionClassName,
    SourceMapping Source,
    ConstructorMapping Constructor,
    ImmutableArray<PropertyMapping> Properties,
    Location Location,
    MethodMapping BeforeMapMethod,
    MethodMapping AfterMapMethod,
    ImmutableArray<ProjectionMapping> Projections,
    TypeConverterMapping? TypeConverter,
    CodeGeneratorOptions Options);

internal static class TargetMappingFactory
{
    public static TargetMapping Create(MappingContext context)
    {
        var (targetTypeSyntax, targetTypeSymbol, _, sourceTypeSymbol, _, codeGeneratorOptions, _) = context;

        var properties = PropertyMappingFactory.Create(context);

        var constructorMapping = ConstructorMappingFactory.Create(context, properties);
        properties = properties.ExceptConstructorInitializers(constructorMapping);

        var ns = NamespaceMapping.Create(targetTypeSymbol);
        var name = targetTypeSyntax?.Identifier.Text ?? targetTypeSymbol.Name;
        var source = SourceMapping.Create(sourceTypeSymbol);
        var extensionClassName = context.CodeGeneratorOptions.GenerateFullExtensionClassNames
            ? $"{source.GetFullClassName()}To{GetFullClassName(ns, name)}{context.CodeGeneratorOptions.MapExtensionClassSuffix}"
            : $"{source.Name}To{name}{context.CodeGeneratorOptions.MapExtensionClassSuffix}";

        var mapping = new TargetMapping(
            Modifier: targetTypeSyntax?.GetAccessibility() ?? targetTypeSymbol.DeclaredAccessibility,
            Name: name,
            Namespace: ns,
            IsPartial: targetTypeSyntax?.IsPartial() ?? false,
            TypeKeyword: targetTypeSyntax?.GetKeywordText() ?? targetTypeSymbol.TypeKind.ToKeywordText(),
            TypeKind: targetTypeSymbol.TypeKind,
            ExtensionClassName: extensionClassName,
            Source: source,
            Constructor: constructorMapping,
            Properties: properties,
            Location: targetTypeSyntax?.Identifier.GetLocation() ?? targetTypeSymbol.GetLocation() ?? Location.None,
            BeforeMapMethod: MethodMapping.CreateBeforeMapMethod(context),
            AfterMapMethod: MethodMapping.CreateAfterMapMethod(context),
            Projections: ProjectionMapping.Create(context),
            TypeConverter: CreateTypeConverter(context),
            Options: codeGeneratorOptions with
            {
                ReferenceHandling = context.UseReferenceHandling(properties)
            });

        return mapping.IsValid(context) ? mapping : default;
    }

    internal static string GetReturnType(this TargetMapping mapping) =>
        mapping.UseFullyQualifiedName() ? mapping.ToFullyQualifiedName() : mapping.Name;

    internal static string GetSourceType(this TargetMapping mapping) =>
        mapping.UseFullyQualifiedName() ? mapping.Source.ToFullyQualifiedName() : mapping.Source.Name;

    internal static string GetFullName(this TargetMapping mapping) => $"{mapping.Namespace}.{mapping.Name}";

    private static string ToFullyQualifiedName(this TargetMapping mapping) =>
        mapping.Namespace.IsGlobalNamespace ? mapping.Name : $"global::{mapping.Namespace}.{mapping.Name}";

    private static bool UseFullyQualifiedName(this TargetMapping mapping) =>
        mapping.Namespace != mapping.Source.Namespace;

    private static string GetFullClassName(NamespaceMapping ns, string name) =>
        $"{ns.Value.Replace(".", string.Empty)}{name}";

    private static ReferenceHandling UseReferenceHandling(this MappingContext context, ImmutableArray<PropertyMapping> properties)
    {
        var targetType = context.TargetTypeSymbol.ToTypeMapping();
        return context.CodeGeneratorOptions.ReferenceHandling == ReferenceHandling.Auto &&
               properties.Any(p => p.Type == targetType || context.TargetTypeSymbol.HasNonPrimitiveProperties())
            ? ReferenceHandling.Enabled
            : context.CodeGeneratorOptions.ReferenceHandling;
    }

    private static bool IsValid(this TargetMapping mapping, MappingContext context)
    {
        if (mapping is { IsPartial: false, Constructor.IsGenerated: true })
        {
            context.ReportDiagnostic(DiagnosticsFactory.MissingPartialKeywordOnTargetClassError(mapping.Location, mapping.Name));
            return false;
        }

        return true;
    }

    private static TypeConverterMapping? CreateTypeConverter(MappingContext context)
    {
        if (context.TargetTypeSymbol.TypeKind is not TypeKind.Enum)
        {
            return null;
        }

        var methodPrefix = context.CodeGeneratorOptions.MapMethodPrefix;

        return new TypeConverterMapping(
            Method: new MethodMapping(
                ContainingType: string.Empty,
                MethodName: $"{methodPrefix}{context.SourceTypeSymbol.Name}",
                ReturnType: context.TargetTypeSymbol.ToTypeMapping()),
            Explicit: false,
            EnumMapping: EnumTypeMappingFactory.Create(context));
    }
}