namespace MapTo;

/// <summary>
/// Specifies whether to enforce strict enum mapping.
/// </summary>
public enum StrictEnumMapping
{
    /// <summary>
    /// Do not enforce strict enum mapping. This is the default.
    /// </summary>
    Off,

    /// <summary>
    /// Enforce all source enum members to be mapped to target enum members.
    /// </summary>
    SourceOnly,

    /// <summary>
    /// Enforce all target enum members to be mapped from source enum members.
    /// </summary>
    TargetOnly,

    /// <summary>
    /// Enforce all source enum members to be mapped to target enum members and all target enum members to be mapped from source enum members.
    /// </summary>
    SourceAndTarget
}