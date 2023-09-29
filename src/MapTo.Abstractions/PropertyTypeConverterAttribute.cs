namespace MapTo;

/// <summary>
/// Specifies what type to use as a converter for the property this attribute is bound to.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class PropertyTypeConverterAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyTypeConverterAttribute" /> class.
    /// </summary>
    /// <param name="methodName">The name of the static method to be used to convert the source type.</param>
    /// <exception cref="ArgumentNullException"><paramref name="methodName" /> is <c>null</c>.</exception>
    public PropertyTypeConverterAttribute(string methodName)
    {
        // ReSharper disable once JoinNullCheckWithUsage
        if (methodName is null)
        {
            throw new ArgumentNullException(nameof(methodName));
        }

        MethodName = methodName;
    }

    /// <summary>
    /// Gets the name of the method to be used to convert the source type.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Gets or sets the list of parameters to pass to the <see cref="MethodName" /> during the type conversion.
    /// </summary>
    public object[]? Parameters { get; set; }
}