using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.Mappings;

internal readonly record struct MappingContext(
    BaseTypeDeclarationSyntax? TargetTypeSyntax,
    INamedTypeSymbol TargetTypeSymbol,
    SemanticModel TargetSemanticModel,
    INamedTypeSymbol SourceTypeSymbol,
    KnownTypes KnownTypes,
    AttributeDataMapping AttributeDataMapping,
    CodeGeneratorOptions CodeGeneratorOptions = default,
    CompilerOptions CompilerOptions = default)
{
    private readonly List<Diagnostic> _diagnostics = new();

    internal bool HasError => Diagnostics.Count > 0;

    internal IReadOnlyCollection<Diagnostic> Diagnostics => _diagnostics;

    internal Compilation Compilation => TargetSemanticModel.Compilation;

    public void Deconstruct(
        out BaseTypeDeclarationSyntax? targetTypeSyntax,
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
        var mapFromAttributeData = source.Builder.AttributeDataMapping;
        var compilation = source.Builder.TargetSemanticModel.Compilation;
        return source.Builder with
        {
            CompilerOptions = CompilerOptions.From(compilation),
            CodeGeneratorOptions = source.Options with
            {
                CopyPrimitiveArrays = mapFromAttributeData.CopyPrimitiveArrays ?? source.Options.CopyPrimitiveArrays,
                ReferenceHandling = mapFromAttributeData.ReferenceHandling ?? source.Options.ReferenceHandling,
                EnumMappingStrategy = mapFromAttributeData.EnumMappingStrategy ?? source.Options.EnumMappingStrategy,
                StrictEnumMapping = mapFromAttributeData.StrictEnumMapping ?? source.Options.StrictEnumMapping,
                ProjectionType = mapFromAttributeData.ProjectTo ?? source.Options.ProjectionType,
                NullHandling = mapFromAttributeData.NullHandling ??
                               (compilation.Options.NullableContextOptions is NullableContextOptions.Disable && source.Options.NullHandling == NullHandling.Auto
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
        TargetTypeSyntax?.GetHashCode(),
        SymbolEqualityComparer.IncludeNullability.GetHashCode(TargetTypeSymbol),
        SymbolEqualityComparer.IncludeNullability.GetHashCode(SourceTypeSymbol),
        CodeGeneratorOptions.GetHashCode(),
        CompilerOptions.GetHashCode());
}

internal static class MappingContextExtensions
{
    public static IncrementalValuesProvider<MappingContext> CreateMappingContext(this SyntaxValueProvider provider) => provider.CreateSyntaxProvider(
            static (node, _) => node.HasMapFromAttribute(),
            static (context, cancellationToken) =>
            {
                var semanticModel = context.SemanticModel;

                var knownTypes = KnownTypes.Create(semanticModel.Compilation);
                AttributeData mapAttribute;
                INamedTypeSymbol sourceTypeSymbol;
                INamedTypeSymbol targetTypeSymbol;
                BaseTypeDeclarationSyntax? targetTypeSyntax;

                if (context.Node is AttributeListSyntax attributeListSyntax)
                {
                    var mapAttributeSyntax = attributeListSyntax.Attributes.SingleOrDefault() ?? throw new InvalidOperationException("MapAttribute is not found");
                    var mapAttributeTypeInfo = semanticModel.GetTypeInfo(mapAttributeSyntax, cancellationToken: cancellationToken);
                    mapAttribute = semanticModel.Compilation.Assembly.GetAttributes()
                        .Single(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, mapAttributeTypeInfo.Type));

                    sourceTypeSymbol = mapAttribute.AttributeClass?.TypeArguments[0] as INamedTypeSymbol ?? throw new InvalidOperationException("TFrom type is not found");
                    targetTypeSymbol = mapAttribute.AttributeClass?.TypeArguments[1] as INamedTypeSymbol ?? throw new InvalidOperationException("TTo type is not found");

                    targetTypeSyntax = targetTypeSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) as BaseTypeDeclarationSyntax;
                }
                else
                {
                    targetTypeSyntax = context.Node as BaseTypeDeclarationSyntax ?? throw new InvalidOperationException("TargetNode is not a ClassDeclarationSyntax");
                    targetTypeSymbol = semanticModel.GetDeclaredSymbol(targetTypeSyntax, cancellationToken: cancellationToken) ??
                                   throw new InvalidOperationException("TargetSymbol is not an ITypeSymbol");

                    mapAttribute = targetTypeSymbol.GetAttribute(knownTypes.GenericMapFromAttributeTypeSymbol)
                                   ?? targetTypeSymbol.GetAttribute<MapFromAttribute>() ?? throw new InvalidOperationException("MapFromAttribute is not found");

                    var sourceType = mapAttribute.AttributeClass?.IsGenericType == true
                        ? mapAttribute.AttributeClass.TypeArguments.First() as INamedTypeSymbol
                        : mapAttribute.ConstructorArguments.First().Value as INamedTypeSymbol;

                    sourceTypeSymbol = sourceType ?? throw new InvalidOperationException("SourceType is not a ITypeSymbol");
                }

                return new MappingContext(
                    TargetTypeSyntax: targetTypeSyntax,
                    TargetTypeSymbol: targetTypeSymbol,
                    TargetSemanticModel: semanticModel,
                    SourceTypeSymbol: sourceTypeSymbol ?? throw new InvalidOperationException("SourceType is not a ITypeSymbol"),
                    KnownTypes: knownTypes,
                    AttributeDataMapping: mapAttribute.ToMapAttributeData());
            })
        .WithTrackingName(nameof(CreateMappingContext));

    [SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1513:Closing brace should be followed by blank line", Justification = "Used in pattern matching")]
    private static bool HasMapFromAttribute(this SyntaxNode node)
    {
        if (node is AttributeListSyntax attributeListSyntax)
        {
            return attributeListSyntax.Target?.Identifier.IsKind(SyntaxKind.AssemblyKeyword) == true &&
                   attributeListSyntax.Attributes.Any(
                       a => a.Name is QualifiedNameSyntax
                           {
                               Left: SimpleNameSyntax { Identifier.Text: KnownTypes.FriendlyMapToNamespace },
                               Right.Identifier.Text: KnownTypes.FriendlyMapAttributeName
                           }
                           or
                           SimpleNameSyntax { Identifier.Text: KnownTypes.FriendlyMapAttributeName });
        }

        return node is BaseTypeDeclarationSyntax typeDeclarationSyntax && typeDeclarationSyntax.HasAttribute(KnownTypes.FriendlyMapFromAttributeName);
    }
}