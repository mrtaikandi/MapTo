using MapTo.Extensions;

namespace MapTo.Mappings;

internal readonly record struct ConstructorMapping(string Name, bool IsGenerated, ImmutableArray<ConstructorParameterMapping> Parameters)
{
    public bool HasParameters => !Parameters.IsDefaultOrEmpty;
}

internal static class ConstructorMappingFactory
{
    public static ConstructorMapping Create(MappingContext context, ImmutableArray<PropertyMapping> properties)
    {
        var (targetType, targetTypeSymbol, _, _, knownTypes, _, _) = context;
        var constructorName = targetType.Identifier.ValueText;

        var constructor = targetTypeSymbol.Constructors.SingleOrDefault(c => c.HasAttribute(knownTypes.MapConstructorAttributeTypeSymbol));

        if (constructor == null)
        {
            // Checking if there is a constructor that has matching parameters with the properties
            var constructors = targetTypeSymbol.Constructors
                .Where(c => c is { IsStatic: false, Parameters.Length: > 0 })
                .OrderByDescending(c => c.Parameters.Length)
                .ToArray();

            constructor = constructors.FirstOrDefault(c => c.Parameters.All(param => properties.Any(param.IsEqual))) ?? constructors.FirstOrDefault();
        }

        if (constructor is null)
        {
            var constructorProperties = properties.Where(p => p.InitializationMode == PropertyInitializationMode.Constructor).ToArray();
            return new ConstructorMapping(
                constructorName,
                constructorProperties.Length > 0,
                constructorProperties.Select(p => new ConstructorParameterMapping(p.ParameterName, p.Type, p, Location.None)).ToImmutableArray());
        }

        var argumentMappings = constructor.GetArgumentMappings(properties);
        return argumentMappings.IsValid(context) ? new ConstructorMapping(constructorName, false, argumentMappings) : default;
    }

    internal static bool IsEqual(this IParameterSymbol parameter, PropertyMapping property) =>
        parameter.Name.ToParameterNameCasing() == property.ParameterName && SymbolEqualityComparer.Default.Equals(parameter.Type, property.Type);

    internal static bool IsEqual(this ConstructorParameterMapping parameter, PropertyMapping property) =>
        parameter.Name.ToParameterNameCasing() == property.ParameterName && SymbolEqualityComparer.Default.Equals(parameter.Type, property.Type);

    private static ImmutableArray<ConstructorParameterMapping> GetArgumentMappings(this IMethodSymbol constructor, ImmutableArray<PropertyMapping> properties)
    {
        var arguments = ImmutableArray.CreateBuilder<ConstructorParameterMapping>();
        foreach (var parameter in constructor.Parameters)
        {
            var property = properties.SingleOrDefault(prop => parameter.IsEqual(prop));
            if (property == default)
            {
                return ImmutableArray<ConstructorParameterMapping>.Empty;
            }

            arguments.Add(new ConstructorParameterMapping(parameter.Name, parameter.Type, property, parameter.Locations.FirstOrDefault() ?? Location.None));
        }

        return arguments.ToImmutable();
    }

    private static bool IsValid(this ImmutableArray<ConstructorParameterMapping> argumentMappings, MappingContext context)
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