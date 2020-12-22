using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace MapTo.Models
{
    public class MapModel
    {
        internal MapModel(
            string? ns,
            SyntaxTokenList classModifiers,
            string className,
            string sourceNamespace,
            string sourceClassName,
            string sourceClassFullName,
            ImmutableArray<string> mappedProperties)
        {
            Namespace = ns;
            ClassModifiers = classModifiers;
            ClassName = className;
            SourceNamespace = sourceNamespace;
            SourceClassName = sourceClassName;
            SourceClassFullName = sourceClassFullName;
            MappedProperties = mappedProperties;
        }

        public string? Namespace { get; }

        public SyntaxTokenList ClassModifiers { get; }

        public string ClassName { get; }

        public string SourceNamespace { get; }

        public string SourceClassName { get; }

        public string SourceClassFullName { get; }

        public ImmutableArray<string> MappedProperties { get; }
    }
}