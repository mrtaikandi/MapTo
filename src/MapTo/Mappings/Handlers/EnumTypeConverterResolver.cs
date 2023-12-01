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
        var enumMappingStrategy = GetEnumMappingStrategy(context, property);

        return new TypeConverterMapping(
            ContainingType: string.Empty,
            MethodName: $"{methodPrefix}{sourceProperty.TypeSymbol.Name}",
            Type: property.Type.ToTypeMapping(),
            EnumMapping: new EnumTypeMapping(
                Strategy: enumMappingStrategy,
                Mappings: GetMemberMappings(property.Type, sourceProperty.TypeSymbol, enumMappingStrategy)));
    }

    private static EnumMappingStrategy GetEnumMappingStrategy(MappingContext context, IPropertySymbol property)
    {
        // NB: Check for the mapping strategy in order of precedence.
        // Property Type > Property > Containing Type > Fall back to code generator options.
        var mapFromAttribute = property.Type.GetAttribute(context.KnownTypes.MapFromAttributeTypeSymbol)
                               ?? property.ContainingType.GetAttribute(context.KnownTypes.MapFromAttributeTypeSymbol);

        return mapFromAttribute.GetNamedArgument(nameof(MapFromAttribute.EnumMappingStrategy), context.CodeGeneratorOptions.EnumMappingStrategy);
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