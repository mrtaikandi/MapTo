using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.Mappings;

internal readonly record struct MethodMapping(
    string ContainingType,
    string MethodName,
    ImmutableArray<string> Parameters,
    bool ReturnsVoid)
{
    private MethodMapping(IMethodSymbol methodSymbol)
        : this(
            ContainingType: methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            MethodName: methodSymbol.Name,
            Parameters: methodSymbol.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).ToImmutableArray(),
            ReturnsVoid: methodSymbol.ReturnsVoid) { }

    public string MethodFullName => $"{ContainingType}.{MethodName}";

    internal static MethodMapping CreateBeforeMapMethod(MappingContext context)
    {
        var mapFromAttribute = context.MapFromAttribute;
        if (mapFromAttribute.GetNamedArgument(nameof(MapFromAttribute.BeforeMap)) is null)
        {
            return default;
        }

        var methodSymbol = GetMethodSymbol(mapFromAttribute, context, nameof(MapFromAttribute.BeforeMap));
        return ValidateBeforeMapMethod(methodSymbol, context) ? new(methodSymbol) : default;
    }

    internal static MethodMapping CreateAfterMapMethod(MappingContext context)
    {
        var mapFromAttribute = context.MapFromAttribute;
        if (mapFromAttribute.GetNamedArgument(nameof(MapFromAttribute.AfterMap)) is null)
        {
            return default;
        }

        var methodSymbol = GetMethodSymbol(mapFromAttribute, context, nameof(MapFromAttribute.AfterMap));
        return ValidateAfterMapMethod(methodSymbol, context) ? new(methodSymbol) : default;
    }

    private static IMethodSymbol? GetMethodSymbol(AttributeData attributeData, MappingContext context, string argumentName)
    {
        var argumentExpression = attributeData.GetNamedArgumentExpression(argumentName);
        if (argumentExpression is null)
        {
            return null;
        }

        return argumentExpression switch
        {
            InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" } } i => context.Compilation.GetMethodSymbol(i),
            LiteralExpressionSyntax { Token.Value: string value } when value.Contains(".") => context.Compilation.GetMethodSymbolByFullyQualifiedName(value.AsSpan()),
            LiteralExpressionSyntax { Token.Value: string value } => context.TargetTypeSymbol.GetMembers(value).OfType<IMethodSymbol>().SingleOrDefault(),
            _ => default
        };
    }

    private static bool ValidateBeforeMapMethod([NotNullWhen(true)] IMethodSymbol? methodSymbol, MappingContext context)
    {
        var sourceTypeSymbol = context.SourceTypeSymbol;
        var compilation = context.Compilation;
        var mapFromAttribute = context.MapFromAttribute;
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

    /// <summary>
    /// A valid after map method can be in the form of:
    /// void AfterMap();
    /// void AfterMap(TTarget target);
    /// void AfterMap(TTarget target, TSource source);
    /// void AfterMap(TSource source, TTarget target);
    /// and it may be void or return TTarget.
    /// </summary>
    private static bool ValidateAfterMapMethod([NotNullWhen(true)] IMethodSymbol? methodSymbol, MappingContext context)
    {
        var targetTypeSymbol = context.TargetTypeSymbol;
        var sourceTypeSymbol = context.SourceTypeSymbol;
        var compilation = context.Compilation;
        var mapFromAttribute = context.MapFromAttribute;
        var compilerOptions = context.CompilerOptions;

        if (methodSymbol is null)
        {
            context.ReportDiagnostic(DiagnosticsFactory.BeforeOrAfterMapMethodNotFoundError(mapFromAttribute, nameof(MapFromAttribute.AfterMap)));
            return false;
        }

        var parameters = methodSymbol.Parameters;
        if (!parameters.IsDefaultOrEmpty)
        {
            if (parameters.Length > 2 ||
                (parameters.Length == 1 && !compilation.HasCompatibleTypes(targetTypeSymbol, parameters[0].Type)) ||
                (parameters.Length == 2 && (!parameters.Any(p => compilation.HasCompatibleTypes(targetTypeSymbol, p.Type)) ||
                                            !parameters.Any(p => compilation.HasCompatibleTypes(sourceTypeSymbol, p.Type)))))
            {
                context.ReportDiagnostic(
                    DiagnosticsFactory.AfterMapMethodInvalidParametersError(mapFromAttribute, nameof(MapFromAttribute.AfterMap), sourceTypeSymbol, targetTypeSymbol));

                return false;
            }

            if (compilerOptions.NullableReferenceTypes && parameters.All(p => p.NullableAnnotation is not NullableAnnotation.Annotated))
            {
                context.ReportDiagnostic(DiagnosticsFactory.BeforeOrAfterMapMethodMissingParameterNullabilityAnnotationError(mapFromAttribute, nameof(MapFromAttribute.AfterMap)));
                return false;
            }
        }

        if (!methodSymbol.ReturnsVoid)
        {
            if (!compilation.HasCompatibleTypes(methodSymbol.ReturnType, targetTypeSymbol))
            {
                context.ReportDiagnostic(DiagnosticsFactory.BeforeOrAfterMapMethodInvalidReturnTypeError(mapFromAttribute, nameof(MapFromAttribute.AfterMap), targetTypeSymbol));
                return false;
            }

            if (methodSymbol.Parameters.IsDefaultOrEmpty)
            {
                context.ReportDiagnostic(DiagnosticsFactory.BeforeOrAfterMapMethodMissingParameterError(mapFromAttribute, nameof(MapFromAttribute.AfterMap), targetTypeSymbol));
                return false;
            }
        }

        return true;
    }
}