namespace MapTo;

/// <summary>
/// Specifies the strategy to use when mapping enum values.
/// </summary>
public enum EnumMappingStrategy
{
    /// <summary>
    /// Use the enum's underlying value to map to the target enum. This is the default.
    /// </summary>
    ByValue,

    /// <summary>
    /// Use the enum's name to map to the target enum.
    /// </summary>
    ByName,

    /// <summary>
    /// Use the enum's name to map to the target enum, ignoring case.
    /// </summary>
    ByNameCaseInsensitive
}