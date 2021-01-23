using System.Threading.Tasks;

namespace MapTo.Extensions
{
    internal static class StringExtensions
    {
        public static string ToCamelCase(this string value) => string.IsNullOrWhiteSpace(value) ? value : $"{char.ToLower(value[0])}{value.Substring(1)}";

        public static string ToSourceCodeString(this object? value) => value switch
        {
            null => "null",
            string strValue => $"\"{strValue}\"",
            char charValue => $"'{charValue}'",
            _ => value.ToString()
        };
    }
}