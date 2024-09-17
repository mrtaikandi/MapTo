using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.Mappings;

internal readonly record struct ParameterMapping(string Name, string Type)
{
    public ParameterMapping(IParameterSymbol parameterSymbol)
        : this(parameterSymbol.Name, parameterSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)) { }

    public string ToDisplayString() => $"{Type} {Name}";
}

internal static class ParameterMappingFactory
{
    public static ImmutableArray<ParameterMapping> ToParameterMappings(this ImmutableArray<IParameterSymbol> parameters) =>
        parameters.Select(p => new ParameterMapping(p)).ToImmutableArray();

    public static ImmutableArray<ParameterMapping> ToParameterMappings(this SeparatedSyntaxList<ParameterSyntax> parameterSyntaxes) =>
        parameterSyntaxes.Select(p => new ParameterMapping(p.Identifier.Text, string.Empty)).ToImmutableArray();

    public static ImmutableArray<ParameterMapping> GetParameterMappings(this ParenthesizedLambdaExpressionSyntax expression, SemanticModel semanticModel)
    {
        var typeInfo = semanticModel.GetTypeInfo(expression);
        if (typeInfo.ConvertedType is not INamedTypeSymbol namedTypeSymbol)
        {
            return ImmutableArray<ParameterMapping>.Empty;
        }

        var parameterMappings = ImmutableArray.CreateBuilder<ParameterMapping>();
        for (var i = 0; i < expression.ParameterList.Parameters.Count; i++)
        {
            parameterMappings.Add(new ParameterMapping(
                expression.ParameterList.Parameters[i].Identifier.Text,
                namedTypeSymbol.TypeArguments[i].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }

        return parameterMappings.ToImmutable();
    }

    public static ImmutableArray<ParameterMapping> GetParameterMappings(this SimpleLambdaExpressionSyntax expression, SemanticModel semanticModel)
    {
        var typeInfo = semanticModel.GetTypeInfo(expression);
        if (typeInfo.ConvertedType is not INamedTypeSymbol namedTypeSymbol)
        {
            return ImmutableArray<ParameterMapping>.Empty;
        }

        return ImmutableArray.Create(new ParameterMapping(
            expression.Parameter.Identifier.Text,
            namedTypeSymbol.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
    }
}