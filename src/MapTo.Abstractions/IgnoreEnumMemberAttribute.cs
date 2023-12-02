namespace MapTo;

/// <summary>
/// Specifies which enum members to ignore.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
public class IgnoreEnumMemberAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IgnoreEnumMemberAttribute"/> class.
    /// </summary>
    /// <param name="enumMember">The enum member to ignore.</param>
    public IgnoreEnumMemberAttribute(object? enumMember = null)
    {
        if (enumMember is not null)
        {
            EnumMember = (Enum)enumMember;
        }
    }

    /// <summary>
    /// Gets the enum member to ignore.
    /// </summary>
    public Enum? EnumMember { get; }
}