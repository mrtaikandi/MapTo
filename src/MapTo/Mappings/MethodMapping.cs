using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.Mappings;

internal readonly record struct MethodMapping(
    string ContainingType,
    string MethodName,
    TypeMapping ReturnType,
    ImmutableArray<ParameterMapping> Parameters = default,
    ImmutableArray<string> Body = default)
{
    public MethodMapping(IMethodSymbol methodSymbol)
        : this(
            ContainingType: methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            MethodName: methodSymbol.Name,
            ReturnType: methodSymbol.ReturnType.ToTypeMapping(),
            Parameters: methodSymbol.Parameters.ToParameterMappings()) { }

    public string MethodFullName => $"{ContainingType}.{MethodName}";

    public bool ReturnsVoid => ReturnType.SpecialType is SpecialType.System_Void;

    public bool Equals(MethodMapping other) =>
        ContainingType == other.ContainingType &&
        MethodName == other.MethodName &&
        (Parameters.IsDefaultOrEmpty == other.Parameters.IsDefaultOrEmpty || Parameters.SequenceEqual(other.Parameters)) &&
        ReturnType.Equals(other.ReturnType) &&
        (Body.IsDefaultOrEmpty == other.Body.IsDefaultOrEmpty || Body.SequenceEqual(other.Body));

    public override int GetHashCode() => HashCode.Combine(ContainingType, MethodName, Parameters, ReturnType, Body);

    internal static MethodMapping CreateBeforeMapMethod(MappingContext context)
    {
        var methodExpressionSyntax = context.Configuration.BeforeMap;
        if (methodExpressionSyntax is null)
        {
            return default;
        }

        var methodSymbol = GetMethodSymbol(methodExpressionSyntax, context.Compilation, context.TargetTypeSymbol);
        return ValidateBeforeMapMethod(methodSymbol, context) ? new MethodMapping(methodSymbol) : default;
    }

    internal static MethodMapping CreateAfterMapMethod(MappingContext context)
    {
        var methodExpressionSyntax = context.Configuration.AfterMap;
        if (methodExpressionSyntax is null)
        {
            return default;
        }

        var methodSymbol = GetMethodSymbol(methodExpressionSyntax, context.Compilation, context.TargetTypeSymbol);
        return ValidateAfterMapMethod(methodSymbol, context) ? new MethodMapping(methodSymbol) : default;
    }

    private static IMethodSymbol? GetMethodSymbol(ExpressionSyntax? expressionSyntax, Compilation compilation, INamedTypeSymbol targetTypeSymbol)
    {
        if (expressionSyntax is null)
        {
            return null;
        }

        return expressionSyntax switch
        {
            InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" } } i => compilation.GetMethodSymbol(i),
            LiteralExpressionSyntax { Token.Value: string value } when value.Contains(".") => compilation.GetMethodSymbolByFullyQualifiedName(value.AsSpan()),
            LiteralExpressionSyntax { Token.Value: string value } => targetTypeSymbol.GetMembers(value).OfType<IMethodSymbol>().SingleOrDefault(),
            _ => null
        };
    }

    private static bool ValidateBeforeMapMethod([NotNullWhen(true)] IMethodSymbol? methodSymbol, MappingContext context)
    {
        var sourceTypeSymbol = context.SourceTypeSymbol;
        var compilation = context.Compilation;
        var mapFromAttribute = context.Configuration;
        var compilerOptions = context.CompilerOptions;

        if (methodSymbol is null)
        {
            context.ReportDiagnostic(DiagnosticsFactory.BeforeMapMethodNotFoundError(mapFromAttribute));
            return false;
        }

        var methodParameter = methodSymbol.Parameters.FirstOrDefault();
        if (methodParameter is not null)
        {
            if (methodSymbol.Parameters.Length > 1 || !compilation.HasCompatibleTypes(sourceTypeSymbol, methodParameter.Type))
            {
                context.ReportDiagnostic(DiagnosticsFactory.BeforeMapMethodInvalidParameterError(mapFromAttribute, sourceTypeSymbol));
                return false;
            }

            if (compilerOptions.NullableReferenceTypes && methodParameter.NullableAnnotation is not NullableAnnotation.Annotated)
            {
                context.ReportDiagnostic(DiagnosticsFactory.BeforeMapMethodMissingParameterNullabilityAnnotationError(mapFromAttribute));
                return false;
            }
        }

        if (!methodSymbol.ReturnsVoid)
        {
            if (!compilation.HasCompatibleTypes(methodSymbol.ReturnType, sourceTypeSymbol))
            {
                context.ReportDiagnostic(DiagnosticsFactory.BeforeMapMethodInvalidReturnTypeError(mapFromAttribute, sourceTypeSymbol));
                return false;
            }

            if (methodSymbol.Parameters.IsDefaultOrEmpty)
            {
                context.ReportDiagnostic(DiagnosticsFactory.BeforeMapMethodMissingParameterError(mapFromAttribute, sourceTypeSymbol));
                return false;
            }

            if (compilerOptions.NullableReferenceTypes && methodSymbol.ReturnType.NullableAnnotation is not NullableAnnotation.Annotated)
            {
                context.ReportDiagnostic(
                    DiagnosticsFactory.BeforeMapMethodMissingReturnTypeNullabilityAnnotationError(mapFromAttribute));

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
        var mapFromAttribute = context.Configuration;
        var compilerOptions = context.CompilerOptions;

        if (methodSymbol is null)
        {
            context.ReportDiagnostic(DiagnosticsFactory.AfterMapMethodNotFoundError(mapFromAttribute));
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
                    DiagnosticsFactory.AfterMapMethodInvalidParametersError(mapFromAttribute, sourceTypeSymbol, targetTypeSymbol));

                return false;
            }

            if (compilerOptions.NullableReferenceTypes && parameters.All(p => p.NullableAnnotation is not NullableAnnotation.Annotated))
            {
                context.ReportDiagnostic(DiagnosticsFactory.AfterMapMethodMissingParameterNullabilityAnnotationError(mapFromAttribute));
                return false;
            }
        }

        if (!methodSymbol.ReturnsVoid)
        {
            if (!compilation.HasCompatibleTypes(methodSymbol.ReturnType, targetTypeSymbol))
            {
                context.ReportDiagnostic(DiagnosticsFactory.AfterMapMethodInvalidReturnTypeError(mapFromAttribute, targetTypeSymbol));
                return false;
            }

            if (methodSymbol.Parameters.IsDefaultOrEmpty)
            {
                context.ReportDiagnostic(DiagnosticsFactory.AfterMapMethodMissingParameterError(mapFromAttribute, targetTypeSymbol));
                return false;
            }
        }

        return true;
    }
}