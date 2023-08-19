using MapTo.Configuration;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.Mappings;

internal readonly record struct MappingContext(
    ClassDeclarationSyntax TargetTypeSyntax,
    INamedTypeSymbol TargetTypeSymbol,
    SemanticModel TargetSemanticModel,
    INamedTypeSymbol SourceTypeSymbol,
    WellKnownTypes WellKnownTypes,
    CodeGeneratorOptions CodeGeneratorOptions = default,
    CompilerOptions CompilerOptions = default)
{
    private readonly List<Diagnostic> _diagnostics = new();

    internal bool HasError => Diagnostics.Count > 0;

    internal IReadOnlyCollection<Diagnostic> Diagnostics => _diagnostics;

    internal Compilation Compilation => TargetSemanticModel.Compilation;

    public static MappingContext WithOptions((MappingContext Builder, CodeGeneratorOptions Options) source, CancellationToken cancellationToken) => source.Builder with
    {
        CodeGeneratorOptions = source.Options,
        CompilerOptions = CompilerOptions.From(source.Builder.TargetSemanticModel.Compilation)
    };

    internal void ReportDiagnostic(Diagnostic diagnostic) => _diagnostics.Add(diagnostic);
}

internal static class MappingContextExtensions
{
    public static IncrementalValuesProvider<MappingContext> CreateMappingContext(this SyntaxValueProvider provider) => provider.ForAttributeWithMetadataName(
        WellKnownTypes.MapFromAttributeFullyQualifiedName,
        static (node, _) => node is ClassDeclarationSyntax,
        static (context, _) => new MappingContext(
            context.TargetNode as ClassDeclarationSyntax ?? throw new InvalidOperationException("TargetNode is not a ClassDeclarationSyntax"),
            context.TargetSymbol as INamedTypeSymbol ?? throw new InvalidOperationException("TargetSymbol is not a ITypeSymbol"),
            context.SemanticModel,
            context.Attributes.Single().ConstructorArguments.Single().Value as INamedTypeSymbol ?? throw new InvalidOperationException("SourceType is not a ITypeSymbol"),
            WellKnownTypes.Create(context.SemanticModel.Compilation)));
}