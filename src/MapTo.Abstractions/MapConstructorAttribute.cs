namespace MapTo;

/// <summary>
/// Specifies that annotated constructor should be used for mapping objects.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor)]
public sealed class MapConstructorAttribute : Attribute { }