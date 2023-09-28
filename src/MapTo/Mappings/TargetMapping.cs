using MapTo.Configuration;
using MapTo.Extensions;

namespace MapTo.Mappings;

internal readonly record struct TargetMapping(
    AccessModifier Modifier,
    string Name,
    NamespaceMapping Namespace,
    bool IsPartial,
    SourceMapping Source,
    ConstructorMapping Constructor,
    ImmutableArray<PropertyMapping> Properties,
    Location Location,
    ImmutableArray<string> UsingDirectives,
    MethodMapping BeforeMapMethod,
    MethodMapping AfterMapMethod,
    CodeGeneratorOptions Options);

internal static class TargetMappingFactory
{
    public static TargetMapping Create(MappingContext context)
    {
        var (targetTypeSyntax, targetTypeSymbol, _, sourceTypeSymbol, _, codeGeneratorOptions, _) = context;

        var properties = PropertyMappingFactory.Create(context);
        var constructorMapping = ConstructorMappingFactory.Create(context, properties);
        properties = properties.ExceptConstructorInitializers(constructorMapping);

        var mapping = new TargetMapping(
            Modifier: targetTypeSyntax.GetAccessModifier(),
            Name: targetTypeSyntax.Identifier.Text,
            Namespace: NamespaceMapping.Create(targetTypeSymbol),
            IsPartial: targetTypeSyntax.IsPartial(),
            Source: SourceMapping.Create(sourceTypeSymbol),
            Constructor: constructorMapping,
            Properties: properties,
            Location: targetTypeSyntax.Identifier.GetLocation(),
            UsingDirectives: properties.SelectMany(p => p.UsingDirectives).Distinct().ToImmutableArray(),
            BeforeMapMethod: targetTypeSymbol.GetBeforeMapMethod(context),
            AfterMapMethod: targetTypeSymbol.GetAfterMapMethod(context),
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

    private static string ToFullyQualifiedName(this TargetMapping mapping) =>
        mapping.Namespace.IsGlobalNamespace ? mapping.Name : $"global::{mapping.Namespace}.{mapping.Name}";

    private static bool UseFullyQualifiedName(this TargetMapping mapping) =>
        mapping.Namespace != mapping.Source.Namespace;

    private static ReferenceHandling UseReferenceHandling(this MappingContext context, ImmutableArray<PropertyMapping> properties) =>
        context.CodeGeneratorOptions.ReferenceHandling == ReferenceHandling.Auto &&
        (properties.Any(p => SymbolEqualityComparer.Default.Equals(p.Type, context.TargetTypeSymbol)) || context.TargetTypeSymbol.HasNonPrimitiveProperties())
            ? ReferenceHandling.Enabled
            : context.CodeGeneratorOptions.ReferenceHandling;

    private static bool IsValid(this TargetMapping mapping, MappingContext context)
    {
        if (mapping is { IsPartial: false, Constructor.IsGenerated: true })
        {
            context.ReportDiagnostic(DiagnosticsFactory.MissingPartialKeywordOnTargetClassError(mapping.Location, mapping.Name));
            return false;
        }

        return true;
    }

    private static MethodMapping GetBeforeMapMethod(this ITypeSymbol targetTypeSymbol, MappingContext context)
    {
        var compilation = context.Compilation;
        var mapFromAttribute = context.MapFromAttributeData;

        var methodName = mapFromAttribute.GetNamedArgument<string?>(nameof(MapFromAttribute.BeforeMap));
        if (methodName is null)
        {
            return default;
        }

        var methodSymbol = methodName.Contains(".")
            ? compilation.GetMethodSymbolByFullyQualifiedName(methodName.AsSpan())
            : targetTypeSymbol.GetMembers(methodName).OfType<IMethodSymbol>().SingleOrDefault();

        return !methodSymbol.ValidateBeforeMapMethod(context)
            ? default
            : new MethodMapping(
                ContainingType: methodSymbol.ContainingType.ToDisplayString(),
                MethodName: methodSymbol.Name,
                Parameter: methodSymbol.Parameters.Select(p => p.Name.ToSourceCodeString()).ToImmutableArray(),
                ReturnsVoid: methodSymbol.ReturnsVoid);
    }

    private static MethodMapping GetAfterMapMethod(this ITypeSymbol targetTypeSymbol, MappingContext context)
    {
        var compilation = context.Compilation;
        var mapFromAttribute = context.MapFromAttributeData;

        var methodName = mapFromAttribute.GetNamedArgument<string?>(nameof(MapFromAttribute.AfterMap));
        if (methodName is null)
        {
            return default;
        }

        var methodSymbol = methodName.Contains(".")
            ? compilation.GetMethodSymbolByFullyQualifiedName(methodName.AsSpan())
            : targetTypeSymbol.GetMembers(methodName).OfType<IMethodSymbol>().SingleOrDefault();

        return !methodSymbol.ValidateAfterMapMethod(context)
            ? default
            : new MethodMapping(
                ContainingType: methodSymbol.ContainingType.ToDisplayString(),
                MethodName: methodSymbol.Name,
                Parameter: methodSymbol.Parameters.Select(p => p.Name.ToSourceCodeString()).ToImmutableArray(),
                ReturnsVoid: methodSymbol.ReturnsVoid);
    }

    private static bool ValidateBeforeMapMethod([NotNullWhen(true)] this IMethodSymbol? methodSymbol, MappingContext context)
    {
        var sourceTypeSymbol = context.SourceTypeSymbol;
        var compilation = context.Compilation;
        var mapFromAttribute = context.MapFromAttributeData;
        var compilerOptions = context.CompilerOptions;

        if (methodSymbol is null)
        {
            context.ReportDiagnostic(DiagnosticsFactory.BeforeOrAfterMapMethodNotFoundError(mapFromAttribute, nameof(MapFromAttribute.BeforeMap)));
            return false;
        }

        var methodParameter = methodSymbol.Parameters.FirstOrDefault();
        if (methodParameter is not null)
        {
            if (methodSymbol.Parameters.Length > 1 || !compilation.HasCompatibleTypes(sourceTypeSymbol, methodParameter.Type))
            {
                context.ReportDiagnostic(DiagnosticsFactory.BeforeOrAfterMapMethodInvalidParameterError(mapFromAttribute, nameof(MapFromAttribute.BeforeMap), sourceTypeSymbol));
                return false;
            }

            if (compilerOptions.NullableReferenceTypes && methodParameter.NullableAnnotation is not NullableAnnotation.Annotated)
            {
                context.ReportDiagnostic(DiagnosticsFactory.BeforeOrAfterMapMethodMissingParameterNullabilityAnnotationError(mapFromAttribute, nameof(MapFromAttribute.BeforeMap)));
                return false;
            }
        }

        if (!methodSymbol.ReturnsVoid)
        {
            if (!compilation.HasCompatibleTypes(methodSymbol.ReturnType, sourceTypeSymbol))
            {
                context.ReportDiagnostic(DiagnosticsFactory.BeforeOrAfterMapMethodInvalidReturnTypeError(mapFromAttribute, nameof(MapFromAttribute.BeforeMap), sourceTypeSymbol));
                return false;
            }

            if (methodSymbol.Parameters.IsDefaultOrEmpty)
            {
                context.ReportDiagnostic(DiagnosticsFactory.BeforeOrAfterMapMethodMissingParameterError(mapFromAttribute, nameof(MapFromAttribute.BeforeMap), sourceTypeSymbol));
                return false;
            }

            if (compilerOptions.NullableReferenceTypes && methodSymbol.ReturnType.NullableAnnotation is not NullableAnnotation.Annotated)
            {
                context.ReportDiagnostic(
                    DiagnosticsFactory.BeforeOrAfterMapMethodMissingReturnTypeNullabilityAnnotationError(mapFromAttribute, nameof(MapFromAttribute.BeforeMap)));

                return false;
            }
        }

        return true;
    }

    private static bool ValidateAfterMapMethod([NotNullWhen(true)] this IMethodSymbol? methodSymbol, MappingContext context)
    {
        var targetTypeSymbol = context.TargetTypeSymbol;
        var compilation = context.Compilation;
        var mapFromAttribute = context.MapFromAttributeData;
        var compilerOptions = context.CompilerOptions;

        if (methodSymbol is null)
        {
            context.ReportDiagnostic(DiagnosticsFactory.BeforeOrAfterMapMethodNotFoundError(mapFromAttribute, nameof(MapFromAttribute.AfterMap)));
            return false;
        }

        var methodParameter = methodSymbol.Parameters.FirstOrDefault();
        if (methodParameter is not null)
        {
            if (methodSymbol.Parameters.Length > 1 || !compilation.HasCompatibleTypes(targetTypeSymbol, methodParameter.Type))
            {
                context.ReportDiagnostic(DiagnosticsFactory.BeforeOrAfterMapMethodInvalidParameterError(mapFromAttribute, nameof(MapFromAttribute.AfterMap), targetTypeSymbol));
                return false;
            }

            if (compilerOptions.NullableReferenceTypes && methodParameter.NullableAnnotation is not NullableAnnotation.Annotated)
            {
                context.ReportDiagnostic(DiagnosticsFactory.BeforeOrAfterMapMethodMissingParameterNullabilityAnnotationError(mapFromAttribute, nameof(MapFromAttribute.AfterMap)));
                return false;
            }
        }

        return true;
    }
}