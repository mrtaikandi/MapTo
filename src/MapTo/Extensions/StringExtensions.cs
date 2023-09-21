using System.Diagnostics;

namespace MapTo.Extensions;

internal static class StringExtensions
{
    public static string ToParameterNameCasing(this string value) =>
        string.IsNullOrWhiteSpace(value) ? value : $"{char.ToLower(value[0])}{value.Substring(1)}";

    public static string ToSourceCodeString(this object? value) => value switch
    {
        null => "null",
        string strValue => $"\"{strValue}\"",
        char charValue => $"'{charValue}'",
        _ => value.ToString()
    };

    public static string ToSourceCodeString(this TypedConstant constant)
    {
        if (constant.IsNull)
        {
            return "null";
        }

        if (constant.Kind == TypedConstantKind.Array)
        {
            Debug.Assert(constant.Type != null, "constant.Type != null");
            return $"new {constant.Type!.ToDisplayString()} {{ {string.Join(", ", constant.Values.Select(v => v.ToCSharpString()))} }}";
        }

        return constant.ToCSharpString();
    }

    public static string Pluralize(this string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return word;
        }

        if (word.EndsWith("s") || word.EndsWith("x") || word.EndsWith("z") || word.EndsWith("ch") || word.EndsWith("sh"))
        {
            return word + "es";
        }

        if (word.EndsWith("y"))
        {
            return word.Substring(0, word.Length - 1) + "ies";
        }

        return word + "s";
    }
}