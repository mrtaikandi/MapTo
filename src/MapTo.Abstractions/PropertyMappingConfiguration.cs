using System.Linq.Expressions;

namespace MapTo;

/// <summary>
/// The mapping configuration for a property.
/// </summary>
/// <typeparam name="TSource">The source type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public sealed class PropertyMappingConfiguration<TSource, TProperty>
{
    /// <summary>
    /// Configures the mapping to ignore the property.
    /// </summary>
    public void Ignore() { }

    /// <summary>
    /// Configures the mapping to use the specified type converter when mapping the property.
    /// </summary>
    /// <param name="converter">The type converter to use.</param>
    /// <typeparam name="T">The type of the converter.</typeparam>
    /// <returns>The current instance of the <see cref="PropertyMappingConfiguration{TSource, TProperty}"/>.</returns>
    public PropertyMappingConfiguration<TSource, TProperty> UseTypeConverter<T>(Func<T, TProperty> converter) => this;

    /// <summary>
    /// Configures the mapping to use the specified type converter when mapping the property.
    /// </summary>
    /// <param name="converter">The type converter to use.</param>
    /// <param name="parameters">The parameters to pass to the converter.</param>
    /// <typeparam name="T">The type of the converter.</typeparam>
    /// <returns>The current instance of the <see cref="PropertyMappingConfiguration{TSource, TProperty}"/>.</returns>
    public PropertyMappingConfiguration<TSource, TProperty> UseTypeConverter<T>(Func<T, object[]?, TProperty> converter, object[]? parameters) => this;

    /// <summary>
    /// Configures the mapping to map the property to the specified <typeparamref name="TSource"/>'s property.
    /// </summary>
    /// <typeparam name="T">The type of the property to map to.</typeparam>
    /// <param name="property">The property to map to.</param>
    /// <param name="nullHandling">The way to handle null properties.</param>
    /// <returns>The current instance of the <see cref="PropertyMappingConfiguration{TSource, TProperty}"/>.</returns>
    public PropertyMappingConfiguration<TSource, TProperty> MapTo<T>(Expression<Func<TSource, T>> property, NullHandling nullHandling = NullHandling.Auto) => this;
}