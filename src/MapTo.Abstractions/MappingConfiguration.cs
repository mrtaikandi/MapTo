using System.Linq.Expressions;
using MapTo.Configuration;

namespace MapTo;

/// <summary>
/// The mapping configuration of <typeparamref name="TSource"/> and <typeparamref name="TTarget"/>.
/// </summary>
/// <typeparam name="TSource">The source type.</typeparam>
/// <typeparam name="TTarget">The target type.</typeparam>
public sealed class MappingConfiguration<TSource, TTarget>
{
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
    public ReferenceHandling ReferenceHandling { get; set; } = ReferenceHandling.Auto;

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

    /// <summary>
    /// Gets the <typeparamref name="TTarget" /> property mapping configuration.
    /// </summary>
    /// <param name="property">The property to configure.</param>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <returns>The property mapping configuration.</returns>
    public PropertyMappingConfiguration<TSource, TProperty> ForProperty<TProperty>(Expression<Func<TTarget, TProperty>> property) => new();
}