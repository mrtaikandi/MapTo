using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.Mappings;

internal readonly record struct MappingContext(
    BaseTypeDeclarationSyntax TargetTypeSyntax,
    INamedTypeSymbol TargetTypeSymbol,
    SemanticModel TargetSemanticModel,
    INamedTypeSymbol SourceTypeSymbol,
    KnownTypes KnownTypes,
    AttributeData MapFromAttribute,
    CodeGeneratorOptions CodeGeneratorOptions = default,
    CompilerOptions CompilerOptions = default)
{
    private readonly List<Diagnostic> _diagnostics = new();

    internal bool HasError => Diagnostics.Count > 0;

    internal IReadOnlyCollection<Diagnostic> Diagnostics => _diagnostics;

    internal Compilation Compilation => TargetSemanticModel.Compilation;

    public void Deconstruct(
        out BaseTypeDeclarationSyntax targetTypeSyntax,
        out INamedTypeSymbol targetTypeSymbol,
        out SemanticModel targetSemanticModel,
        out INamedTypeSymbol sourceTypeSymbol,
        out KnownTypes knownTypes,
        out CodeGeneratorOptions codeGeneratorOptions,
        out CompilerOptions compilerOptions)
    {
        targetTypeSyntax = TargetTypeSyntax;
        targetTypeSymbol = TargetTypeSymbol;
        targetSemanticModel = TargetSemanticModel;
        sourceTypeSymbol = SourceTypeSymbol;
        knownTypes = KnownTypes;
        codeGeneratorOptions = CodeGeneratorOptions;
        compilerOptions = CompilerOptions;
    }

    public static MappingContext WithOptions((MappingContext Builder, CodeGeneratorOptions Options) source, CancellationToken cancellationToken)
    {
        var mapFromAttributeData = source.Builder.MapFromAttribute;
        var compilation = source.Builder.TargetSemanticModel.Compilation;
        return source.Builder with
        {
            CompilerOptions = CompilerOptions.From(compilation),
            CodeGeneratorOptions = source.Options with
            {
                CopyPrimitiveArrays = mapFromAttributeData.GetNamedArgument(nameof(MapTo.MapFromAttribute.CopyPrimitiveArrays), source.Options.CopyPrimitiveArrays),
                ReferenceHandling = mapFromAttributeData.GetNamedArgument(nameof(MapTo.MapFromAttribute.ReferenceHandling), source.Options.ReferenceHandling),
                EnumMappingStrategy = mapFromAttributeData.GetNamedArgument(nameof(MapTo.MapFromAttribute.EnumMappingStrategy), source.Options.EnumMappingStrategy),
                StrictEnumMapping = mapFromAttributeData.GetNamedArgument(nameof(MapTo.MapFromAttribute.StrictEnumMapping), source.Options.StrictEnumMapping),
                ProjectionType = mapFromAttributeData.GetNamedArgument(nameof(MapTo.MapFromAttribute.ProjectTo), source.Options.ProjectionType),
                NullHandling = mapFromAttributeData.GetNamedArgument(
                    nameof(MapTo.MapFromAttribute.NullHandling),
                    compilation.Options.NullableContextOptions is NullableContextOptions.Disable && source.Options.NullHandling == NullHandling.Auto
                        ? NullHandling.SetNull
                        : source.Options.NullHandling)
            }
        };
    }

    internal void ReportDiagnostic(Diagnostic diagnostic) => _diagnostics.Add(diagnostic);

    /// <inheritdoc />
    public bool Equals(MappingContext other)
    {
        return TargetTypeSyntax == other.TargetTypeSyntax &&
               SymbolEqualityComparer.IncludeNullability.Equals(TargetTypeSymbol, other.TargetTypeSymbol) &&
               SymbolEqualityComparer.IncludeNullability.Equals(SourceTypeSymbol, other.SourceTypeSymbol) &&
               CodeGeneratorOptions == other.CodeGeneratorOptions &&
               CompilerOptions == other.CompilerOptions;
    }

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(
        TargetTypeSyntax.GetHashCode(),
        SymbolEqualityComparer.IncludeNullability.GetHashCode(TargetTypeSymbol),
        SymbolEqualityComparer.IncludeNullability.GetHashCode(SourceTypeSymbol),
        CodeGeneratorOptions.GetHashCode(),
        CompilerOptions.GetHashCode());
}

internal static class MappingContextExtensions
{
    public static IncrementalValuesProvider<MappingContext> CreateMappingContext(this SyntaxValueProvider provider) => provider.CreateSyntaxProvider(
            static (node, _) => node.HasMapFromAttribute(),
            static (context, ctx) =>
            {
                var semanticModel = context.SemanticModel;

                var knownTypes = KnownTypes.Create(semanticModel.Compilation);
                var targetNode = context.Node as BaseTypeDeclarationSyntax ?? throw new InvalidOperationException("TargetNode is not a ClassDeclarationSyntax");
                var targetSymbol = semanticModel.GetDeclaredSymbol(targetNode, cancellationToken: ctx) ?? throw new InvalidOperationException("TargetSymbol is not a ITypeSymbol");

                var mapFromAttribute = targetSymbol.GetAttribute(knownTypes.GenericMapFromAttributeTypeSymbol)
                                       ?? targetSymbol.GetAttribute<MapFromAttribute>() ?? throw new InvalidOperationException("MapFromAttribute is not found");

                var sourceTypeSymbol = mapFromAttribute.AttributeClass!.IsGenericType
                    ? mapFromAttribute.AttributeClass.TypeArguments.First() as INamedTypeSymbol
                    : mapFromAttribute.ConstructorArguments.First().Value as INamedTypeSymbol ?? throw new InvalidOperationException("SourceType is not a ITypeSymbol");

                return new MappingContext(
                    TargetTypeSyntax: targetNode,
                    TargetTypeSymbol: targetSymbol,
                    TargetSemanticModel: semanticModel,
                    SourceTypeSymbol: sourceTypeSymbol ?? throw new InvalidOperationException("SourceType is not a ITypeSymbol"),
                    KnownTypes: knownTypes,
                    MapFromAttribute: mapFromAttribute);
            })
        .WithTrackingName(nameof(CreateMappingContext));

    private static bool HasMapFromAttribute(this SyntaxNode node) =>
        node is BaseTypeDeclarationSyntax typeDeclarationSyntax && typeDeclarationSyntax.HasAttribute(KnownTypes.FriendlyMapFromAttributeName);
}