using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.Mappings;

internal readonly struct TargetClassDeclaration
{
    internal TargetClassDeclaration(
        ClassDeclarationSyntax targetNode,
        ITypeSymbol targetSymbol,
        SemanticModel semanticModel,
        ImmutableArray<AttributeData> attributes)
    {
        TargetNode = targetNode ?? throw new ArgumentNullException(nameof(targetNode));
        TargetSymbol = targetSymbol ?? throw new ArgumentNullException(nameof(targetSymbol));
        SemanticModel = semanticModel;
        Attributes = attributes;
    }

    /// <summary>
    /// Gets the syntax node the MapFrom attribute is attached to.  For example, with <c>[MapFrom] class C { }</c> this would
    /// the class declaration node.
    /// </summary>
    public ClassDeclarationSyntax TargetNode { get; }

    /// <summary>
    /// Gets the symbol that the MapFrom attribute is attached to.  For example, with <c>[MapFrom] class C { }</c> this would be
    /// the <see cref="INamedTypeSymbol" /> for <c>"C"</c>.
    /// </summary>
    public ITypeSymbol TargetSymbol { get; }

    /// <summary>
    /// Gets the semantic model for the file that <see cref="TargetNode" /> is contained within.
    /// </summary>
    public SemanticModel SemanticModel { get; }

    /// <summary>
    /// Gets the <see cref="AttributeData" />s for any matching attributes on <see cref="TargetSymbol" />.  Always non-empty.  All
    /// these attributes will have an <see cref="AttributeData.AttributeClass" /> whose fully qualified name metadata
    /// name matches the name requested in <see cref="SyntaxValueProvider.ForAttributeWithMetadataName{T}" />.
    /// <para>
    /// To get the entire list of attributes, use <see cref="ISymbol.GetAttributes" /> on <see cref="TargetSymbol" />.
    /// </para>
    /// </summary>
    public ImmutableArray<AttributeData> Attributes { get; }
}