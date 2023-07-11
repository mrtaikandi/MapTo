namespace MapTo.Configuration;

/// <summary>
/// Represents the access modifier for a generated constructor or method.
/// </summary>
internal enum AccessModifier
{
    /// <summary>
    /// The generated constructor or method is public.
    /// </summary>
    Public,

    /// <summary>
    /// The generated constructor or method is internal.
    /// </summary>
    Internal,

    /// <summary>
    /// The generated constructor or method is private.
    /// </summary>
    Private
}