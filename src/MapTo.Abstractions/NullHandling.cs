namespace MapTo;

/// <summary>
/// Specifies how to handle null properties.
/// </summary>
public enum NullHandling
{
    /// <summary>
    /// Indicates that the <c>null</c> values should be handled based on the source and target nullability context.
    /// </summary>
    /// <remarks>
    /// When Nullability context is <b>disabled</b>: All <c>null</c> checks are ignored and the target property will be the same as the source property.
    /// This is equivalent to <see cref="SetNull" /> option.
    /// <para>
    /// When Nullability context is <b>enabled</b>:
    /// <list type="bullet">
    ///     <item>If both target and source are nullable, no null check will be performed and the target will be the same as the source.</item>
    ///     <item>
    ///     If the target is nullable but the source is not, no null check will be performed and the target will be the same as the source unless a
    ///     <see cref="PropertyTypeConverterAttribute" />
    ///     is specified on the target property, in which case the converter's return value's nullability context will be used.
    ///     </item>
    ///     <item>If the target is not nullable but the source is, for collections target will be empty collection; for other types reports diagnostic.</item>
    ///     <item>If both target and source are not nullable, no null check will be performed and the target will be the same as the source.</item>
    /// </list>
    /// </para>
    /// </remarks>
    Auto,

    /// <summary>
    /// Indicates that if the source is <c>null</c>, the target will be <c>null</c> too, regardless of its nullability context.
    /// </summary>
    SetNull,

    /// <summary>
    /// Indicates that if the source collection is <c>null</c>, the target collection will be empty, regardless of its nullability context.
    /// This option is only valid for collections.
    /// </summary>
    SetEmptyCollection,

    /// <summary>
    /// Indicates to throw an <see cref="ArgumentNullException" /> if the source is nullable but the target is not.
    /// </summary>
    ThrowException
}