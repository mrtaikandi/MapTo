using System.Diagnostics;
using System.Text;
using MapTo.Extensions;
using MapTo.Mappings.Handlers;

namespace MapTo.Mappings;

[DebuggerDisplay($"{{{nameof(Name)}}} ({{{nameof(TypeName)}}})")]
internal readonly record struct PropertyMapping(
    string Name,
    TypeMapping Type,
    string SourceName,
    TypeMapping SourceType,
    PropertyInitializationMode InitializationMode,
    string ParameterName,
    bool IsRequired,
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
                        (!p.HasAttribute(knownTypes.IgnorePropertyAttributeTypeSymbol) || p.IsRequired))
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

        if (!TypeConverterResolver.TryGet(context, property, sourceProperty, out var converter))
        {
            return null;
        }

        return new(
            Name: property.Name,
            Type: property.Type.ToTypeMapping(),
            SourceName: sourceProperty.Name,
            SourceType: sourceProperty.Type,
            InitializationMode: property.GetInitializationMode(context.KnownTypes),
            ParameterName: property.Name.ToParameterNameCasing(),
            IsRequired: property.IsRequired,
            TypeConverter: converter,
            UsingDirectives: converter.UsingDirectives ?? ImmutableArray<string>.Empty,
            NullHandling: mapPropertyAttribute.GetNamedArgument(nameof(MapPropertyAttribute.NullHandling), context.CodeGeneratorOptions.NullHandling));
    }

    private static SourceProperty FindSource(
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

        return new SourceProperty(
            TypeSymbol: sourceType,
            Type: sourceType.ToTypeMapping(nullableAnnotation),
            Name: sourcePropertyName.ToString());
    }

    private static PropertyInitializationMode GetInitializationMode(this IPropertySymbol property, KnownTypes knownTypes)
    {
        if (property.HasAttribute(knownTypes.IgnorePropertyAttributeTypeSymbol))
        {
            return PropertyInitializationMode.None;
        }

        return property.SetMethod switch
        {
            null => PropertyInitializationMode.Constructor,
            { IsInitOnly: true } => PropertyInitializationMode.ObjectInitializer,
            { IsInitOnly: false } when property.Type.IsPrimitiveType(true) => PropertyInitializationMode.ObjectInitializer,
            _ => PropertyInitializationMode.Setter
        };
    }

    private static bool IsTargetTypeInheritFromMappedBaseClass(this MappingContext context)
    {
        var (typeSyntax, _, semanticModel, _, knownTypes, _, _) = context;
        return typeSyntax.BaseList is not null && typeSyntax.BaseList.Types
            .Select(t => semanticModel.GetTypeInfo(t.Type).Type)
            .Any(t => t?.GetAttribute(knownTypes.MapFromAttributeTypeSymbol) is not null);
    }
}