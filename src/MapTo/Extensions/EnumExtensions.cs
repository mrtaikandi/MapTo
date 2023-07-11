namespace MapTo.Extensions;

internal static class EnumExtensions
{
    internal static string ToLowercaseString(this Enum member) => member.ToString().ToLower();
}