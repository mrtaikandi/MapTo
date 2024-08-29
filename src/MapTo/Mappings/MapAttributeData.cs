using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.Mappings;

internal readonly record struct AttributeDataMapping(
    TypeMapping SourceType,
    bool? CopyPrimitiveArrays,
    ReferenceHandling? ReferenceHandling,
    NullHandling? NullHandling,
    EnumMappingStrategy? EnumMappingStrategy,
    StrictEnumMapping? StrictEnumMapping,
    object? EnumMappingFallbackValue,
    ProjectionType? ProjectTo,
    ExpressionSyntax? BeforeMap,
    Location BeforeMapArgumentLocation,
    ExpressionSyntax? AfterMap,
    Location AfterMapArgumentLocation);

internal static class AttributeDataMappingExtensions
{
    public static AttributeDataMapping ToMapAttributeData(this AttributeData attribute)
    {
        var sourceType = attribute switch
        {
            { AttributeClass.TypeArguments.Length: > 0 } => attribute.AttributeClass!.TypeArguments[0],
            { ConstructorArguments.Length: > 0 } => attribute.ConstructorArguments[0].Value as ITypeSymbol,
            _ => null
        };

        return new AttributeDataMapping(
            SourceType: sourceType?.ToTypeMapping() ?? throw new InvalidOperationException("Unable to determine source type from MapTo attribute."),
            CopyPrimitiveArrays: attribute.GetNamedArgumentOrNull<bool>(nameof(MapFromAttribute.CopyPrimitiveArrays)),
            ReferenceHandling: attribute.GetNamedArgumentOrNull<ReferenceHandling>(nameof(MapFromAttribute.ReferenceHandling)),
            NullHandling: attribute.GetNamedArgumentOrNull<NullHandling>(nameof(MapFromAttribute.NullHandling)),
            EnumMappingStrategy: attribute.GetNamedArgumentOrNull<EnumMappingStrategy>(nameof(MapFromAttribute.EnumMappingStrategy)),
            StrictEnumMapping: attribute.GetNamedArgumentOrNull<StrictEnumMapping>(nameof(MapFromAttribute.StrictEnumMapping)),
            EnumMappingFallbackValue: attribute.GetNamedArgument(nameof(MapFromAttribute.EnumMappingFallbackValue)),
            ProjectTo: attribute.GetNamedArgumentOrNull<ProjectionType>(nameof(MapFromAttribute.ProjectTo)),
            BeforeMap: attribute.GetNamedArgumentExpression(nameof(MapFromAttribute.BeforeMap)),
            AfterMap: attribute.GetNamedArgumentExpression(nameof(MapFromAttribute.AfterMap)),
            BeforeMapArgumentLocation: attribute.GetNamedArgumentLocation(nameof(MapFromAttribute.BeforeMap)),
            AfterMapArgumentLocation: attribute.GetNamedArgumentLocation(nameof(MapFromAttribute.AfterMap)));
    }
}