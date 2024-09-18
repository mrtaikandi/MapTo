using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.Mappings;

internal readonly record struct MappingContext(
    BaseTypeDeclarationSyntax? TargetTypeSyntax,
    INamedTypeSymbol TargetTypeSymbol,
    SemanticModel TargetSemanticModel,
    INamedTypeSymbol SourceTypeSymbol,
    KnownTypes KnownTypes,
    MappingConfiguration Configuration,
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
        var configuration = source.Builder.Configuration;
        var compilation = source.Builder.TargetSemanticModel.Compilation;

        return source.Builder with
        {
            CompilerOptions = CompilerOptions.From(compilation),
            CodeGeneratorOptions = source.Options with
            {
                CopyPrimitiveArrays = configuration.CopyPrimitiveArrays ?? source.Options.CopyPrimitiveArrays,
                ReferenceHandling = configuration.ReferenceHandling ?? source.Options.ReferenceHandling,
                EnumMappingStrategy = configuration.EnumMappingStrategy ?? source.Options.EnumMappingStrategy,
                StrictEnumMapping = configuration.StrictEnumMapping ?? source.Options.StrictEnumMapping,
                ProjectionType = configuration.ProjectTo ?? source.Options.ProjectionType,
                NullHandling = configuration.NullHandling ??
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
                AttributeData? mapAttribute;
                INamedTypeSymbol sourceTypeSymbol;
                INamedTypeSymbol targetTypeSymbol;
                BaseTypeDeclarationSyntax? targetTypeSyntax;
                Result<MappingConfiguration> mappingConfiguration;

                if (context.Node is AttributeListSyntax attributeListSyntax)
                {
                    var mapAttributeSyntax = attributeListSyntax.Attributes.SingleOrDefault() ?? throw new InvalidOperationException("MapAttribute is not found");
                    var mapAttributeTypeInfo = semanticModel.GetTypeInfo(mapAttributeSyntax, cancellationToken: cancellationToken);

                    if (attributeListSyntax.Parent is not null &&
                        semanticModel.GetDeclaredSymbol(attributeListSyntax.Parent, cancellationToken: cancellationToken) is { } parentSymbol)
                    {
                        mapAttribute = parentSymbol.GetAttributes().Single(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, mapAttributeTypeInfo.Type));
                    }
                    else
                    {
                        mapAttribute = semanticModel.Compilation.Assembly.GetAttributes()
                            .Single(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, mapAttributeTypeInfo.Type));
                    }

                    sourceTypeSymbol = mapAttribute.AttributeClass?.TypeArguments[0] as INamedTypeSymbol ?? throw new InvalidOperationException("TFrom type is not found");
                    targetTypeSymbol = mapAttribute.AttributeClass?.TypeArguments[1] as INamedTypeSymbol ?? throw new InvalidOperationException("TTo type is not found");

                    targetTypeSyntax = targetTypeSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) as BaseTypeDeclarationSyntax;
                    mappingConfiguration = mapAttribute.ToMappingConfiguration(context.SemanticModel, sourceTypeSymbol, targetTypeSymbol);
                }
                else
                {
                    targetTypeSyntax = context.Node as BaseTypeDeclarationSyntax ?? throw new InvalidOperationException("TargetNode is not a ClassDeclarationSyntax");
                    targetTypeSymbol = semanticModel.GetDeclaredSymbol(targetTypeSyntax, cancellationToken: cancellationToken) ??
                                       throw new InvalidOperationException("TargetSymbol is not an ITypeSymbol");

                    INamedTypeSymbol? sourceType;
                    mapAttribute = targetTypeSymbol.GetAttribute(knownTypes.GenericMapFromAttributeTypeSymbol) ?? targetTypeSymbol.GetAttribute<MapFromAttribute>();

                    if (mapAttribute is not null)
                    {
                        mappingConfiguration = mapAttribute.ToMappingConfiguration();
                        sourceType = mapAttribute.AttributeClass?.IsGenericType == true
                            ? mapAttribute.AttributeClass.TypeArguments.First() as INamedTypeSymbol
                            : mapAttribute.ConstructorArguments.First().Value as INamedTypeSymbol;
                    }
                    else
                    {
                        mapAttribute = targetTypeSymbol.GetAttribute(knownTypes.MapAttributeTypeSymbol) ?? throw new InvalidOperationException("MapFromAttribute is not found");
                        sourceType = mapAttribute.AttributeClass?.TypeArguments[0] as INamedTypeSymbol ?? throw new InvalidOperationException("TFrom type is not found");
                        targetTypeSymbol = mapAttribute.AttributeClass?.TypeArguments[1] as INamedTypeSymbol ?? throw new InvalidOperationException("TTo type is not found");
                        targetTypeSyntax = targetTypeSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) as BaseTypeDeclarationSyntax;

                        mappingConfiguration = mapAttribute.ToMappingConfiguration(context.SemanticModel, sourceType, targetTypeSymbol);
                    }

                    sourceTypeSymbol = sourceType ?? throw new InvalidOperationException("SourceType is not a ITypeSymbol");
                }

                var mappingContext = new MappingContext(
                    TargetTypeSyntax: targetTypeSyntax,
                    TargetTypeSymbol: targetTypeSymbol,
                    TargetSemanticModel: semanticModel,
                    SourceTypeSymbol: sourceTypeSymbol ?? throw new InvalidOperationException("SourceType is not a ITypeSymbol"),
                    KnownTypes: knownTypes,
                    Configuration: mappingConfiguration.IsSuccess ? mappingConfiguration.Value : default);

                if (mappingConfiguration.IsFailure)
                {
                    mappingContext.ReportDiagnostic(mappingConfiguration.Error);
                }

                return mappingContext;
            })
        .WithTrackingName(nameof(CreateMappingContext));

    [SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1513:Closing brace should be followed by blank line", Justification = "Used in pattern matching")]
    private static bool HasMapFromAttribute(this SyntaxNode node) => node switch
    {
        AttributeListSyntax attributeListSyntax => attributeListSyntax.HasAttribute(KnownTypes.FriendlyMapAttributeName),
        BaseTypeDeclarationSyntax typeDeclarationSyntax => typeDeclarationSyntax.HasAttribute(KnownTypes.FriendlyMapFromAttributeName),
        _ => false
    };
}