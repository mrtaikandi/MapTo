using MapTo.Mappings.Handlers;

namespace MapTo.Mappings;

internal readonly record struct EnumTypeMapping(
    EnumMappingStrategy Strategy,
    ImmutableArray<EnumMemberMapping> Mappings,
    string? FallBackValue,
    bool Initialized = true)
{
    public bool IsNull => !Initialized;
}

internal static class EnumTypeMappingFactory
{
    public static EnumTypeMapping Create(MappingContext context)
    {
        var mapFromAttribute = context.Configuration;
        var enumMappingStrategy = GetEnumMappingStrategy(context, mapFromAttribute);
        var memberMappings = GetMemberMappings(context, enumMappingStrategy);

        if (memberMappings.IsFailure)
        {
            context.ReportDiagnostic(memberMappings.Error);
            return default;
        }

        return new EnumTypeMapping(
            enumMappingStrategy,
            memberMappings.Value,
            GetFallbackValue(context.TargetTypeSymbol, mapFromAttribute));
    }

    internal static string? GetFallbackValue(ITypeSymbol enumTypeSymbol, MappingConfiguration? mapFromAttribute)
    {
        var value = mapFromAttribute?.EnumMappingFallbackValue;
        return value is null ? null : enumTypeSymbol.GetMembers().OfType<IFieldSymbol>().SingleOrDefault(m => m.ConstantValue == value)?.ToDisplayString();
    }

    internal static EnumMappingStrategy GetEnumMappingStrategy(MappingContext context, MappingConfiguration? mapFromAttribute) =>
        mapFromAttribute?.EnumMappingStrategy ?? context.CodeGeneratorOptions.EnumMappingStrategy;

    internal static bool VerifyIgnoreEnumMemberAttributeUsage(
        MappingContext context,
        IEnumerable<IFieldSymbol?>? members,
        IEnumerable<AttributeData> attributes,
        [NotNullWhen(false)] out Diagnostic? error)
    {
        error = null;
        var knownTypes = context.KnownTypes;

        var faultyAttribute = members?.Where(m => m is not null)
            .SelectMany(m => m!.GetAttributes(knownTypes.IgnoreEnumMemberAttributeTypeSymbol))
            .FirstOrDefault(a => a.ConstructorArguments.First().Value is not null);

        if (faultyAttribute is not null)
        {
            error = DiagnosticsFactory.IgnoreEnumMemberWithParameterOnMemberError(faultyAttribute, knownTypes.IgnoreEnumMemberAttributeTypeSymbol);
            return false;
        }

        faultyAttribute = attributes.FirstOrDefault(a => a.ConstructorArguments.First().Value is null);
        if (faultyAttribute is not null)
        {
            error = DiagnosticsFactory.IgnoreEnumMemberWithoutParameterTypeError(faultyAttribute, knownTypes.IgnoreEnumMemberAttributeTypeSymbol);
            return false;
        }

        return true;
    }

    internal static bool VerifyStrictMappingsConditions(
        MappingContext context,
        ITypeSymbol targeTypeSymbol,
        ITypeSymbol targetEnumTypeSymbol,
        ITypeSymbol sourceEnumTypeSymbol,
        EnumMappingStrategy enumMappingStrategy,
        [NotNullWhen(false)] out Diagnostic? error)
    {
        error = null;
        var knownTypes = context.KnownTypes;
        var mapFromAttribute = context.Configuration;

        var strictEnumMapping = mapFromAttribute.StrictEnumMapping ?? StrictEnumMapping.Off;
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

        var ignoredMembers = targeTypeSymbol
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

    private static ResolverResult<ImmutableArray<EnumMemberMapping>> GetMemberMappings(MappingContext context, EnumMappingStrategy enumMappingStrategy)
    {
        var knownTypes = context.KnownTypes;
        var enumTypeSymbol = context.TargetTypeSymbol;
        var sourceEnumTypeSymbol = context.SourceTypeSymbol;

        var sourceMembers = sourceEnumTypeSymbol.GetMembers().OfType<IFieldSymbol>().Where(m => m.HasConstantValue).ToArray();
        var members = enumTypeSymbol.GetMembers().OfType<IFieldSymbol>()
            .Where(m => m.HasConstantValue && !m.HasAttribute(context.KnownTypes.IgnoreEnumMemberAttributeTypeSymbol))
            .OrderBy(m => m.ConstantValue)
            .ToArray();

        var stringComparison = enumMappingStrategy is EnumMappingStrategy.ByNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        var allMembers = members.Union<IFieldSymbol>(sourceMembers, SymbolEqualityComparer.Default);
        var allAttributes = new[] { context.TargetTypeSymbol, context.SourceTypeSymbol }.SelectMany(t => t.GetAttributes(knownTypes.IgnoreEnumMemberAttributeTypeSymbol));

        if (!VerifyIgnoreEnumMemberAttributeUsage(context, allMembers, allAttributes, out var error))
        {
            return error;
        }

        if (!VerifyStrictMappingsConditions(context, enumTypeSymbol, enumTypeSymbol, sourceEnumTypeSymbol, enumMappingStrategy, out error))
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
                builder.Add(new EnumMemberMapping(sourceMember.ToDisplayString(), member.ToDisplayString()));
            }
        }

        return builder.ToImmutable();
    }
}