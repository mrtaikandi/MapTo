using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace MapTo.Models
{
    internal record MapModel (
        SourceGenerationOptions Options,
        string? Namespace,
        SyntaxTokenList ClassModifiers,
        string ClassName,
        string SourceNamespace,
        string SourceClassName,
        string SourceClassFullName,
        ImmutableArray<string> MappedProperties
    );
}