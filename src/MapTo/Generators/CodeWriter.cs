using System.CodeDom.Compiler;

namespace MapTo.Generators;

internal sealed class CodeWriter : IDisposable
{
    private readonly IndentedTextWriter _indentedWriter;
    private readonly StringWriter _writer;

    public CodeWriter()
    {
        _writer = new StringWriter();
        _indentedWriter = new IndentedTextWriter(_writer, new string(' ', 4));
    }

    public int CurrentIndent => _indentedWriter.Indent;

    public string NewLine => _indentedWriter.NewLine;

    /// <inheritdoc />
    public void Dispose()
    {
        _writer.Dispose();
        _indentedWriter.Dispose();
    }

    public CodeWriter Indent()
    {
        _indentedWriter.Indent++;
        return this;
    }

    /// <inheritdoc />
    public override string ToString() => _writer.ToString();

    public CodeWriter Unindent()
    {
        _indentedWriter.Indent--;
        return this;
    }

    public CodeWriter Write(string? value = null)
    {
        _indentedWriter.Write(value);
        return this;
    }

    public CodeWriter WriteClosingBracket() => WriteClosingBracket(true);

    public CodeWriter WriteClosingBracket(bool emitNewLine)
    {
        _indentedWriter.Indent--;

        if (emitNewLine)
        {
            _indentedWriter.WriteLine("}");
        }
        else
        {
            _indentedWriter.Write("}");
        }

        return this;
    }

    public CodeWriter WriteClosingBracketIf(bool condition)
    {
        if (condition)
        {
            WriteClosingBracket();
        }

        return this;
    }

    public CodeWriter WriteClosingParenthesis() => WriteLine(")");

    public CodeWriter WriteIf(bool condition, string? value = null, string? elseValue = null)
    {
        if (condition)
        {
            Write(value);
        }
        else if (elseValue != null)
        {
            Write(elseValue);
        }

        return this;
    }

    public CodeWriter WriteJoin(string separator, IEnumerable<string?> values)
    {
        _indentedWriter.Write(string.Join(separator, values));
        return this;
    }

    public CodeWriter WriteNewLine()
    {
        _indentedWriter.WriteLine();
        return this;
    }

    public CodeWriter WriteLine(string? value = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _indentedWriter.WriteLineNoTabs(string.Empty);
        }
        else
        {
            _indentedWriter.WriteLine(value);
        }

        return this;
    }

    public CodeWriter WriteLineIf(bool condition, string? value = null, string? elseValue = null)
    {
        if (condition)
        {
            WriteLine(value);
        }
        else if (elseValue != null)
        {
            WriteLine(elseValue);
        }

        return this;
    }

    public CodeWriter WriteLineJoin(string separator, IEnumerable<string?> values)
    {
        using var enumerator = values.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return WriteLine();
        }

        var firstValue = enumerator.Current;
        if (!enumerator.MoveNext())
        {
            // Only one value is available; no need to join.
            return WriteLine(firstValue);
        }

        _indentedWriter.Write(firstValue);

        do
        {
            WriteLine(separator);
            _indentedWriter.Write(enumerator.Current);
        }
        while (enumerator.MoveNext());

        _indentedWriter.WriteLine();

        return this;
    }

    public CodeWriter WriteLines(IEnumerable<string?> values)
    {
        foreach (var value in values)
        {
            WriteLine(value);
        }

        return this;
    }

    public CodeWriter WriteLinesIf(bool condition, IEnumerable<string?> values)
    {
        if (condition)
        {
            WriteLines(values);
        }

        return this;
    }

    public CodeWriter WriteOpeningBracket()
    {
        _indentedWriter.WriteLine("{");
        _indentedWriter.Indent++;

        return this;
    }

    public CodeWriter WriteOpeningBracketIf(bool condition)
    {
        if (condition)
        {
            WriteOpeningBracket();
        }

        return this;
    }

    public CodeWriter WriteOpenParenthesis() => Write("(");

    public CodeWriter WriteWhitespace() =>
        Write(" ");
}