using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MapTo.Extensions;
using MapTo.Sources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo
{
    internal class MappingContext
    {
        private readonly ClassDeclarationSyntax _classSyntax;
        private readonly Compilation _compilation;
        private readonly List<Diagnostic> _diagnostics;
        private readonly INamedTypeSymbol _ignorePropertyAttributeTypeSymbol;
        private readonly INamedTypeSymbol _mapFromAttributeTypeSymbol;
        private readonly INamedTypeSymbol _mapPropertyAttributeTypeSymbol;
        private readonly INamedTypeSymbol _mapTypeConverterAttributeTypeSymbol;
        private readonly SourceGenerationOptions _sourceGenerationOptions;
        private readonly INamedTypeSymbol _typeConverterInterfaceTypeSymbol;
        private readonly List<string> _usings;

        internal MappingContext(Compilation compilation, SourceGenerationOptions sourceGenerationOptions, ClassDeclarationSyntax classSyntax)
        {
            _diagnostics = new List<Diagnostic>();
            _usings = new List<string> { "System", Constants.RootNamespace };
            _sourceGenerationOptions = sourceGenerationOptions;
            _classSyntax = classSyntax;
            _compilation = compilation;

            _ignorePropertyAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(IgnorePropertyAttributeSource.FullyQualifiedName);
            _mapTypeConverterAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(MapTypeConverterAttributeSource.FullyQualifiedName);
            _typeConverterInterfaceTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(ITypeConverterSource.FullyQualifiedName);
            _mapPropertyAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(MapPropertyAttributeSource.FullyQualifiedName);
            _mapFromAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(MapFromAttributeSource.FullyQualifiedName);

            AddUsingIfRequired(sourceGenerationOptions.SupportNullableStaticAnalysis, "System.Diagnostics.CodeAnalysis");

            Initialize();
        }

        public MappingModel? Model { get; private set; }

        public IEnumerable<Diagnostic> Diagnostics => _diagnostics;

        private void Initialize()
        {
            var semanticModel = _compilation.GetSemanticModel(_classSyntax.SyntaxTree);
            if (!(semanticModel.GetDeclaredSymbol(_classSyntax) is INamedTypeSymbol classTypeSymbol))
            {
                _diagnostics.Add(DiagnosticProvider.TypeNotFoundError(_classSyntax.GetLocation(), _classSyntax.Identifier.ValueText));
                return;
            }

            var sourceTypeSymbol = GetSourceTypeSymbol(_classSyntax, semanticModel);
            if (sourceTypeSymbol is null)
            {
                _diagnostics.Add(DiagnosticProvider.MapFromAttributeNotFoundError(_classSyntax.GetLocation()));
                return;
            }

            var className = _classSyntax.GetClassName();
            var sourceClassName = sourceTypeSymbol.Name;
            var isClassInheritFromMappedBaseClass = IsClassInheritFromMappedBaseClass(semanticModel);

            var mappedProperties = GetMappedProperties(classTypeSymbol, sourceTypeSymbol, isClassInheritFromMappedBaseClass);
            if (!mappedProperties.Any())
            {
                _diagnostics.Add(DiagnosticProvider.NoMatchingPropertyFoundError(_classSyntax.GetLocation(), classTypeSymbol, sourceTypeSymbol));
                return;
            }

            AddUsingIfRequired(sourceTypeSymbol);
            AddUsingIfRequired(mappedProperties.Any(p => p.IsEnumerable), "System.Linq");

            Model = new MappingModel(
                _sourceGenerationOptions,
                _classSyntax.GetNamespace(),
                _classSyntax.Modifiers,
                className,
                sourceTypeSymbol.ContainingNamespace.ToString(),
                sourceClassName,
                sourceTypeSymbol.ToString(),
                mappedProperties.ToImmutableArray(),
                isClassInheritFromMappedBaseClass,
                _usings.ToImmutableArray());
        }

        private bool IsClassInheritFromMappedBaseClass(SemanticModel semanticModel)
        {
            return _classSyntax.BaseList is not null && _classSyntax.BaseList.Types
                .Select(t => semanticModel.GetTypeInfo(t.Type).Type)
                .Any(t => t?.GetAttribute(_mapFromAttributeTypeSymbol) != null);
        }

        private ImmutableArray<MappedProperty> GetMappedProperties(ITypeSymbol classSymbol, ITypeSymbol sourceTypeSymbol, bool isClassInheritFromMappedBaseClass)
        {
            var mappedProperties = new List<MappedProperty>();
            var sourceProperties = sourceTypeSymbol.GetAllMembers().OfType<IPropertySymbol>().ToArray();
            var classProperties = classSymbol.GetAllMembers(!isClassInheritFromMappedBaseClass)
                .OfType<IPropertySymbol>()
                .Where(p => !p.HasAttribute(_ignorePropertyAttributeTypeSymbol));

            foreach (var property in classProperties)
            {
                var sourceProperty = FindSourceProperty(sourceProperties, property);
                if (sourceProperty is null)
                {
                    continue;
                }

                string? converterFullyQualifiedName = null;
                var converterParameters = ImmutableArray<string>.Empty;
                ITypeSymbol? mappedSourcePropertyType = null;
                ITypeSymbol? enumerableTypeArgumentType = null;

                if (!_compilation.HasCompatibleTypes(sourceProperty, property))
                {
                    if (!TryGetMapTypeConverter(property, sourceProperty, out converterFullyQualifiedName, out converterParameters) &&
                        !TryGetNestedObjectMappings(property, out mappedSourcePropertyType, out enumerableTypeArgumentType))
                    {
                        continue;
                    }
                }

                AddUsingIfRequired(property.Type);
                AddUsingIfRequired(sourceTypeSymbol);
                AddUsingIfRequired(enumerableTypeArgumentType);
                AddUsingIfRequired(mappedSourcePropertyType);

                mappedProperties.Add(
                    new MappedProperty(
                        property.Name,
                        property.Type.Name,
                        converterFullyQualifiedName,
                        converterParameters.ToImmutableArray(),
                        sourceProperty.Name,
                        mappedSourcePropertyType?.Name,
                        enumerableTypeArgumentType?.Name));
            }

            return mappedProperties.ToImmutableArray();
        }

        private bool TryGetNestedObjectMappings(IPropertySymbol property, out ITypeSymbol? mappedSourcePropertyType, out ITypeSymbol? enumerableTypeArgument)
        {
            mappedSourcePropertyType = null;
            enumerableTypeArgument = null;

            if (!_diagnostics.IsEmpty())
            {
                return false;
            }

            var mapFromAttribute = property.Type.GetAttribute(_mapFromAttributeTypeSymbol);
            if (mapFromAttribute is null && 
                property.Type is INamedTypeSymbol namedTypeSymbol && 
                !property.Type.IsPrimitiveType() &&
                (_compilation.IsGenericEnumerable(property.Type) || property.Type.AllInterfaces.Any(i => _compilation.IsGenericEnumerable(i))))
            {
                enumerableTypeArgument = namedTypeSymbol.TypeArguments.First();
                mapFromAttribute = enumerableTypeArgument.GetAttribute(_mapFromAttributeTypeSymbol);
            }

            mappedSourcePropertyType = mapFromAttribute?.ConstructorArguments.First().Value as INamedTypeSymbol;

            if (mappedSourcePropertyType is null && enumerableTypeArgument is null)
            {
                _diagnostics.Add(DiagnosticProvider.NoMatchingPropertyTypeFoundError(property));
            }

            return _diagnostics.IsEmpty();
        }

        private void AddUsingIfRequired(ISymbol? namedTypeSymbol) =>
            AddUsingIfRequired(namedTypeSymbol?.ContainingNamespace.IsGlobalNamespace == false, namedTypeSymbol?.ContainingNamespace.ToDisplayString());

        private void AddUsingIfRequired(bool condition, string? ns)
        {
            if (condition && ns is not null && ns != _classSyntax.GetNamespace() && !_usings.Contains(ns))
            {
                _usings.Add(ns);
            }
        }

        private bool TryGetMapTypeConverter(IPropertySymbol property, IPropertySymbol sourceProperty, out string? converterFullyQualifiedName, out ImmutableArray<string> converterParameters)
        {
            converterFullyQualifiedName = null;
            converterParameters = ImmutableArray<string>.Empty;

            if (!_diagnostics.IsEmpty())
            {
                return false;
            }

            var typeConverterAttribute = property.GetAttribute(_mapTypeConverterAttributeTypeSymbol);
            if (!(typeConverterAttribute?.ConstructorArguments.First().Value is INamedTypeSymbol converterTypeSymbol))
            {
                return false;
            }

            var baseInterface = GetTypeConverterBaseInterface(converterTypeSymbol, property, sourceProperty);
            if (baseInterface is null)
            {
                _diagnostics.Add(DiagnosticProvider.InvalidTypeConverterGenericTypesError(property, sourceProperty));
                return false;
            }

            converterFullyQualifiedName = converterTypeSymbol.ToDisplayString();
            converterParameters = GetTypeConverterParameters(typeConverterAttribute);
            return true;
        }

        private IPropertySymbol? FindSourceProperty(IEnumerable<IPropertySymbol> sourceProperties, IPropertySymbol property)
        {
            var propertyName = property
                .GetAttribute(_mapPropertyAttributeTypeSymbol)
                ?.NamedArguments
                .SingleOrDefault(a => a.Key == MapPropertyAttributeSource.SourcePropertyNamePropertyName)
                .Value.Value as string ?? property.Name;

            return sourceProperties.SingleOrDefault(p => p.Name == propertyName);
        }

        private INamedTypeSymbol? GetTypeConverterBaseInterface(ITypeSymbol converterTypeSymbol, IPropertySymbol property, IPropertySymbol sourceProperty)
        {
            return converterTypeSymbol.AllInterfaces
                .SingleOrDefault(i =>
                    i.TypeArguments.Length == 2 &&
                    SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, _typeConverterInterfaceTypeSymbol) &&
                    SymbolEqualityComparer.Default.Equals(sourceProperty.Type, i.TypeArguments[0]) &&
                    SymbolEqualityComparer.Default.Equals(property.Type, i.TypeArguments[1]));
        }

        private static ImmutableArray<string> GetTypeConverterParameters(AttributeData typeConverterAttribute)
        {
            var converterParameter = typeConverterAttribute.ConstructorArguments.Skip(1).FirstOrDefault();
            return converterParameter.IsNull
                ? ImmutableArray<string>.Empty
                : converterParameter.Values.Where(v => v.Value is not null).Select(v => v.Value!.ToSourceCodeString()).ToImmutableArray();
        }

        private INamedTypeSymbol? GetSourceTypeSymbol(ClassDeclarationSyntax classDeclarationSyntax, SemanticModel? semanticModel = null) =>
            GetSourceTypeSymbol(classDeclarationSyntax.GetAttribute(MapFromAttributeSource.AttributeName), semanticModel);

        private INamedTypeSymbol? GetSourceTypeSymbol(AttributeSyntax? attributeSyntax, SemanticModel? semanticModel = null)
        {
            if (attributeSyntax is null)
            {
                return null;
            }

            semanticModel ??= _compilation.GetSemanticModel(attributeSyntax.SyntaxTree);
            var sourceTypeExpressionSyntax = attributeSyntax
                .DescendantNodes()
                .OfType<TypeOfExpressionSyntax>()
                .SingleOrDefault();

            return sourceTypeExpressionSyntax is not null ? semanticModel.GetTypeInfo(sourceTypeExpressionSyntax.Type).Type as INamedTypeSymbol : null;
        }
    }
}