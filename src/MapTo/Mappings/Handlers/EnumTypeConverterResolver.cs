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
        var enumMappingStrategy = EnumTypeMappingFactory.GetEnumMappingStrategy(context, mapFromAttribute);
        var memberMappings = GetMemberMappings(context, property, sourceProperty, enumMappingStrategy);
        if (memberMappings.IsFailure)
        {
            return memberMappings.Error;
        }

        return new TypeConverterMapping(
            ContainingType: string.Empty,
            MethodName: $"{methodPrefix}{sourceProperty.TypeSymbol.Name}",
            Type: property.Type.ToTypeMapping(),
            Explicit: false,
            EnumMapping: new EnumTypeMapping(
                Strategy: enumMappingStrategy,
                Mappings: memberMappings.Value,
                FallBackValue: EnumTypeMappingFactory.GetFallbackValue(property.Type, mapFromAttribute)));
    }

    private static AttributeData? GetEffectiveMapFromAttribute(MappingContext context, IPropertySymbol property)
    {
        // NB: Check for the mapping strategy in order of precedence.
        // Property Type > Property > Containing Type > Fall back to code generator options.
        return property.Type.GetAttribute(context.KnownTypes.MapFromAttributeTypeSymbol)
               ?? property.Type.GetAttribute(context.KnownTypes.MapFromAttributeGenericTypeSymbol)
               ?? property.ContainingType.GetAttribute(context.KnownTypes.MapFromAttributeTypeSymbol)
               ?? property.ContainingType.GetAttribute(context.KnownTypes.MapFromAttributeGenericTypeSymbol);
    }

    private static ResolverResult<ImmutableArray<EnumMemberMapping>> GetMemberMappings(
        MappingContext context,
        IPropertySymbol property,
        SourceProperty sourceProperty,
        EnumMappingStrategy enumMappingStrategy)
    {
        var enumTypeSymbol = property.Type;
        var sourceEnumTypeSymbol = sourceProperty.TypeSymbol;
        var members = enumTypeSymbol.GetMembers().OfType<IFieldSymbol>().Where(m => m.HasConstantValue).OrderBy(m => m.ConstantValue).ToArray();
        var sourceMembers = sourceEnumTypeSymbol.GetMembers().OfType<IFieldSymbol>().Where(m => m.HasConstantValue).ToArray();
        var stringComparison = enumMappingStrategy is EnumMappingStrategy.ByNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        if (!VerifyIgnoreEnumMemberAttributeUsage(context, property, sourceProperty, out var error))
        {
            return error;
        }

        if (!VerifyStrictMappingsConditions(context, property, sourceProperty, enumMappingStrategy, out error))
        {
            return error;
        }

        if (enumMappingStrategy is EnumMappingStrategy.ByValue)
        {
            return ImmutableArray<EnumMemberMapping>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<EnumMemberMapping>();

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

    private static bool VerifyIgnoreEnumMemberAttributeUsage(
        MappingContext context,
        IPropertySymbol property,
        SourceProperty sourceProperty,
        [NotNullWhen(false)] out Diagnostic? error)
    {
        var allMembers = property.Type.GetMembers()
            .OfType<IFieldSymbol>()
            .Union<IFieldSymbol>(sourceProperty.TypeSymbol.GetMembers().OfType<IFieldSymbol>(), SymbolEqualityComparer.Default);

        var knownTypes = context.KnownTypes;
        var attributes = property.Type.GetAttributes(knownTypes.IgnoreEnumMemberAttributeTypeSymbol)
            .Union(property.ContainingType.GetAttributes(knownTypes.IgnoreEnumMemberAttributeTypeSymbol))
            .Union(sourceProperty.TypeSymbol.GetAttributes(knownTypes.IgnoreEnumMemberAttributeTypeSymbol));

        return EnumTypeMappingFactory.VerifyIgnoreEnumMemberAttributeUsage(context, allMembers, attributes, out error);
    }

    private static bool VerifyStrictMappingsConditions(
        MappingContext context,
        IPropertySymbol property,
        SourceProperty sourceProperty,
        EnumMappingStrategy enumMappingStrategy,
        [NotNullWhen(false)] out Diagnostic? error)
    {
        return EnumTypeMappingFactory.VerifyStrictMappingsConditions(
            context,
            property.ContainingType,
            property.Type,
            sourceProperty.TypeSymbol,
            enumMappingStrategy,
            out error);
    }
}