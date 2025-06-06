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
    NullHandling NullHandling)
{
    public string TypeName => Type.FullName;

    public bool HasTypeConverter => TypeConverter != default;

    public bool Equals(PropertyMapping other) =>
        Name == other.Name &&
        Type.Equals(other.Type) &&
        SourceName == other.SourceName &&
        SourceType.Equals(other.SourceType) &&
        InitializationMode == other.InitializationMode &&
        ParameterName == other.ParameterName &&
        IsRequired == other.IsRequired;

    public override int GetHashCode() => HashCode.Combine(Name, Type, SourceName, SourceType, InitializationMode, ParameterName, IsRequired);
}

internal static class PropertyMappingFactory
{
    internal static ImmutableArray<PropertyMapping> Create(MappingContext context)
    {
        var (_, targetTypeSymbol, _, _, knownTypes, _, _) = context;

        return targetTypeSymbol
            .GetAllMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal &&
                        !p.HasAttribute(knownTypes.CompilerGeneratedAttributeTypeSymbol) &&
                        (!p.HasAttribute(knownTypes.IgnorePropertyAttributeTypeSymbol) || p.IsRequired) &&
                        (!context.Configuration.IgnoredProperties.Contains(p.Name) || p.IsRequired))
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

        string? mappedPropertyName;
        NullHandling nullHandling;

        if (context.Configuration.MappedProperties.TryGetValue(property.Name, out var mapping))
        {
            mappedPropertyName = mapping.From;
            nullHandling = mapping.NullHandling;
        }
        else
        {
            mappedPropertyName = mapPropertyAttribute.GetNamedArgumentStringValue(nameof(MapPropertyAttribute.From));
            nullHandling = mapPropertyAttribute.GetNamedArgument(nameof(MapPropertyAttribute.NullHandling), context.CodeGeneratorOptions.NullHandling);
        }

        var sourceProperty = property.FindSource(sourceTypeSymbol, mappedPropertyName);
        if (sourceProperty.NotFound)
        {
            if (property.IsRequired)
            {
                return new PropertyMapping(
                    Name: property.Name,
                    Type: property.Type.ToTypeMapping(),
                    SourceName: string.Empty,
                    SourceType: default,
                    InitializationMode: PropertyInitializationMode.None,
                    ParameterName: property.Name.ToParameterNameCasing(),
                    IsRequired: property.IsRequired,
                    TypeConverter: default,
                    NullHandling: nullHandling);
            }

            return null;
        }

        if (!TypeConverterResolver.TryGet(context, property, sourceProperty, out var converter))
        {
            return null;
        }

        return new PropertyMapping(
            Name: property.Name,
            Type: property.Type.ToTypeMapping(),
            SourceName: sourceProperty.Name,
            SourceType: sourceProperty.Type,
            InitializationMode: property.GetInitializationMode(context.KnownTypes),
            ParameterName: property.Name.ToParameterNameCasing(),
            IsRequired: property.IsRequired,
            TypeConverter: converter,
            NullHandling: nullHandling);
    }

    private static SourceProperty FindSource(this IPropertySymbol property, ITypeSymbol sourceTypeSymbol, string? propertyName)
    {
        propertyName ??= property.Name;
        var propertySegments = propertyName.Split('.');

        var nullableAnnotation = NullableAnnotation.None;
        var sourcePropertyName = new StringBuilder();

        // ReSharper disable once SuggestVarOrType_SimpleTypes
        ITypeSymbol? sourceType = sourceTypeSymbol;

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
            { IsInitOnly: false } when property.Type.IsPrimitiveType(true) || property.IsRequired => PropertyInitializationMode.ObjectInitializer,
            _ => PropertyInitializationMode.Setter
        };
    }
}