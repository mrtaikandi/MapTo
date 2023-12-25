namespace MapTo.Mappings;

internal readonly record struct ProjectionMapping(
    Accessibility Accessibility,
    Accessibility OriginalAccessibility,
    string MethodName,
    TypeMapping ReturnType,
    TypeMapping ParameterType,
    string ParameterName)
{
    internal static ImmutableArray<ProjectionMapping> Create(MappingContext context)
    {
        return FindProjectionMethods(context)
            .Select(m => new ProjectionMapping(
                MethodName: m.Name,
                ReturnType: m.ReturnType.ToTypeMapping(),
                ParameterType: m.Parameters[0].Type.ToTypeMapping(),
                ParameterName: m.Parameters[0].Name,
                OriginalAccessibility: m.DeclaredAccessibility,
                Accessibility: m.DeclaredAccessibility == Accessibility.Public ? Accessibility.Public : Accessibility.Internal))
            .ToImmutableArray();
    }

    private static IEnumerable<IMethodSymbol> FindProjectionMethods(MappingContext context)
    {
        var targetTypeSymbol = context.TargetTypeSymbol;
        var sourceTypeSymbol = context.SourceTypeSymbol;
        var compilation = context.Compilation;

        return targetTypeSymbol.GetMembers().OfType<IMethodSymbol>().Where(m =>
            m is { IsStatic: true, IsPartialDefinition: true, DeclaredAccessibility: Accessibility.Private or Accessibility.Internal or Accessibility.Public } &&
            compilation.IsEnumerableType(m.ReturnType, out var typeArguments) && typeArguments.Length == 1 &&
            SymbolEqualityComparer.Default.Equals(typeArguments[0], targetTypeSymbol) &&
            m.Parameters.Length == 1 && compilation.IsEnumerableType(m.Parameters[0].Type, out var parameterTypeArguments) &&
            parameterTypeArguments.Length == 1 && SymbolEqualityComparer.Default.Equals(parameterTypeArguments[0], sourceTypeSymbol));
    }
}