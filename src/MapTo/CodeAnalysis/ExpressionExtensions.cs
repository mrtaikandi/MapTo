using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.CodeAnalysis;

internal static class ExpressionExtensions
{
    internal static IMethodSymbol? GetMethodSymbol(this ExpressionSyntax? expression, Compilation compilation, INamedTypeSymbol? typeSymbol = null) => expression switch
    {
        InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" } } i => compilation.GetMethodSymbol(i),
        LiteralExpressionSyntax { Token.Value: string value } when value.Contains(".") => compilation.GetMethodSymbolByFullyQualifiedName(value.AsSpan()),
        LiteralExpressionSyntax { Token.Value: string value } => typeSymbol?.GetMembers(value).OfType<IMethodSymbol>().SingleOrDefault(),
        _ => null
    };

    internal static TEnum? GetEnumValue<TEnum>(this ExpressionSyntax expression, SemanticModel semanticModel)
        where TEnum : struct
    {
        var constantValue = semanticModel.GetConstantValue(expression);
        return constantValue is { HasValue: true, Value: not null } && Enum.TryParse<TEnum>(constantValue.Value.ToString(), out var result) ? result : null;
    }

    internal static TEnum? GetEnumValue<TEnum>(this ExpressionSyntax expression)
        where TEnum : struct
    {
        if (expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return null;
        }

        var enumName = memberAccess.Name.Identifier.ValueText;
        if (Enum.TryParse<TEnum>(enumName, out var result))
        {
            return result;
        }

        return null;
    }
}