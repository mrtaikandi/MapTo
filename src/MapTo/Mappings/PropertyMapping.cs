using MapTo.Configuration;
using MapTo.Diagnostics;
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
        var (_, targetTypeSymbol, _, _, knownTypes, _, _) = context;
        var isInheritFromMappedBaseClass = context.IsTargetTypeInheritFromMappedBaseClass();

        return targetTypeSymbol
            .GetAllMembers(isInheritFromMappedBaseClass)
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal &&
                        !p.HasAttribute(knownTypes.CompilerGeneratedAttributeTypeSymbol) &&
                        !p.HasAttribute(knownTypes.IgnorePropertyAttributeTypeSymbol))
            .Select(p => CreatePropertyMapping(context, p))
            .WhereNotNull()
            .ToImmutableArray();
    }

    internal static ImmutableArray<PropertyMapping> ExceptConstructorInitializers(this ImmutableArray<PropertyMapping> properties, ConstructorMapping constructor)
    {
        if (!constructor.HasParameters)
        {
            return properties;
        }

        return properties
            .Where(prop => !constructor.Parameters.Any(param => param.IsEqual(prop)))
            .ToImmutableArray();
    }

    private static PropertyMapping? CreatePropertyMapping(MappingContext context, IPropertySymbol property)
    {
        var (_, _, _, sourceTypeSymbol, knownTypes, _, _) = context;
        var mapPropertyAttributeTypeSymbol = knownTypes.MapPropertyAttributeTypeSymbol;
        var sourcePropertyName = property.GetSourcePropertyName(mapPropertyAttributeTypeSymbol);
        var sourceProperty = sourceTypeSymbol.FindProperty(sourcePropertyName);

        if (sourceProperty is null)
        {
            return null;
        }

        if (!property.TryGetTypeConverter(sourceProperty, context, out var converter))
        {
            return null;
        }

        return new(
            Name: property.Name,
            Type: property.GetTypeNamedSymbol(),
            SourceName: sourceProperty.Name,
            SourceType: sourceProperty.GetTypeNamedSymbol(),
            InitializationMode: property.GetInitializationMode(),
            ParameterName: property.Name.ToParameterNameCasing(),
            TypeConverter: converter.Value,
            UsingDirectives: converter.Value.UsingDirectives ?? ImmutableArray<string>.Empty);
    }

    private static IPropertySymbol? FindProperty(this ITypeSymbol typeSymbol, string propertyName) => typeSymbol
        .GetAllMembers()
        .OfType<IPropertySymbol>()
        .SingleOrDefault(p => p.Name == propertyName);

    private static PropertyInitializationMode GetInitializationMode(this IPropertySymbol property) => property.SetMethod switch
    {
        null => PropertyInitializationMode.Constructor,
        { IsInitOnly: true } => PropertyInitializationMode.ObjectInitializer,
        { IsInitOnly: false } when property.Type.IsPrimitiveType(true) => PropertyInitializationMode.ObjectInitializer,
        _ => PropertyInitializationMode.Setter
    };

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
        var mapFromAttribute = propertyType.GetAttribute(context.KnownTypes.MapFromAttributeTypeSymbol);

        if (mapFromAttribute is null && !propertyType.IsPrimitiveType())
        {
            if (propertyType is IArrayTypeSymbol { ElementType: INamedTypeSymbol arrayNamedTypeSymbol })
            {
                enumerableTypeSymbol = arrayNamedTypeSymbol;
                enumerableType = EnumerableType.Array;

                mapFromAttribute = enumerableTypeSymbol.GetAttribute(context.KnownTypes.MapFromAttributeTypeSymbol);
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
                mapFromAttribute = typeSymbol.GetAttribute(context.KnownTypes.MapFromAttributeTypeSymbol);
                propertyTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            }
        }

        var mappedSourcePropertyType = mapFromAttribute?.ConstructorArguments.First().Value as INamedTypeSymbol ?? enumerableTypeSymbol;
        if (mappedSourcePropertyType is null)
        {
            return null;
        }

        var referenceHandling = context.CodeGeneratorOptions.ReferenceHandling switch
        {
            ReferenceHandling.Disabled => false,
            ReferenceHandling.Enabled => true,
            ReferenceHandling.Auto when mappedSourcePropertyType.HasNonPrimitiveProperties() => true,
            _ => false
        };

        var methodName = mappedSourcePropertyType.IsPrimitiveType()
            ? $"{context.CodeGeneratorOptions.MapMethodPrefix}{mappedSourcePropertyType.Name}"
            : $"{context.CodeGeneratorOptions.MapMethodPrefix}{propertyTypeName}";

        var containingType = mappedSourcePropertyType.IsPrimitiveType()
            ? "System"
            : $"{mappedSourcePropertyType.ToDisplayString()}{context.CodeGeneratorOptions.MapExtensionClassSuffix}";

        return new TypeConverterMapping(
            containingType,
            methodName,
            null,
            enumerableTypeSymbol?.Name,
            enumerableType,
            mapFromAttribute is not null,
            referenceHandling,
            ImmutableArray.Create("global::System.Linq"));
    }

    private static TypeConverterMapping? GetPropertyTypeConverter(this IPropertySymbol property, MappingContext context, IPropertySymbol sourceProperty)
    {
        TypedConstant? parameters = null;
        var (error, converterMethodSymbol) = GetPropertyTypeConverterMethod(context, property, sourceProperty);
        if (error is null)
        {
            (error, parameters) = GetPropertyTypeConverterAdditionalParameters(context, property, converterMethodSymbol!);
        }

        if (error is null)
        {
            return new(converterMethodSymbol!, parameters?.IsNull != false ? null : parameters.Value);
        }

        var converter = property.GetNestedObjectMapping(context, out var nestedObjectError);
        if (converter is not null)
        {
            return converter;
        }

        if ((nestedObjectError ??= error) is not null)
        {
            context.ReportDiagnostic(nestedObjectError);
        }

        return null;
    }

    private static (Diagnostic? Error, TypedConstant? Parameters) GetPropertyTypeConverterAdditionalParameters(
        MappingContext context,
        IPropertySymbol property,
        IMethodSymbol converterMethodSymbol)
    {
        var typeConverterAttribute = property.GetAttribute(context.KnownTypes.PropertyTypeConverterAttributeTypeSymbol);
        var additionalParameters = typeConverterAttribute?.NamedArguments.SingleOrDefault(a => a.Key == nameof(PropertyTypeConverterAttribute.Parameters)).Value;

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
        var typeConverterAttribute = property.GetAttribute(context.KnownTypes.PropertyTypeConverterAttributeTypeSymbol);
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

    private static string GetSourcePropertyName(this ISymbol targetProperty, ITypeSymbol propertyAttributeTypeSymbol) => targetProperty
        .GetAttribute(propertyAttributeTypeSymbol)
        .GetNamedArgument(nameof(MapPropertyAttribute.From), defaultValue: targetProperty.Name);

    private static bool IsTargetTypeInheritFromMappedBaseClass(this MappingContext context)
    {
        var (typeSyntax, _, semanticModel, _, knownTypes, _, _) = context;
        return typeSyntax.BaseList is not null && typeSyntax.BaseList.Types
            .Select(t => semanticModel.GetTypeInfo(t.Type).Type)
            .Any(t => t?.GetAttribute(knownTypes.MapFromAttributeTypeSymbol) is not null);
    }

    private static bool TryGetTypeConverter(
        this IPropertySymbol property,
        IPropertySymbol sourceProperty,
        MappingContext context,
        [NotNullWhen(true)] out TypeConverterMapping? converter)
    {
        converter = default(TypeConverterMapping);
        if (property.Type.IsArray() || !context.Compilation.HasCompatibleTypes(sourceProperty, property))
        {
            converter = property.GetPropertyTypeConverter(context, sourceProperty);
            if (converter is null)
            {
                return false;
            }
        }

        return true;
    }
}