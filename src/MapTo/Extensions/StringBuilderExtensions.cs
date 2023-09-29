﻿using System.Text;

namespace MapTo.Extensions;

internal static class StringBuilderExtensions
{
    public static StringBuilder PadLeft(this StringBuilder builder, int width)
    {
        for (var i = 0; i < width; i++)
        {
            builder.Append(" ");
        }

        return builder;
    }

    internal static StringBuilder AppendClosingBracket(this StringBuilder builder, int indent = 0, bool padNewLine = true)
    {
        if (padNewLine)
        {
            builder.AppendLine();
        }

        return builder.PadLeft(indent).Append("}");
    }

    internal static StringBuilder AppendOpeningBracket(this StringBuilder builder, int indent = 0) => builder.AppendLine().PadLeft(indent).Append("{{\r\n");
}