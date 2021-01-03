using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MapTo.Tests.Infrastructure
{
    internal sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        public TestAnalyzerConfigOptionsProvider(IDictionary<string, string> options)
        {
            GlobalOptions = new TestAnalyzerConfigOptions(options);
        }

        /// <inheritdoc />
        public override AnalyzerConfigOptions GlobalOptions { get; }

        /// <inheritdoc />
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => throw new NotImplementedException();

        /// <inheritdoc />
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => throw new NotImplementedException();
    }
}