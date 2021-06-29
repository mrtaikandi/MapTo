using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MapTo.Tests.Infrastructure
{
    internal sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly ImmutableDictionary<string, string> _backing;

        public TestAnalyzerConfigOptions(IDictionary<string, string>? properties)
        {
            _backing = properties?.ToImmutableDictionary(KeyComparer) ?? ImmutableDictionary.Create<string, string>(KeyComparer);
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => _backing.TryGetValue(key, out value);
    }
}