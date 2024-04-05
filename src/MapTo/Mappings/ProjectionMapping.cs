using Humanizer;

namespace MapTo.Mappings;

internal readonly record struct ProjectionMapping(
    Accessibility Accessibility,
    Accessibility OriginalAccessibility,
    string MethodName,
    TypeMapping ReturnType,
    TypeMapping ParameterType,
    string ParameterName,
    bool IsPartial)
{
    internal static ImmutableArray<ProjectionMapping> Create(MappingContext context)
    {
        var customProjectionMethods = FindProjectionMethods(context)
            .Select(m => new ProjectionMapping(
                MethodName: m.Name,
                ReturnType: m.ReturnType.ToTypeMapping(),
                ParameterType: m.Parameters[0].Type.ToTypeMapping(),
                ParameterName: m.Parameters[0].Name,
                OriginalAccessibility: m.DeclaredAccessibility,
                Accessibility: m.DeclaredAccessibility == Accessibility.Public ? Accessibility.Public : Accessibility.Internal,
                IsPartial: true))
            .ToList();

        foreach (var projection in GetProjectionMethods(context))
        {
            if (customProjectionMethods.All(m => m.MethodName != projection.MethodName || m.ParameterType.FullName != projection.ParameterType.FullName))
            {
                customProjectionMethods.Add(projection);
            }
        }

        return customProjectionMethods.ToImmutableArray();
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

    private static IEnumerable<ProjectionMapping> GetProjectionMethods(MappingContext context)
    {
        if (context.CodeGeneratorOptions.ProjectionType is ProjectionType.None)
        {
            return Enumerable.Empty<ProjectionMapping>();
        }

        var methodName = $"{context.CodeGeneratorOptions.MapMethodPrefix}{context.TargetTypeSyntax.Identifier.Text}".Pluralize();
        var accessibility = context.TargetTypeSyntax.GetAccessibility();
        var projectionType = context.CodeGeneratorOptions.ProjectionType;

        return Enum.GetValues(typeof(ProjectionType))
            .Cast<ProjectionType>()
            .Where(p => p is not ProjectionType.None and not ProjectionType.All && projectionType.HasFlag(p))
            .Select(p => new ProjectionMapping(
                Accessibility: accessibility,
                OriginalAccessibility: accessibility,
                MethodName: methodName,
                ReturnType: CreateTypeMapping(context.TargetTypeSymbol, p, context.Compilation),
                ParameterType: CreateTypeMapping(context.SourceTypeSymbol, p, context.Compilation),
                ParameterName: "source",
                IsPartial: false));
    }

    private static TypeMapping CreateTypeMapping(ITypeSymbol typeSymbol, ProjectionType projectionType, Compilation compilation)
    {
        var type = projectionType switch
        {
            ProjectionType.Array => compilation.CreateArrayTypeSymbol(typeSymbol),
            ProjectionType.IEnumerable => compilation.CreateGenericTypeSymbol(SpecialType.System_Collections_Generic_IEnumerable_T, typeSymbol),
            ProjectionType.ICollection => compilation.CreateGenericTypeSymbol(SpecialType.System_Collections_Generic_ICollection_T, typeSymbol),
            ProjectionType.IReadOnlyCollection => compilation.CreateGenericTypeSymbol(SpecialType.System_Collections_Generic_IReadOnlyCollection_T, typeSymbol),
            ProjectionType.IList => compilation.CreateGenericTypeSymbol(SpecialType.System_Collections_Generic_IList_T, typeSymbol),
            ProjectionType.IReadOnlyList => compilation.CreateGenericTypeSymbol(SpecialType.System_Collections_Generic_IReadOnlyList_T, typeSymbol),
            ProjectionType.List => compilation.CreateGenericTypeSymbol(KnownTypes.SystemCollectionsGenericListOfT, typeSymbol),
            ProjectionType.Memory => compilation.CreateGenericTypeSymbol(KnownTypes.SystemMemoryOfT, typeSymbol),
            ProjectionType.ReadOnlyMemory => compilation.CreateGenericTypeSymbol(KnownTypes.SystemReadOnlyMemoryOfT, typeSymbol),
            _ => throw new ArgumentOutOfRangeException(nameof(projectionType), projectionType, $"Invalid projection type '{projectionType}'.")
        };

        return type.WithNullableAnnotation(NullableAnnotation.Annotated).ToTypeMapping();
    }
}