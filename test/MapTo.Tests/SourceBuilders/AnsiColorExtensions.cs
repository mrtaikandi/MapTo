using System.Text;
using DiffPlex.DiffBuilder.Model;
using Microsoft.CodeAnalysis.Text;

namespace MapTo.Tests.SourceBuilders;

internal static class AnsiColorExtensions
{
    private const string Background = "48";

    private const string BeginForegroundColor = $"\u001b[{Foreground};2;";

    private const string BoldFormat = "\u001b[1m";
    private const string ErrorConsoleColor = $"{BeginForegroundColor}189;38;38m";
    private const string Foreground = "38";
    private const string ItalicFormat = "\u001b[3m";
    private const int Padding = 0;

    private const string ResetFormat = "\u001b[0m";
    private const string StrikethroughFormat = "\u001b[9m";
    private const string SuccessConsoleColor = $"{BeginForegroundColor}16;147;8m";
    private const string UnderlineFormat = "\u001b[4m";
    private const string WarningConsoleColor = $"{BeginForegroundColor}220;94;38m";

    internal static StringBuilder Append(this StringBuilder builder, string value, AnsiColor color) => builder
        .Append(value)
        .ApplyFormat(builder.Length - value.Length, builder.Length, color);

    internal static StringBuilder AppendError(this StringBuilder builder, string value) =>
        builder.Append(value, AnsiColor.Error);

    internal static StringBuilder AppendErrorIf(this StringBuilder builder, bool condition, string value) => condition
        ? builder.AppendError(value)
        : builder.Append(value);

    internal static StringBuilder AppendErrorLine(this StringBuilder builder, string value, FileLinePositionSpan position) =>
        builder.AppendErrorLine(value, position.StartLinePosition.Character, position.EndLinePosition.Character);

    internal static StringBuilder AppendErrorLine(this StringBuilder builder, string value, LinePositionSpan position) =>
        builder.AppendErrorLine(value, position.Start.Character, position.End.Character);

    internal static StringBuilder AppendErrorLine(this StringBuilder builder, string value, int startPosition, int endPosition) => builder
        .Append(value)
        .ApplyFormat(
            builder.Length - value.Length + startPosition,
            builder.Length - value.Length + endPosition,
            AnsiColor.Error)
        .AppendLine();

    internal static StringBuilder AppendIf(this StringBuilder builder, bool condition, string value, AnsiColor color) => condition
        ? builder.Append(value, color)
        : builder.Append(value);

    internal static StringBuilder AppendLine(this StringBuilder builder, string value, AnsiColor color) => builder
        .AppendLine(value)
        .ApplyFormat(builder.Length - value.Length, builder.Length, color);

    internal static StringBuilder AppendLine(this StringBuilder builder, DiffPiece diff) => diff.Type switch
    {
        ChangeType.Deleted => builder.Append("A ", AnsiColor.Error).AppendLine(diff.Text),
        ChangeType.Inserted => builder.Append("E ", AnsiColor.Success).AppendLine(diff.Text),
        _ => builder.Append("  ").AppendLine(diff.Text)
    };

    internal static StringBuilder AppendLines(this StringBuilder builder, IEnumerable<string> values)
    {
        foreach (var value in values)
        {
            builder.AppendLine(value);
        }

        return builder;
    }

    private static StringBuilder ApplyFormat(
        this StringBuilder builder,
        int start,
        int end,
        AnsiColor color = AnsiColor.Default,
        AnsiFormat format = AnsiFormat.Regular)
    {
        if (color is AnsiColor.Default && format is AnsiFormat.Regular)
        {
            return builder;
        }

        if (color is not AnsiColor.Default)
        {
            var colorValue = color.ToColorValue();
            builder.Insert(start, colorValue);
            start += colorValue.Length;
            end += colorValue.Length;
        }

        if (format is not AnsiFormat.Regular)
        {
            var formatValue = format.ToFormatValue();
            builder.Insert(start, formatValue);
            end += formatValue.Length;
        }

        builder.Insert(end, ResetFormat);

        return builder;
    }

    private static StringBuilder MakeBold(this StringBuilder builder, int start, int end, bool reset = false)
    {
        builder.Insert(start, BoldFormat);

        if (reset)
        {
            builder.Insert(end + BoldFormat.Length, ResetFormat);
        }

        return builder;
    }

    private static string ToColorValue(this AnsiColor color) => color switch
    {
        AnsiColor.Default => string.Empty,
        AnsiColor.Success => SuccessConsoleColor,
        AnsiColor.Warning => WarningConsoleColor,
        AnsiColor.Error => ErrorConsoleColor,
        _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
    };

    private static string ToFormatValue(this AnsiFormat format) => format switch
    {
        AnsiFormat.Bold => BoldFormat,
        AnsiFormat.Italic => ItalicFormat,
        _ => string.Empty
    };
}