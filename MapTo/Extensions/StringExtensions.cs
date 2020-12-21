namespace MapTo.Extensions
{
    internal static class StringExtensions
    {
        public static string ToCamelCase(this string value) => string.IsNullOrWhiteSpace(value) ? value : $"{char.ToLower(value[0])}{value.Substring(1)}";
    }
}