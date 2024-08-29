using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.CodeAnalysis;

internal static class AttributeDataExtensions
{
    internal static object? GetNamedArgument(this AttributeData? attributeData, string name) =>
        attributeData?.NamedArguments.SingleOrDefault(a => a.Key == name).Value.Value;

    internal static T GetNamedArgument<T>(this AttributeData? attributeData, string name, T defaultValue = default!)
        where T : struct
    {
        var value = attributeData?.NamedArguments.SingleOrDefault(a => a.Key == name).Value.Value;
        return value is null ? defaultValue : (T)value;
    }

    internal static T? GetNamedArgumentOrNull<T>(this AttributeData? attributeData, string name)
        where T : struct
    {
        var value = attributeData?.NamedArguments.SingleOrDefault(a => a.Key == name).Value.Value;
        return value is null ? null : (T)value;
    }

    internal static string? GetNamedArgumentStringValue(this AttributeData? attributeData, string name) => attributeData is null
        ? null
        : attributeData.GetAttributeSyntax().GetNamedArgumentValue(name) ?? attributeData.NamedArguments.SingleOrDefault(a => a.Key == name).Value.Value as string;

    internal static AttributeSyntax? GetAttributeSyntax(this AttributeData? attributeData) =>
        attributeData?.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;

    internal static ExpressionSyntax[] GetArgumentsExpressions(this AttributeData? attributeData) =>
        attributeData?.GetAttributeSyntax()?.ArgumentList?.Arguments.Select(a => a.Expression).ToArray() ?? [];

    internal static ExpressionSyntax? GetNamedArgumentExpression(this AttributeData? attributeData, string name) =>
        attributeData?.GetAttributeSyntax().GetNamedArgumentExpression(name);

    internal static ExpressionSyntax? GetNamedArgumentExpression(this AttributeSyntax? attributeSyntax, string name) =>
        attributeSyntax?.ArgumentList?.Arguments.SingleOrDefault(a => a.NameEquals?.Name.Identifier.Text == name)?.Expression;

    internal static string? GetNamedArgumentValue(this AttributeSyntax? attributeSyntax, string name) => attributeSyntax.GetNamedArgumentExpression(name) switch
    {
        LiteralExpressionSyntax { Token.Value: string value } => value,
        InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" }, ArgumentList.Arguments: { Count: 1 } arguments } =>
            arguments[0].Expression.ToString(),
        _ => null
    };

    internal static Location? GetLocation(this AttributeData? symbol) =>
        symbol?.GetAttributeSyntax()?.GetLocation();

    internal static Location GetNamedArgumentLocation(this AttributeData attribute, string name) => attribute.ApplicationSyntaxReference?.GetSyntax()
        .DescendantNodes()
        .OfType<AttributeArgumentSyntax>()
        .FirstOrDefault(a => a.NameEquals?.Name.Identifier.ValueText == name)?.GetLocation() ?? Location.None;
}