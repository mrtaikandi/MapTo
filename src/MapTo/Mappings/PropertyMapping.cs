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
    internal static ImmutableArray<PropertyMapping> Create(MappingContext context)
    {
        var (_, targetTypeSymbol, _, _, wellKnownTypes, _, _) = context;
        var isInheritFromMappedBaseClass = context.IsTargetTypeInheritFromMappedBaseClass();
        var constructorParameters = targetTypeSymbol.GetConstructorParameters(wellKnownTypes);

        return targetTypeSymbol
            .GetAllMembers(isInheritFromMappedBaseClass)
            .OfType<IPropertySymbol>()
            .Where(p => !p.HasAttribute(wellKnownTypes.IgnorePropertyAttributeTypeSymbol))
            .Select(p => CreatePropertyMapping(context, p, constructorParameters))
            .Where(p => p is not null)
            .Select(p => p!.Value)
            .ToImmutableArray();
    }

    private static PropertyMapping? CreatePropertyMapping(MappingContext context, IPropertySymbol property, IParameterSymbol[] constructorParameters)
    {
        var (_, _, _, sourceTypeSymbol, wellKnownTypes, _, _) = context;
        var mapPropertyAttributeTypeSymbol = wellKnownTypes.MapPropertyAttributeTypeSymbol;
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
                converter = property.GetNestedObjectMapping(context, out error);
                globalUsings = globalUsings.Add("global::System.Linq");
            }

            if (converter is null && error is not null)
            {
                context.ReportDiagnostic(error);
                return null;
            }
        }

        return new(
            Name: property.Name,
            Type: property.GetTypeNamedSymbol(),
            SourceName: sourceProperty.Name,
            SourceType: sourceProperty.GetTypeNamedSymbol(),
            InitializationMode: property.GetInitializationMode(context.Compilation, constructorParameters),
            ParameterName: property.Name.ToParameterNameCasing(),
            TypeConverter: converter ?? default,
            UsingDirectives: globalUsings);
    }

    private static PropertyInitializationMode GetInitializationMode(this IPropertySymbol property, Compilation compilation, IParameterSymbol[] constructorParameters)
    {
        var propertyName = property.Name.ToParameterNameCasing();
        if (constructorParameters.Any(p => p.Name == propertyName && compilation.HasCompatibleTypes(p.Type, property.Type)))
        {
            return PropertyInitializationMode.Constructor;
        }

        if (property.SetMethod == null)
        {
            return PropertyInitializationMode.Constructor;
        }

        if (property.SetMethod is { IsInitOnly: true } || property.Type.IsPrimitiveType())
        {
            return PropertyInitializationMode.ObjectInitializer;
        }

        return PropertyInitializationMode.Setter;
    }

    private static IParameterSymbol[] GetConstructorParameters(this INamedTypeSymbol typeSymbol, WellKnownTypes wellKnownTypes)
    {
        return typeSymbol.Constructors
            .Where(c => !c.IsImplicitlyDeclared && !c.IsStatic)
            .Where(c => c.HasAttribute(wellKnownTypes.MapConstructorAttributeTypeSymbol) || c.Parameters.Length > 0)
            .SelectMany(c => c.Parameters)
            .ToArray();
    }

    private static bool IsTargetTypeInheritFromMappedBaseClass(this MappingContext context)
    {
        var (typeSyntax, _, semanticModel, _, wellKnownTypes, _, _) = context;
        return typeSyntax.BaseList is not null && typeSyntax.BaseList.Types
            .Select(t => semanticModel.GetTypeInfo(t.Type).Type)
            .Any(t => t?.GetAttribute(wellKnownTypes.MapFromAttributeTypeSymbol) is not null);
    }

    private static TypeConverterMapping? GetNestedObjectMapping(this IPropertySymbol property, MappingContext context, out Diagnostic? error)
    {
        error = null;

        if (!property.TryGetTypeSymbol(out var propertyType))
        {
            return default;
        }

        var propertyTypeName = propertyType.Name;
        var enumerableType = EnumerableType.None;
        INamedTypeSymbol? enumerableTypeSymbol = null;
        var mapFromAttribute = propertyType.GetAttribute(context.WellKnownTypes.MapFromAttributeTypeSymbol);

        if (mapFromAttribute is null && !propertyType.IsPrimitiveType())
        {
            if (propertyType is IArrayTypeSymbol { ElementType: INamedTypeSymbol arrayNamedTypeSymbol })
            {
                enumerableTypeSymbol = arrayNamedTypeSymbol;
                enumerableType = EnumerableType.Array;

                mapFromAttribute = enumerableTypeSymbol.GetAttribute(context.WellKnownTypes.MapFromAttributeTypeSymbol);
                propertyTypeName = enumerableTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            }
            else
            {
                enumerableTypeSymbol = propertyType as INamedTypeSymbol;
                enumerableType = EnumerableType.List;

                if (enumerableTypeSymbol is null || enumerableTypeSymbol.TypeArguments.IsEmpty)
                {
                    error = DiagnosticsFactory.SuitableMappingTypeInNestedPropertyNotFoundError(property, propertyType);
                    return null;
                }

                var typeSymbol = enumerableTypeSymbol.TypeArguments.First();
                mapFromAttribute = typeSymbol.GetAttribute(context.WellKnownTypes.MapFromAttributeTypeSymbol);
                propertyTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            }
        }

        if (mapFromAttribute?.ConstructorArguments.First().Value is not INamedTypeSymbol mappedSourcePropertyType)
        {
            return null;
        }

        return new TypeConverterMapping(
            ContainingType: $"{mappedSourcePropertyType.ToDisplayString()}{context.CodeGeneratorOptions.MapExtensionClassSuffix}",
            MethodName: $"{context.CodeGeneratorOptions.MapMethodPrefix}{propertyTypeName}",
            Parameter: null,
            EnumerableElementTypeName: enumerableTypeSymbol?.Name,
            EnumerableType: enumerableType,
            IsMapToExtensionMethod: true,
            UsingReferenceHandler: mappedSourcePropertyType.HasNonPrimitiveProperties());
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