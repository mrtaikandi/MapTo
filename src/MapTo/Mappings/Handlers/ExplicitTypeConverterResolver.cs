namespace MapTo.Mappings.Handlers;

internal class ExplicitTypeConverterResolver : ITypeConverterResolver
{
    /// <inheritdoc />
    public ResolverResult<TypeConverterMapping> Get(MappingContext context, IPropertySymbol property, SourceProperty sourceProperty)
    {
        var converterMethodSymbolResult = GetTypeConverterMethod(context, property, sourceProperty);
        if (converterMethodSymbolResult.IsFailure)
        {
            if (!context.Compilation.IsNonGenericEnumerable(property.Type) && context.Compilation.IsNonGenericEnumerable(sourceProperty.TypeSymbol))
            {
                return converterMethodSymbolResult.Error;
            }

            return ResolverResult.Undetermined<TypeConverterMapping>(converterMethodSymbolResult.Error);
        }

        var additionalParametersResult = GetAdditionalParameters(context, property, converterMethodSymbolResult.Value);
        return additionalParametersResult.IsFailure
            ? ResolverResult.Undetermined<TypeConverterMapping>(additionalParametersResult.Error)
            : new TypeConverterMapping(converterMethodSymbolResult.Value, additionalParametersResult.Value);
    }

    private static ResolverResult<TypedConstant> GetAdditionalParameters(MappingContext context, IPropertySymbol property, IMethodSymbol converterMethodSymbol)
    {
        var typeConverterAttribute = property.GetAttribute(context.KnownTypes.PropertyTypeConverterAttributeTypeSymbol);
        var additionalParameters = typeConverterAttribute?.NamedArguments.SingleOrDefault(a => a.Key == nameof(PropertyTypeConverterAttribute.Parameters)).Value;

        if (additionalParameters is null || additionalParameters.Value.IsNull)
        {
            return ResolverResult.Undetermined<TypedConstant>();
        }

        return converterMethodSymbol.Parameters.Length switch
        {
            1 => DiagnosticsFactory.PropertyTypeConverterMethodAdditionalParametersIsMissingWarning(property, converterMethodSymbol),
            2 when !converterMethodSymbol.Parameters[1].Type.IsArrayOf(SpecialType.System_Object) =>
                DiagnosticsFactory.PropertyTypeConverterMethodAdditionalParametersTypeCompatibilityError(converterMethodSymbol),
            _ => additionalParameters.Value
        };
    }

    private static ResolverResult<IMethodSymbol> GetTypeConverterMethod(MappingContext context, IPropertySymbol property, SourceProperty sourceProperty)
    {
        var typeConverterAttribute = property.GetAttribute(context.KnownTypes.PropertyTypeConverterAttributeTypeSymbol);
        var firstArgument = typeConverterAttribute?.ConstructorArguments.First();

        if (firstArgument?.Value is not string converterMethodName)
        {
            return DiagnosticsFactory.PropertyTypeConverterRequiredError(property);
        }

        var converterMethodSymbol = property.ContainingType.GetMembers(converterMethodName).OfType<IMethodSymbol>().SingleOrDefault();
        if (converterMethodSymbol is null)
        {
            return DiagnosticsFactory.PropertyTypeConverterMethodNotFoundInTargetClassError(property, typeConverterAttribute!);
        }

        if (!converterMethodSymbol.IsStatic)
        {
            return DiagnosticsFactory.PropertyTypeConverterMethodIsNotStaticError(converterMethodSymbol);
        }

        if (!context.Compilation.HasCompatibleTypes(converterMethodSymbol.ReturnType, property))
        {
            return DiagnosticsFactory.PropertyTypeConverterMethodReturnTypeCompatibilityError(property, converterMethodSymbol);
        }

        if (!context.Compilation.HasCompatibleTypes(converterMethodSymbol.Parameters.First().Type, sourceProperty.TypeSymbol))
        {
            return DiagnosticsFactory.PropertyTypeConverterMethodInputTypeCompatibilityError(sourceProperty.Name, sourceProperty.TypeSymbol, converterMethodSymbol);
        }

        if (sourceProperty.Type.NullableAnnotation is NullableAnnotation.Annotated &&
            converterMethodSymbol.Parameters.First().NullableAnnotation is NullableAnnotation.NotAnnotated)
        {
            return DiagnosticsFactory.PropertyTypeConverterMethodInputTypeNullCompatibilityError(sourceProperty.Name, converterMethodSymbol);
        }

        return ResolverResult.Success(converterMethodSymbol);
    }
}