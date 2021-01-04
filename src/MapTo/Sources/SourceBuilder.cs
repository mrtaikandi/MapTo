using System;
using System.CodeDom.Compiler;
using System.IO;

namespace MapTo.Sources
{
    public sealed class SourceBuilder : IDisposable
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
        
        public SourceBuilder WriteLine(string value)
        {
            _indentedWriter.WriteLine(value);
            return this;
        }
        
        public SourceBuilder WriteLine()
        {
            _indentedWriter.WriteLineNoTabs(string.Empty);
            return this;
        }

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

        /// <inheritdoc />
        public override string ToString() => _writer.ToString();
    }
}