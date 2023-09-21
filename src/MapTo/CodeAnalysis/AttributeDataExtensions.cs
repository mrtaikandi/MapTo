namespace MapTo.CodeAnalysis;

internal static class AttributeDataExtensions
{
    internal static bool TryGetNamedArgument(this AttributeData attributeData, string name, [NotNullWhen(true)] out object? value)
    {
        value = attributeData.NamedArguments.SingleOrDefault(a => a.Key == name).Value.Value;
        return value is not null;
    }

    internal static object? GetNamedArgument(this AttributeData attributeData, string name) =>
        attributeData.NamedArguments.SingleOrDefault(a => a.Key == name).Value.Value;
}