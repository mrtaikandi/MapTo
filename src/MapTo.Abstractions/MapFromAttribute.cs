using MapTo.Configuration;

namespace MapTo;

/// <summary>
/// Specifies the source type to map from.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum, Inherited = false)]
public sealed class MapFromAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapFromAttribute" /> class.
    /// </summary>
    /// <param name="sourceType">The source type to map from.</param>
    public MapFromAttribute(Type sourceType)
    {
        // ReSharper disable once JoinNullCheckWithUsage
        if (sourceType is null)
        {
            throw new ArgumentNullException(nameof(sourceType));
        }

        SourceType = sourceType;
        ReferenceHandling = ReferenceHandling.Auto;
    }

    /// <summary>
    /// Gets the source type to map from.
    /// </summary>
    public Type SourceType { get; }

    /// <summary>
    /// Gets or sets a value indicating whether to copy object and primitive type arrays into a new array.
    /// </summary>
    public bool CopyPrimitiveArrays { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use reference handling.
    /// If not set, MapTo will try to automatically determine whether to use reference handling.
    /// Set to <see cref="Configuration.ReferenceHandling.Enabled"/> to force reference handling or
    /// <see cref="Configuration.ReferenceHandling.Disabled"/> to force no reference handling.
    /// </summary>
    public ReferenceHandling ReferenceHandling { get; set; }

    /// <summary>
    /// Gets or sets the name of the method to call before mapping the property.
    /// </summary>
    /// <value>The name of the method to call before mapping the property.</value>
    public string? BeforeMap { get; set; }

    /// <summary>
    /// Gets or sets the name of the method to call after mapping the property.
    /// </summary>
    /// <value>The name of the method to call after mapping the property.</value>
    public string? AfterMap { get; set; }

    /// <summary>
    /// Gets or sets the way to handle null properties.
    /// </summary>
    /// <value>The way to handle null properties.</value>
    public NullHandling NullHandling { get; set; } = NullHandling.Auto;

    /// <summary>
    /// Gets or sets the enum mapping strategy. Defaults to <see cref="EnumMappingStrategy.ByValue" />.
    /// </summary>
    public EnumMappingStrategy EnumMappingStrategy { get; set; } = EnumMappingStrategy.ByValue;

    /// <summary>
    /// Gets or sets the fallback value to use when the enum value is not found in the target enum.
    /// </summary>
    public object? EnumMappingFallbackValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating how strict the enum mapping should be. Defaults to <see cref="StrictEnumMapping.Off" />.
    /// </summary>
    public StrictEnumMapping StrictEnumMapping { get; set; } = StrictEnumMapping.Off;

    /// <summary>
    /// Gets or sets a value indicating whether to generate projection mappings and which types to generate them for.
    /// By default, projection mappings are generated for all supported types.
    /// </summary>
    public ProjectionType ProjectTo { get; set; } = ProjectionType.None;
}