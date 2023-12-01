namespace MapTo.Mappings.Handlers;

internal class EnumTypeConverterResolver : ITypeConverterResolver
{
    /// <inheritdoc />
    public ResolverResult<TypeConverterMapping> Get(MappingContext context, IPropertySymbol property, SourceProperty sourceProperty)
    {
        if (property.Type.TypeKind is not TypeKind.Enum)
        {
            return ResolverResult.Undetermined<TypeConverterMapping>();
        }

        var methodPrefix = context.CodeGeneratorOptions.MapMethodPrefix;
        var mapFromAttribute = GetEffectiveMapFromAttribute(context, property);
        var enumMappingStrategy = GetEnumMappingStrategy(context, mapFromAttribute);

        return new TypeConverterMapping(
            ContainingType: string.Empty,
            MethodName: $"{methodPrefix}{sourceProperty.TypeSymbol.Name}",
            Type: property.Type.ToTypeMapping(),
            EnumMapping: new EnumTypeMapping(
                Strategy: enumMappingStrategy,
                Mappings: GetMemberMappings(property.Type, sourceProperty.TypeSymbol, enumMappingStrategy),
                FallBackValue: GetFallbackValue(property.Type, mapFromAttribute)));
    }

    private static AttributeData? GetEffectiveMapFromAttribute(MappingContext context, IPropertySymbol property)
    {
        // NB: Check for the mapping strategy in order of precedence.
        // Property Type > Property > Containing Type > Fall back to code generator options.
        return property.Type.GetAttribute(context.KnownTypes.MapFromAttributeTypeSymbol)
               ?? property.ContainingType.GetAttribute(context.KnownTypes.MapFromAttributeTypeSymbol);
    }

    private static EnumMappingStrategy GetEnumMappingStrategy(MappingContext context, AttributeData? mapFromAttribute) =>
        mapFromAttribute.GetNamedArgument(nameof(MapFromAttribute.EnumMappingStrategy), context.CodeGeneratorOptions.EnumMappingStrategy);

    private static string? GetFallbackValue(ITypeSymbol enumTypeSymbol, AttributeData? mapFromAttribute)
    {
        var value = mapFromAttribute.GetNamedArgument(nameof(MapFromAttribute.EnumMappingFallbackValue));
        return value is null ? null : enumTypeSymbol.GetMembers().OfType<IFieldSymbol>().SingleOrDefault(m => m.ConstantValue == value)?.ToDisplayString();
    }

    private static ImmutableArray<EnumMemberMapping> GetMemberMappings(ITypeSymbol enumTypeSymbol, ITypeSymbol sourceEnumTypeSymbol, EnumMappingStrategy enumMappingStrategy)
    {
        if (enumMappingStrategy is EnumMappingStrategy.ByValue)
        {
            return ImmutableArray<EnumMemberMapping>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<EnumMemberMapping>();
        var members = enumTypeSymbol.GetMembers().OfType<IFieldSymbol>().Where(m => m.HasConstantValue).OrderBy(m => m.ConstantValue);
        var sourceMembers = sourceEnumTypeSymbol.GetMembers().OfType<IFieldSymbol>().Where(m => m.HasConstantValue).ToArray();
        var stringComparison = enumMappingStrategy is EnumMappingStrategy.ByNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (var member in members)
        {
            var sourceMember = sourceMembers.FirstOrDefault(m => m.Name.Equals(member.Name, stringComparison));
            if (sourceMember is not null)
            {
                builder.Add(new(sourceMember.ToDisplayString(), member.ToDisplayString()));
            }
        }

        return builder.ToImmutable();
    }
}