using MapTo.Configuration;
using MapTo.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.Mappings;

internal readonly record struct TargetMapping(
    Accessibility Modifier,
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
            Modifier: targetTypeSyntax.GetAccessibility(),
            Name: targetTypeSyntax.Identifier.Text,
            Namespace: NamespaceMapping.Create(targetTypeSymbol),
            IsPartial: targetTypeSyntax.IsPartial(),
            Source: SourceMapping.Create(sourceTypeSymbol),
            Constructor: constructorMapping,
            Properties: properties,
            Location: targetTypeSyntax.Identifier.GetLocation(),
            UsingDirectives: properties.SelectMany(p => p.UsingDirectives).Distinct().ToImmutableArray(),
            BeforeMapMethod: GetBeforeMapMethod(context),
            AfterMapMethod: GetAfterMapMethod(context),
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

    private static MethodMapping GetBeforeMapMethod(MappingContext context)
    {
        var mapFromAttribute = context.MapFromAttributeData;
        if (mapFromAttribute.GetNamedArgument(nameof(MapFromAttribute.BeforeMap)) is null)
        {
            return default;
        }

        var methodSymbol = mapFromAttribute.GetMethodSymbol(context, nameof(MapFromAttribute.BeforeMap));
        return methodSymbol.ValidateBeforeMapMethod(context) ? MethodMapping.Create(methodSymbol) : default;
    }

    private static MethodMapping GetAfterMapMethod(MappingContext context)
    {
        var mapFromAttribute = context.MapFromAttributeData;
        if (mapFromAttribute.GetNamedArgument(nameof(MapFromAttribute.AfterMap)) is null)
        {
            return default;
        }

        var methodSymbol = mapFromAttribute.GetMethodSymbol(context, nameof(MapFromAttribute.AfterMap));
        return methodSymbol.ValidateAfterMapMethod(context) ? MethodMapping.Create(methodSymbol) : default;
    }

    private static IMethodSymbol? GetMethodSymbol(this AttributeData attributeData, MappingContext context, string argumentName)
    {
        var argumentExpression = attributeData.GetNamedArgumentExpression(argumentName);
        if (argumentExpression is null)
        {
            return null;
        }

        return argumentExpression switch
        {
            InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" }, ArgumentList.Arguments: { Count: 1 } arguments } =>
                context.TargetSemanticModel.GetSymbolInfo(arguments[0].Expression).GetSymbolOrBestCandidate<IMethodSymbol>(),
            LiteralExpressionSyntax { Token.Value: string value } when value.Contains(".") => context.Compilation.GetMethodSymbolByFullyQualifiedName(value.AsSpan()),
            LiteralExpressionSyntax { Token.Value: string value } => context.TargetTypeSymbol.GetMembers(value).OfType<IMethodSymbol>().SingleOrDefault(),
            _ => default
        };
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