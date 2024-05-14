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

    private static ResolverResult<ImmutableArray<EnumMemberMapping>> GetMemberMappings(
        MappingContext context,
        IPropertySymbol property,
        SourceProperty sourceProperty,
        EnumMappingStrategy enumMappingStrategy)
    {
        // GetMemberMappings(context.KnownTypes, property.Type, sourceProperty.TypeSymbol, mapFromAttribute, enumMappingStrategy);
        var enumTypeSymbol = property.Type;
        var sourceEnumTypeSymbol = sourceProperty.TypeSymbol;
        var members = enumTypeSymbol.GetMembers().OfType<IFieldSymbol>().Where(m => m.HasConstantValue).OrderBy(m => m.ConstantValue).ToArray();
        var sourceMembers = sourceEnumTypeSymbol.GetMembers().OfType<IFieldSymbol>().Where(m => m.HasConstantValue).ToArray();
        var stringComparison = enumMappingStrategy is EnumMappingStrategy.ByNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        if (!VerifyIgnoreEnumMemberAttributeUsage(context, property, sourceProperty, enumMappingStrategy, out var error))
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
        EnumMappingStrategy enumMappingStrategy,
        [NotNullWhen(false)] out Diagnostic? error)
    {
        error = null;
        var knownTypes = context.KnownTypes;

        var faultyAttribute = property.Type.GetMembers().OfType<IFieldSymbol>()
            .Union(sourceProperty.TypeSymbol.GetMembers().OfType<IFieldSymbol>(), SymbolEqualityComparer.Default)
            .Where(m => m is not null)
            .SelectMany(m => m!.GetAttributes(knownTypes.IgnoreEnumMemberAttributeTypeSymbol))
            .FirstOrDefault(a => a.ConstructorArguments.First().Value is not null);

        if (faultyAttribute is not null)
        {
            error = DiagnosticsFactory.IgnoreEnumMemberWithParameterOnMemberError(faultyAttribute, knownTypes.IgnoreEnumMemberAttributeTypeSymbol);
            return false;
        }

        faultyAttribute = property.Type.GetAttributes(knownTypes.IgnoreEnumMemberAttributeTypeSymbol)
            .Union(property.ContainingType.GetAttributes(knownTypes.IgnoreEnumMemberAttributeTypeSymbol))
            .Union(sourceProperty.TypeSymbol.GetAttributes(knownTypes.IgnoreEnumMemberAttributeTypeSymbol))
            .FirstOrDefault(a => a.ConstructorArguments.First().Value is null);

        if (faultyAttribute is not null)
        {
            error = DiagnosticsFactory.IgnoreEnumMemberWithoutParameterTypeError(faultyAttribute, knownTypes.IgnoreEnumMemberAttributeTypeSymbol);
            return false;
        }

        return true;
    }

    private static bool VerifyStrictMappingsConditions(
        MappingContext context,
        IPropertySymbol property,
        SourceProperty sourceProperty,
        EnumMappingStrategy enumMappingStrategy,
        [NotNullWhen(false)] out Diagnostic? error)
    {
        error = null;
        var knownTypes = context.KnownTypes;
        var mapFromAttribute = context.MapFromAttributeData;
        var targetEnumTypeSymbol = property.Type;
        var sourceEnumTypeSymbol = sourceProperty.TypeSymbol;

        var strictEnumMapping = mapFromAttribute.GetNamedArgument(nameof(MapFromAttribute.StrictEnumMapping), StrictEnumMapping.Off);
        if (strictEnumMapping is StrictEnumMapping.Off)
        {
            return true;
        }

        var stringComparison = enumMappingStrategy is EnumMappingStrategy.ByNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var targetMembers = targetEnumTypeSymbol.GetMembers().OfType<IFieldSymbol>().ToArray();
        var sourceMembers = sourceEnumTypeSymbol.GetMembers().OfType<IFieldSymbol>().ToArray();

        static IEnumerable<(ITypeSymbol Type, object? Value)> SelectEnumTypeIgnoreMember(AttributeData a) =>
            a.ConstructorArguments.Select(c => (Type: c.Type!, c.Value));

        (ITypeSymbol Type, object? Value) SelectIgnoredEnumMember(IFieldSymbol m) =>
            (m.Type, Value: m.GetAttribute(knownTypes.IgnoreEnumMemberAttributeTypeSymbol)?.ConstructorArguments.Single().Value ?? m.ConstantValue);

        var ignoredMembers = property.ContainingType
            .GetAttributes(knownTypes.IgnoreEnumMemberAttributeTypeSymbol)
            .SelectMany(SelectEnumTypeIgnoreMember)
            .Union(targetMembers
                .Where(m => m.HasAttribute(knownTypes.IgnoreEnumMemberAttributeTypeSymbol))
                .Select(SelectIgnoredEnumMember))
            .Union(sourceMembers
                .Where(m => m.HasAttribute(knownTypes.IgnoreEnumMemberAttributeTypeSymbol))
                .Select(SelectIgnoredEnumMember))
            .Union(targetEnumTypeSymbol
                .GetAttributes(knownTypes.IgnoreEnumMemberAttributeTypeSymbol)
                .SelectMany(SelectEnumTypeIgnoreMember))
            .Union(sourceEnumTypeSymbol
                .GetAttributes(knownTypes.IgnoreEnumMemberAttributeTypeSymbol)
                .SelectMany(SelectEnumTypeIgnoreMember))
            .Where(m => m.Value is not null)
            .ToArray();

        targetMembers = targetMembers
            .Where(m => !ignoredMembers.Any(i => SymbolEqualityComparer.Default.Equals(i.Type, m.ContainingType) && Equals(i.Value, m.ConstantValue)))
            .ToArray();

        sourceMembers = sourceMembers
            .Where(m => !ignoredMembers.Any(i => SymbolEqualityComparer.Default.Equals(i.Type, m.ContainingType) && Equals(i.Value, m.ConstantValue)))
            .ToArray();

        if (strictEnumMapping is not StrictEnumMapping.TargetOnly && sourceMembers.Length > targetMembers.Length)
        {
            var missingMember = sourceMembers.First(m => targetMembers.All(m2 => !m2.Name.Equals(m.Name, stringComparison)));
            error = DiagnosticsFactory.StringEnumMappingSourceOnlyError(missingMember, targetEnumTypeSymbol);
            return false;
        }

        if (strictEnumMapping is not StrictEnumMapping.SourceOnly && targetMembers.Length > sourceMembers.Length)
        {
            var missingMember = targetMembers.First(m => sourceMembers.All(m2 => !m2.Name.Equals(m.Name, stringComparison)));
            error = DiagnosticsFactory.StringEnumMappingTargetOnlyError(missingMember, sourceEnumTypeSymbol);
            return false;
        }

        return true;
    }
}