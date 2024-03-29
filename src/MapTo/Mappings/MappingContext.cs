﻿using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.Mappings;

internal readonly record struct MappingContext(
    TypeDeclarationSyntax TargetTypeSyntax,
    INamedTypeSymbol TargetTypeSymbol,
    SemanticModel TargetSemanticModel,
    INamedTypeSymbol SourceTypeSymbol,
    KnownTypes KnownTypes,
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
        var mapFromAttributeData = source.Builder.MapFromAttributeData;
        var compilation = source.Builder.TargetSemanticModel.Compilation;
        return source.Builder with
        {
            CompilerOptions = CompilerOptions.From(compilation),
            CodeGeneratorOptions = source.Options with
            {
                CopyPrimitiveArrays = mapFromAttributeData.GetNamedArgument(nameof(MapFromAttribute.CopyPrimitiveArrays), source.Options.CopyPrimitiveArrays),
                ReferenceHandling = mapFromAttributeData.GetNamedArgument(nameof(MapFromAttribute.ReferenceHandling), source.Options.ReferenceHandling),
                EnumMappingStrategy = mapFromAttributeData.GetNamedArgument(nameof(MapFromAttribute.EnumMappingStrategy), source.Options.EnumMappingStrategy),
                StrictEnumMapping = mapFromAttributeData.GetNamedArgument(nameof(MapFromAttribute.StrictEnumMapping), source.Options.StrictEnumMapping),
                ProjectionType = mapFromAttributeData.GetNamedArgument(nameof(MapFromAttribute.ProjectTo), source.Options.ProjectionType),
                NullHandling = mapFromAttributeData.GetNamedArgument(
                    nameof(MapFromAttribute.NullHandling),
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
    public static IncrementalValuesProvider<MappingContext> CreateMappingContext(this SyntaxValueProvider provider) => provider.ForAttributeWithMetadataName(
            typeof(MapFromAttribute).FullName!,
            static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
            static (context, _) =>
            {
                var mapFromAttribute = context.Attributes.Single();
                return new MappingContext(
                    TargetTypeSyntax: context.TargetNode as TypeDeclarationSyntax ?? throw new InvalidOperationException("TargetNode is not a ClassDeclarationSyntax"),
                    TargetTypeSymbol: context.TargetSymbol as INamedTypeSymbol ?? throw new InvalidOperationException("TargetSymbol is not a ITypeSymbol"),
                    TargetSemanticModel: context.SemanticModel,
                    SourceTypeSymbol: mapFromAttribute.ConstructorArguments.First().Value as INamedTypeSymbol ??
                                      throw new InvalidOperationException("SourceType is not a ITypeSymbol"),
                    KnownTypes: KnownTypes.Create(context.SemanticModel.Compilation))
                {
                    MapFromAttributeData = mapFromAttribute
                };
            })
        .WithTrackingName(nameof(CreateMappingContext));
}