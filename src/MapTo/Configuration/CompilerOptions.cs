namespace MapTo.Configuration;

/// <summary>
/// Represents the compiler options used for generating the MapTo methods.
/// </summary>
/// <param name="NullableReferenceTypes">Whether to support nullable reference types.</param>
/// <param name="NullableStaticAnalysis">Whether to support nullable static analysis attributes.</param>
/// <param name="FileScopedNamespace">Whether to support file-scoped namespace.</param>
internal readonly record struct CompilerOptions(
    bool NullableReferenceTypes = true,
    bool NullableStaticAnalysis = true,
    bool FileScopedNamespace = true)
{
    /// <summary>
    /// Gets the nullable reference syntax. Returns <c>?</c> if <see cref="NullableReferenceTypes" /> is <c>true</c>,
    /// otherwise returns <see cref="string.Empty" />.
    /// </summary>
    public string NullableReferenceSyntax => NullableReferenceTypes ? "?" : string.Empty;

    internal static CompilerOptions From(Compilation compilation)
    {
        var languageVersion = compilation is CSharpCompilation cs ? cs.LanguageVersion : LanguageVersion.CSharp7;

        return new CompilerOptions(
            compilation.Options.NullableContextOptions is NullableContextOptions.Warnings or NullableContextOptions.Enable,
            compilation.TypeByMetadataNameExists(WellKnownTypes.NotNullIfNotNullAttribute),
            languageVersion >= LanguageVersion.CSharp10);
    }
}