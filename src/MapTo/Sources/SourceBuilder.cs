using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MapTo.Sources
{
    internal sealed class SourceBuilder : IDisposable
    {
        private readonly StringWriter _writer;
        private readonly IndentedTextWriter _indentedWriter;

        public SourceBuilder()
        {
            _writer = new StringWriter();
            _indentedWriter = new IndentedTextWriter(_writer, new string(' ', 4));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _writer.Dispose();
            _indentedWriter.Dispose();
        }

        public SourceBuilder WriteLine(string? value = null)
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

        public SourceBuilder WriteLineIf(bool condition, string? value)
        {
            if (condition)
            {
                WriteLine(value);
            }

            return this;
        }

        public SourceBuilder WriteNullableContextOptionIf(bool enabled) => WriteLineIf(enabled, "#nullable enable");

        public SourceBuilder WriteOpeningBracket()
        {
            _indentedWriter.WriteLine("{");
            _indentedWriter.Indent++;

            return this;
        }

        public SourceBuilder WriteClosingBracket()
        {
            _indentedWriter.Indent--;
            _indentedWriter.WriteLine("}");

            return this;
        }

        public SourceBuilder WriteUsings(IEnumerable<string> usings)
        {
            foreach (var u in usings.OrderBy(s => s))
            {
                WriteUsing(u);
            }

            return this;
        }

        public SourceBuilder WriteUsing(string u)
        {
            WriteLine($"using {u};");

            return this;
        }

        /// <inheritdoc />
        public override string ToString() => _writer.ToString();
    }
}