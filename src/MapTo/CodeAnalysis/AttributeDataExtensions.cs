namespace MapTo.CodeAnalysis;

internal static class AttributeDataExtensions
{
    internal static bool TryGetNamedArgument(this AttributeData attributeData, string name, [NotNullWhen(true)] out object? value)
    {
        value = attributeData.NamedArguments.SingleOrDefault(a => a.Key == name).Value.Value;
        return value is not null;
    }

    internal static object? GetNamedArgument(this AttributeData? attributeData, string name) =>
        attributeData?.NamedArguments.SingleOrDefault(a => a.Key == name).Value.Value;

    internal static T GetNamedArgument<T>(this AttributeData? attributeData, string name, T defaultValue = default!)
    {
        var value = attributeData?.NamedArguments.SingleOrDefault(a => a.Key == name).Value.Value;
        return value is null ? defaultValue : (T)value;
    }
}