using MapTo.Extensions;

namespace MapTo.Mappings;

internal readonly record struct PropertyMapping(
    string Name,
    INamedTypeSymbol Type,
    string SourceName,
    INamedTypeSymbol SourceType,
    PropertyInitializationMode InitializationMode,
    string ParameterName,
    TypeConverterMapping TypeConverter,
    ImmutableArray<string> UsingDirectives)
{
    public string TypeName => Type.ToDisplayString();

    public bool HasTypeConverter => TypeConverter != default;
}

internal static class PropertyMappingFactory
{
    internal static PropertyMapping? Create(MappingContext context, IPropertySymbol property, INamedTypeSymbol sourceTypeSymbol)
    {
        var mapPropertyAttributeTypeSymbol = context.WellKnownTypes.MapPropertyAttributeTypeSymbol;
        var sourcePropertyName = property.GetSourcePropertyName(mapPropertyAttributeTypeSymbol);
        var sourceProperty = sourceTypeSymbol.FindProperty(sourcePropertyName);

        if (sourceProperty is null)
        {
            return null;
        }

        TypeConverterMapping? converter = null;
        var globalUsings = ImmutableArray<string>.Empty;

        if (!context.Compilation.HasCompatibleTypes(sourceProperty, property))
        {
            converter = property.GetPropertyTypeConverter(context, sourceProperty, out var error);
            if (converter == null)
            {
                converter = property.GetNestedObjectMapping(context);
                globalUsings = globalUsings.Add("global::System.Linq");
            }

            if (converter is null && error is not null)
            {
                context.ReportDiagnostic(error);
                return null;
            }
        }

        return new(
            property.Name,
            (INamedTypeSymbol)property.Type,
            sourceProperty.Name,
            (INamedTypeSymbol)sourceProperty.Type,
            property.SetMethod is null ? PropertyInitializationMode.Constructor : PropertyInitializationMode.ObjectInitializer,
            property.Name.ToParameterNameCasing(),
            converter ?? default,
            globalUsings);
    }

    private static TypeConverterMapping? GetNestedObjectMapping(this ISymbol property, MappingContext context)
    {
        if (!property.TryGetTypeSymbol(out var propertyType))
        {
            return default;
        }

        var propertyTypeName = propertyType.Name;
        INamedTypeSymbol? enumerableTypeSymbol = null;
        var mapFromAttribute = propertyType.GetAttribute(context.WellKnownTypes.MapFromAttributeTypeSymbol);

        if (mapFromAttribute is null &&
            propertyType is INamedTypeSymbol namedTypeSymbol &&
            !propertyType.IsPrimitiveType() &&
            (context.Compilation.IsGenericEnumerable(propertyType) || propertyType.AllInterfaces.Any(i => context.Compilation.IsGenericEnumerable(i))))
        {
            enumerableTypeSymbol = namedTypeSymbol;

            var typeSymbol = namedTypeSymbol.TypeArguments.First();
            mapFromAttribute = typeSymbol.GetAttribute(context.WellKnownTypes.MapFromAttributeTypeSymbol);
            propertyTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        }

        if (mapFromAttribute?.ConstructorArguments.First().Value is not INamedTypeSymbol mappedSourcePropertyType)
        {
            return null;
        }

        return enumerableTypeSymbol is null
            ? new TypeConverterMapping(
                $"{mappedSourcePropertyType.ToDisplayString()}{context.CodeGeneratorOptions.MapExtensionClassSuffix}",
                $"{context.CodeGeneratorOptions.MapMethodPrefix}{propertyTypeName}",
                null)
            : new TypeConverterMapping(
                ContainingType: $"{mappedSourcePropertyType.ToDisplayString()}{context.CodeGeneratorOptions.MapExtensionClassSuffix}",
                MethodName: $"{context.CodeGeneratorOptions.MapMethodPrefix}{propertyTypeName}",
                Parameter: null,
                EnumerableTypeName: enumerableTypeSymbol.Name);
    }

