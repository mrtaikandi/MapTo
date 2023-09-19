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

    internal AttributeData MapFromAttributeData { get; init; }

    public static MappingContext WithOptions((MappingContext Builder, CodeGeneratorOptions Options) source, CancellationToken cancellationToken)
    {
        var codeGenOptions = source.Options;
        if (source.Builder.MapFromAttributeData.TryGetNamedArgument(WellKnownTypes.MapFromReferenceHandlingPropertyName, out var value))
        {
            codeGenOptions = codeGenOptions with
            {
                // Values are from src/MapTo/Resources/ReferenceHandling.cs
                UseReferenceHandling = value.Value switch { 1 => true, 2 => false, _ => null }
            };
        }

        return source.Builder with
        {
            CodeGeneratorOptions = codeGenOptions,
            CompilerOptions = CompilerOptions.From(source.Builder.TargetSemanticModel.Compilation)
        };
    }

    internal void ReportDiagnostic(Diagnostic diagnostic) => _diagnostics.Add(diagnostic);
}

internal static class MappingContextExtensions
{
    public static IncrementalValuesProvider<MappingContext> CreateMappingContext(this SyntaxValueProvider provider) => provider.ForAttributeWithMetadataName(
        WellKnownTypes.MapFromAttributeFullyQualifiedName,
        static (node, _) => node is ClassDeclarationSyntax,
        static (context, _) =>
        {
            var mapFromAttribute = context.Attributes.Single();
            return new MappingContext(
                TargetTypeSyntax: context.TargetNode as ClassDeclarationSyntax ?? throw new InvalidOperationException("TargetNode is not a ClassDeclarationSyntax"),
                TargetTypeSymbol: context.TargetSymbol as INamedTypeSymbol ?? throw new InvalidOperationException("TargetSymbol is not a ITypeSymbol"),
                TargetSemanticModel: context.SemanticModel,
                SourceTypeSymbol: mapFromAttribute.ConstructorArguments.First().Value as INamedTypeSymbol ?? throw new InvalidOperationException("SourceType is not a ITypeSymbol"),
                WellKnownTypes: WellKnownTypes.Create(context.SemanticModel.Compilation))
            {
                MapFromAttributeData = mapFromAttribute
            };
        });
}