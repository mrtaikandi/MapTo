namespace MapTo.Configuration;

/// <summary>
/// Specifies how MapTo should handle references.
/// </summary>
public enum ReferenceHandling
{
    /// <summary>
    /// MapTo will not use reference handling.
    /// </summary>
    Disabled,

    /// <summary>
    /// MapTo will use reference handling.
    /// </summary>
    Enabled,

    /// <summary>
    /// MapTo will try to automatically determine whether to use reference handling.
    /// </summary>
    Auto
}