    private static TypeConverterMapping? GetPropertyTypeConverter(
        this IPropertySymbol property,
        MappingContext context,
        IPropertySymbol sourceProperty,
        out Diagnostic? error)
    {
        TypedConstant? parameters = null;
        (error, var converterMethodSymbol) = GetPropertyTypeConverterMethod(context, property, sourceProperty);
        if (error is null)
        {
            (error, parameters) = GetPropertyTypeConverterAdditionalParameters(context, property, converterMethodSymbol!);
        }

        return error is null
            ? new(converterMethodSymbol!, parameters?.IsNull != false ? null : parameters.Value)
            : null;
    }

    private static (Diagnostic? Error, TypedConstant? Parameters) GetPropertyTypeConverterAdditionalParameters(
        MappingContext context,
        IPropertySymbol property,
        IMethodSymbol converterMethodSymbol)
    {
        var typeConverterAttribute = property.GetAttribute(context.WellKnownTypes.PropertyTypeConverterAttributeTypeSymbol);
        var additionalParameters = typeConverterAttribute?.NamedArguments.SingleOrDefault(a => a.Key == WellKnownTypes.PropertyTypeConverterAttributeAdditionalParameters).Value;

        if (additionalParameters?.IsNull != false)
        {
            return (null, null);
        }

        return converterMethodSymbol.Parameters.Length switch
        {
            1 => (DiagnosticsFactory.PropertyTypeConverterMethodAdditionalParametersIsMissingWarning(property, converterMethodSymbol), null),
            2 when !converterMethodSymbol.Parameters[1].Type.IsArrayOf(SpecialType.System_Object) =>
                (DiagnosticsFactory.PropertyTypeConverterMethodAdditionalParametersTypeCompatibilityError(converterMethodSymbol), null),
            _ => (null, additionalParameters)
        };
    }

    private static (Diagnostic? Error, IMethodSymbol? ConverterMethodSymbol) GetPropertyTypeConverterMethod(
        MappingContext context,
        IPropertySymbol property,
        IPropertySymbol sourceProperty)
    {
        var typeConverterAttribute = property.GetAttribute(context.WellKnownTypes.PropertyTypeConverterAttributeTypeSymbol);
        var firstArgument = typeConverterAttribute?.ConstructorArguments.First();

        if (firstArgument?.Value is not string converterMethodName)
        {
            return (DiagnosticsFactory.PropertyTypeConverterRequiredError(property), null);
        }

        var converterMethodSymbol = property.ContainingType.GetMembers(converterMethodName).OfType<IMethodSymbol>().SingleOrDefault();
        if (converterMethodSymbol is null)
        {
            return (DiagnosticsFactory.PropertyTypeConverterMethodNotFoundInTargetClassError(property, typeConverterAttribute!), null);
        }

        if (!converterMethodSymbol.IsStatic)
        {
            return (DiagnosticsFactory.PropertyTypeConverterMethodIsNotStaticError(converterMethodSymbol), null);
        }

        if (!context.Compilation.HasCompatibleTypes(converterMethodSymbol.ReturnType, property))
        {
            return (DiagnosticsFactory.PropertyTypeConverterMethodReturnTypeCompatibilityError(property, converterMethodSymbol), null);
        }

        if (!context.Compilation.HasCompatibleTypes(converterMethodSymbol.Parameters.First().Type, sourceProperty))
        {
            return (DiagnosticsFactory.PropertyTypeConverterMethodInputTypeCompatibilityError(sourceProperty, converterMethodSymbol), null);
        }

        if (sourceProperty.NullableAnnotation is NullableAnnotation.Annotated && converterMethodSymbol.Parameters.First().NullableAnnotation is NullableAnnotation.NotAnnotated)
        {
            return (DiagnosticsFactory.PropertyTypeConverterMethodInputTypeNullCompatibilityError(sourceProperty, converterMethodSymbol), null);
        }

        return (null, converterMethodSymbol);
    }

    private static IPropertySymbol? FindProperty(this ITypeSymbol typeSymbol, string propertyName) => typeSymbol
        .GetAllMembers()
        .OfType<IPropertySymbol>()
        .SingleOrDefault(p => p.Name == propertyName);

    private static string GetSourcePropertyName(this ISymbol targetProperty, ITypeSymbol propertyAttributeTypeSymbol) => targetProperty
        .GetAttribute(propertyAttributeTypeSymbol)
        ?.NamedArguments
        .SingleOrDefault(a => a.Key == WellKnownTypes.MapPropertyAttributeSourcePropertyName)
        .Value.Value as string ?? targetProperty.Name;
}