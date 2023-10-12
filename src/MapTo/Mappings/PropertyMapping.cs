using System.Diagnostics;
using System.Text;
using MapTo.Configuration;
using MapTo.Diagnostics;
using MapTo.Extensions;

namespace MapTo.Mappings;

[DebuggerDisplay($"{{{nameof(Name)}}} ({{{nameof(TypeName)}}})")]
internal readonly record struct PropertyMapping(
    string Name,
    TypeMapping Type,
    string SourceName,
    TypeMapping SourceType,
    PropertyInitializationMode InitializationMode,
    string ParameterName,
    TypeConverterMapping TypeConverter,
    ImmutableArray<string> UsingDirectives,
    NullHandling NullHandling)
{
    public string TypeName => Type.FullName;

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
        var mapPropertyAttribute = property.GetAttribute(knownTypes.MapPropertyAttributeTypeSymbol);
        var sourceProperty = property.FindSource(sourceTypeSymbol, mapPropertyAttribute);

        if (sourceProperty.NotFound)
        {
            return null;
        }

        if (!property.TryGetTypeConverter(sourceProperty, context, out var converter))
        {
            return null;
        }

        return new(
            Name: property.Name,
            Type: property.GetTypeNamedSymbol().ToTypeMapping(),
            SourceName: sourceProperty.Name,
            SourceType: sourceProperty.Type,
            InitializationMode: property.GetInitializationMode(),
            ParameterName: property.Name.ToParameterNameCasing(),
            TypeConverter: converter.Value,
            UsingDirectives: converter.Value.UsingDirectives ?? ImmutableArray<string>.Empty,
            NullHandling: mapPropertyAttribute.GetNamedArgument(nameof(MapPropertyAttribute.NullHandling), context.CodeGeneratorOptions.NullHandling));
    }

    private static FoundedSource FindSource(
        this IPropertySymbol property,
        ITypeSymbol sourceTypeSymbol,
        AttributeData? mapPropertyAttribute)
    {
        var propertyName = mapPropertyAttribute.GetNamedArgumentStringValue(nameof(MapPropertyAttribute.From)) ?? property.Name;
        var propertySegments = propertyName.Split('.');

        var nullableAnnotation = NullableAnnotation.None;
        var sourcePropertyName = new StringBuilder();
        var sourceType = sourceTypeSymbol;

        // No need to include the source type name if nameof is used.
        var i = propertySegments.Length > 1 && propertySegments[0] == sourceTypeSymbol.Name ? 1 : 0;

        for (; i < propertySegments.Length; i++)
        {
            var name = propertySegments[i];

            var sourceMember = sourceTypeSymbol.GetAllMembers().SingleOrDefault(p => p.Name == name);
            (sourceType, var sourceAnnotation) = sourceMember switch
            {
                IPropertySymbol p => (p.Type, p.NullableAnnotation),
                IMethodSymbol m => (m.ReturnType, m.ReturnType.NullableAnnotation),
                IFieldSymbol f => (f.Type, f.NullableAnnotation),
                _ => (null, default)
            };

            if (sourceType is null)
            {
                return default;
            }

            sourcePropertyName.Append(name);
            sourceTypeSymbol = sourceType;
            nullableAnnotation = nullableAnnotation is NullableAnnotation.Annotated ? nullableAnnotation : sourceAnnotation;

            if (i < propertySegments.Length - 1)
            {
                if (sourceAnnotation is NullableAnnotation.Annotated)
                {
                    sourcePropertyName.Append('?');
                }

                sourcePropertyName.Append('.');
            }
            else if (sourceMember is IMethodSymbol)
            {
                sourcePropertyName.Append("()");
            }
        }

        return new FoundedSource(
            TypeSymbol: sourceType,
            Type: sourceType.ToTypeMapping(nullableAnnotation),
            Name: sourcePropertyName.ToString());
    }

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

        var propertyType = property.Type;
        var propertyTypeName = propertyType.Name;
        INamedTypeSymbol? enumerableTypeSymbol = null;
        var mapFromAttribute = propertyType.GetAttribute(context.KnownTypes.MapFromAttributeTypeSymbol);

        if (mapFromAttribute is null && !propertyType.IsPrimitiveType())
        {
            if (propertyType is IArrayTypeSymbol { ElementType: INamedTypeSymbol arrayNamedTypeSymbol })
            {
                enumerableTypeSymbol = arrayNamedTypeSymbol;
                mapFromAttribute = enumerableTypeSymbol.GetAttribute(context.KnownTypes.MapFromAttributeTypeSymbol);
                propertyTypeName = enumerableTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            }
            else
            {
                enumerableTypeSymbol = propertyType as INamedTypeSymbol;
                if (enumerableTypeSymbol is null || enumerableTypeSymbol.TypeArguments.IsEmpty)
                {
                    error = DiagnosticsFactory.SuitableMappingTypeInNestedPropertyNotFoundError(property, propertyType);
                    return null;
                }

                var typeSymbol = enumerableTypeSymbol.TypeArguments.First();
                mapFromAttribute = typeSymbol.GetAttribute(context.KnownTypes.MapFromAttributeTypeSymbol);
                propertyTypeName = typeSymbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
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
            : $"{mappedSourcePropertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}{context.CodeGeneratorOptions.MapExtensionClassSuffix}";

        return new TypeConverterMapping(
            ContainingType: containingType,
            MethodName: methodName,
            Parameter: null,
            Type: property.Type.ToTypeMapping(),
            IsMapToExtensionMethod: mapFromAttribute is not null,
            ReferenceHandling: referenceHandling,
            UsingDirectives: ImmutableArray.Create("global::System.Linq"));
    }

    private static TypeConverterMapping? GetPropertyTypeConverter(this IPropertySymbol property, MappingContext context, FoundedSource sourceProperty)
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
        FoundedSource sourceProperty)
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

        if (!context.Compilation.HasCompatibleTypes(converterMethodSymbol.Parameters.First().Type, sourceProperty.TypeSymbol))
        {
            return (DiagnosticsFactory.PropertyTypeConverterMethodInputTypeCompatibilityError(sourceProperty.Name, sourceProperty.TypeSymbol, converterMethodSymbol), null);
        }

        if (sourceProperty.Type.NullableAnnotation is NullableAnnotation.Annotated &&
            converterMethodSymbol.Parameters.First().NullableAnnotation is NullableAnnotation.NotAnnotated)
        {
            return (DiagnosticsFactory.PropertyTypeConverterMethodInputTypeNullCompatibilityError(sourceProperty.Name, converterMethodSymbol), null);
        }

        return (null, converterMethodSymbol);
    }

    private static bool IsTargetTypeInheritFromMappedBaseClass(this MappingContext context)
    {
        var (typeSyntax, _, semanticModel, _, knownTypes, _, _) = context;
        return typeSyntax.BaseList is not null && typeSyntax.BaseList.Types
            .Select(t => semanticModel.GetTypeInfo(t.Type).Type)
            .Any(t => t?.GetAttribute(knownTypes.MapFromAttributeTypeSymbol) is not null);
    }

    private static bool TryGetTypeConverter(
        this IPropertySymbol property,
        FoundedSource sourceProperty,
        MappingContext context,
        [NotNullWhen(true)] out TypeConverterMapping? converter)
    {
        converter = default(TypeConverterMapping);

        if (!property.Type.IsArray() && context.Compilation.HasCompatibleTypes(sourceProperty.TypeSymbol, property))
        {
            return true;
        }

        if (!context.Compilation.IsNonGenericEnumerable(property.Type) && context.Compilation.IsNonGenericEnumerable(sourceProperty.TypeSymbol))
        {
            context.ReportDiagnostic(DiagnosticsFactory.PropertyTypeConverterRequiredError(property));
            return false;
        }

        converter = property.GetPropertyTypeConverter(context, sourceProperty);
        return converter is not null;
    }

    private readonly record struct FoundedSource(ITypeSymbol TypeSymbol, TypeMapping Type, string Name)
    {
        internal bool NotFound => string.IsNullOrWhiteSpace(Name);
    }
}