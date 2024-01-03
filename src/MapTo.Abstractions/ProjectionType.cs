using System.Diagnostics.CodeAnalysis;

namespace MapTo;

/// <summary>
/// Specifies the type of projection mappings to generate.
/// </summary>
[Flags]
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Naming convention matches the one used by the compiler.")]
public enum ProjectionType
{
    /// <summary>
    /// Specifies that no projection mappings should be generated.
    /// </summary>
    None = 0,

    /// <summary>
    /// Specifies that projection mappings should be generated for arrays.
    /// </summary>
    Array = 1 << 0,

    /// <summary>
    /// Specifies that projection mappings should be generated for <see cref="IEnumerable{T}" />.
    /// </summary>
    IEnumerable = 1 << 1,

    /// <summary>
    /// Specifies that projection mappings should be generated for <see cref="ICollection{T}" />.
    /// </summary>
    ICollection = 1 << 2,

    /// <summary>
    /// Specifies that projection mappings should be generated for <see cref="IReadOnlyCollection{T}" />.
    /// </summary>
    IReadOnlyCollection = 1 << 3,

    /// <summary>
    /// Specifies that projection mappings should be generated for <see cref="IList{T}" />.
    /// </summary>
    IList = 1 << 4,

    /// <summary>
    /// Specifies that projection mappings should be generated for <see cref="IReadOnlyList{T}" />.
    /// </summary>
    IReadOnlyList = 1 << 5,

    /// <summary>
    /// Specifies that projection mappings should be generated for <see cref="System.Collections.Generic.List{T}" />.
    /// </summary>
    List = 1 << 6,

    /// <summary>
    /// Specifies that projection mappings should be generated for <c>Memory{T}</c> />.
    /// </summary>
    Memory = 1 << 7,

    /// <summary>
    /// Specifies that projection mappings should be generated for <c>ReadOnlyMemory{T}</c> />.
    /// </summary>
    ReadOnlyMemory = 1 << 8,

    /// <summary>
    /// Specifies that projection mappings should be generated for <see cref="IQueryable{T}" />.
    /// </summary>
    Queryable = 1 << 9,

    /// <summary>
    /// Specifies that projection mappings should be generated for all supported types.
    /// </summary>
    All = ~None
}