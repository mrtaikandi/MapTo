namespace MapTo;

/// <summary>
/// Specifies the source type to map from.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class MapAttribute<TFrom, TTo> : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapAttribute{TFrom, TTo}"/> class.
    /// </summary>
    public MapAttribute() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapAttribute{TFrom, TTo}"/> class.
    /// </summary>
    /// <param name="configuration">The name of the configuration method to use when mapping the source to the target.</param>
    public MapAttribute(string configuration)
        : this() { }
}