namespace MapTo;

/// <summary>
/// Specifies that a property should be mapped to a source property.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class MapPropertyAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the source property to map to.
    /// </summary>
    /// <value>The name of the source property to map to.</value>
    public string? From { get; set; }

    /// <summary>
    /// Gets or sets the way to handle null properties.
    /// </summary>
    public NullHandling NullHandling { get; set; } = NullHandling.Auto;
}