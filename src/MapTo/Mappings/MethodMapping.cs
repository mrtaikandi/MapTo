namespace MapTo.Mappings;

internal readonly record struct MethodMapping(
    string ContainingType,
    string MethodName,
    ImmutableArray<string> Parameter,
    bool ReturnsVoid)
{
    public string MethodFullName => $"{ContainingType}.{MethodName}";
}