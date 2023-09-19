namespace MapTo.Mappings;

internal readonly record struct ConstructorMapping(string Name, bool IsGenerated, ImmutableArray<ConstructorArgumentMapping> Arguments)
{
    public bool HasArguments => Arguments.Length > 0;
}

internal static class ConstructorMappingFactory
{
    public static ConstructorMapping Create(MappingContext context, ImmutableArray<PropertyMapping> properties)
    {
        var (targetType, targetTypeSymbol, _, _, _, _, _) = context;
        var constructorName = targetType.Identifier.ValueText;

        var readonlyProperties = properties.Where(p => p.InitializationMode == PropertyInitializationMode.Constructor).ToArray();
        if (readonlyProperties.Length == 0)
        {
            // If there are no properties to initialize in the constructor, then we don't need to do anything.
            return new ConstructorMapping(constructorName, false, ImmutableArray<ConstructorArgumentMapping>.Empty);
        }

        var constructors = targetTypeSymbol.Constructors.Where(c => !c.IsImplicitlyDeclared && !c.IsStatic).ToArray();
        if (constructors.Length == 0)
        {
            return new ConstructorMapping(
                constructorName,
                true,
                readonlyProperties.Select(p => new ConstructorArgumentMapping(p.ParameterName, p.Type, p, Location.None)).ToImmutableArray());
        }

        var argumentMappings = GetArgumentMappings(context, constructors, readonlyProperties);
        return argumentMappings.IsValid(context) ? new ConstructorMapping(constructorName, false, argumentMappings) : default;
    }

    private static ImmutableArray<ConstructorArgumentMapping> GetArgumentMappings(MappingContext context, IEnumerable<IMethodSymbol> constructors, PropertyMapping[] properties)
    {
        var (_, _, semanticModel, _, wellKnownTypes, _, _) = context;
        var argumentMappings = constructors
            .Where(c => c.HasAttribute(wellKnownTypes.MapConstructorAttributeTypeSymbol) || c.Parameters.Length == properties.Length)
            .Select(c => GetArgumentMappings(c, semanticModel.Compilation, properties))
            .FirstOrDefault(a => a.Length > 0);

        return argumentMappings;
    }

    private static ImmutableArray<ConstructorArgumentMapping> GetArgumentMappings(IMethodSymbol constructor, Compilation compilation, PropertyMapping[] properties)
    {
        var arguments = ImmutableArray.CreateBuilder<ConstructorArgumentMapping>();
        foreach (var parameter in constructor.Parameters)
        {
            var property = properties.SingleOrDefault(p => p.ParameterName == parameter.Name && compilation.HasCompatibleTypes(parameter.Type, p.Type));
            if (property == default)
            {
                return ImmutableArray<ConstructorArgumentMapping>.Empty;
            }

            arguments.Add(new ConstructorArgumentMapping(parameter.Name, parameter.Type, property, parameter.Locations.FirstOrDefault() ?? Location.None));
        }

        return arguments.ToImmutable();
    }

    private static bool IsValid(this ImmutableArray<ConstructorArgumentMapping> argumentMappings, MappingContext context)
    {
        var targetType = context.TargetTypeSyntax;
        var targetTypeSymbol = context.TargetTypeSymbol;

        if (argumentMappings.IsDefaultOrEmpty)
        {
            context.ReportDiagnostic(DiagnosticsFactory.MissingConstructorOnTargetClassError(targetType.GetLocation(), targetType.Identifier.ValueText));
            return false;
        }

        var selfReferencingParameter = argumentMappings.FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.Type, targetTypeSymbol));
        if (selfReferencingParameter != default)
        {
            context.ReportDiagnostic(DiagnosticsFactory.SelfReferencingConstructorMappingError(selfReferencingParameter.Location, targetType.Identifier.ValueText));
            return false;
        }

        return true;
    }
}