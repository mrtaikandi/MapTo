namespace MapTo;

/// <summary>
/// Specifies the source type to map from.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class MapAttribute<TFrom, TTo> : MapFromAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapAttribute{TFrom, TTo}"/> class.
    /// </summary>
    public MapAttribute()
        : base(typeof(TTo)) { }
}