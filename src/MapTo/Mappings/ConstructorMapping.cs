namespace MapTo.Mappings;

internal readonly record struct ConstructorMapping(string Name, bool IsGenerated, ImmutableArray<ConstructorArgumentMapping> Arguments)
{
    public bool HasArguments => Arguments.Length > 0;

    public static ConstructorMapping Create(MappingContext context, ImmutableArray<PropertyMapping> properties)
    {
        var (targetType, typeSymbol, _, _, _, _, _) = context;
        var constructorName = targetType.Identifier.ValueText;

        var readonlyProperties = properties.Where(p => p.InitializationMode == PropertyInitializationMode.Constructor).ToArray();
        if (readonlyProperties.Length == 0)
        {
            // If there are no properties to initialize in the constructor, then we don't need to do anything.
            return new ConstructorMapping(constructorName, false, ImmutableArray<ConstructorArgumentMapping>.Empty);
        }

        var constructors = typeSymbol.Constructors.Where(c => !c.IsImplicitlyDeclared && !c.IsStatic).ToArray();
        if (constructors.Length == 0)
        {
            return new ConstructorMapping(
                constructorName,
                true,
                readonlyProperties.Select(p => new ConstructorArgumentMapping(p.ParameterName, p.Type, p)).ToImmutableArray());
        }

        var argumentMappings = GetArgumentMappings(context, constructors, readonlyProperties);
        if (argumentMappings.IsDefaultOrEmpty)
        {
            context.ReportDiagnostic(DiagnosticsFactory.MissingConstructorOnTargetClassError(targetType.GetLocation(), targetType.Identifier.ValueText));
            return default;
        }

        return new ConstructorMapping(constructorName, false, argumentMappings);
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

            arguments.Add(new ConstructorArgumentMapping(parameter.Name, parameter.Type, property));
        }

        return arguments.ToImmutable();
    }
}