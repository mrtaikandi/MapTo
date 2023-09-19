namespace MapTo.CodeAnalysis;

internal static class AttributeDataExtensions
{
    internal static bool TryGetNamedArgument(this AttributeData attributeData, string name, out TypedConstant value)
    {
        value = attributeData.NamedArguments.SingleOrDefault(a => a.Key == name).Value;
        return !value.IsNull;
    }
}