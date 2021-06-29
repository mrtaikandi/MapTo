using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MapTo.Extensions;
using MapTo.Sources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo
{
    internal class MappingContext
    {
        private readonly TypeDeclarationSyntax _typeSyntax;
        private readonly Compilation _compilation;
        private readonly List<Diagnostic> _diagnostics;
        private readonly INamedTypeSymbol _ignorePropertyAttributeTypeSymbol;
        private readonly INamedTypeSymbol _mapFromAttributeTypeSymbol;
        private readonly INamedTypeSymbol _mapPropertyAttributeTypeSymbol;
        private readonly INamedTypeSymbol _mapTypeConverterAttributeTypeSymbol;
        private readonly INamedTypeSymbol _mappingContextTypeSymbol;
        private readonly SourceGenerationOptions _sourceGenerationOptions;
        private readonly INamedTypeSymbol _typeConverterInterfaceTypeSymbol;
        private readonly List<string> _usings;

        internal MappingContext(Compilation compilation, SourceGenerationOptions sourceGenerationOptions, TypeDeclarationSyntax typeSyntax)
        {
            _diagnostics = new List<Diagnostic>();
            _usings = new List<string> { "System", Constants.RootNamespace };
            _sourceGenerationOptions = sourceGenerationOptions;
            _typeSyntax = typeSyntax;
            _compilation = compilation;

            _ignorePropertyAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(IgnorePropertyAttributeSource.FullyQualifiedName);
            _mapTypeConverterAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(MapTypeConverterAttributeSource.FullyQualifiedName);
            _typeConverterInterfaceTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(ITypeConverterSource.FullyQualifiedName);
            _mapPropertyAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(MapPropertyAttributeSource.FullyQualifiedName);
            _mapFromAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(MapFromAttributeSource.FullyQualifiedName);
            _mappingContextTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(MappingContextSource.FullyQualifiedName);

            AddUsingIfRequired(sourceGenerationOptions.SupportNullableStaticAnalysis, "System.Diagnostics.CodeAnalysis");

            Initialize();
        }

        public MappingModel? Model { get; private set; }

        public IEnumerable<Diagnostic> Diagnostics => _diagnostics;

        private void Initialize()
        {
            var semanticModel = _compilation.GetSemanticModel(_typeSyntax.SyntaxTree);
            if (ModelExtensions.GetDeclaredSymbol(semanticModel, _typeSyntax) is not INamedTypeSymbol classTypeSymbol)
            {
                _diagnostics.Add(DiagnosticsFactory.TypeNotFoundError(_typeSyntax.GetLocation(), _typeSyntax.Identifier.ValueText));
                return;
            }

            var sourceTypeSymbol = GetSourceTypeSymbol(_typeSyntax, semanticModel);
            if (sourceTypeSymbol is null)
            {
                _diagnostics.Add(DiagnosticsFactory.MapFromAttributeNotFoundError(_typeSyntax.GetLocation()));
                return;
            }

            var typeIdentifierName = _typeSyntax.GetIdentifierName();
            var sourceTypeIdentifierName = sourceTypeSymbol.Name;
            var isTypeInheritFromMappedBaseClass = IsTypeInheritFromMappedBaseClass(semanticModel);
            var shouldGenerateSecondaryConstructor = ShouldGenerateSecondaryConstructor(semanticModel, sourceTypeSymbol);

            var mappedProperties = GetMappedProperties(classTypeSymbol, sourceTypeSymbol, isTypeInheritFromMappedBaseClass);
            if (!mappedProperties.Any())
            {
                _diagnostics.Add(DiagnosticsFactory.NoMatchingPropertyFoundError(_typeSyntax.GetLocation(), classTypeSymbol, sourceTypeSymbol));
                return;
            }

            AddUsingIfRequired(sourceTypeSymbol);
            AddUsingIfRequired(mappedProperties.Any(p => p.IsEnumerable), "System.Linq");

            Model = new MappingModel(
                _sourceGenerationOptions,
                _typeSyntax.GetNamespace(),
                _typeSyntax.Modifiers,
                _typeSyntax.Keyword.Text,
                typeIdentifierName,
                sourceTypeSymbol.ContainingNamespace.ToString(),
                sourceTypeIdentifierName,
                sourceTypeSymbol.ToString(),
                mappedProperties.ToImmutableArray(),
                isTypeInheritFromMappedBaseClass,
                _usings.ToImmutableArray(),
                shouldGenerateSecondaryConstructor);
        }

        private bool ShouldGenerateSecondaryConstructor(SemanticModel semanticModel, ISymbol sourceTypeSymbol)
        {
            var constructorSyntax = _typeSyntax.DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .SingleOrDefault(c =>
                    c.ParameterList.Parameters.Count == 1 &&
                    SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(c.ParameterList.Parameters.Single().Type!).ConvertedType, sourceTypeSymbol));

            if (constructorSyntax is null)
            {
                // Secondary constructor is not defined.
                return true;
            }

            if (constructorSyntax.Initializer?.ArgumentList.Arguments is not { Count: 2 } arguments ||
                !SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(arguments[0].Expression).ConvertedType, _mappingContextTypeSymbol) ||
                !SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(arguments[1].Expression).ConvertedType, sourceTypeSymbol))
            {
                _diagnostics.Add(DiagnosticsFactory.MissingConstructorArgument(constructorSyntax));
            }

            return false;
        }

        private bool IsTypeInheritFromMappedBaseClass(SemanticModel semanticModel)
        {
            return _typeSyntax.BaseList is not null && _typeSyntax.BaseList.Types
                .Select(t => ModelExtensions.GetTypeInfo(semanticModel, t.Type).Type)
                .Any(t => t?.GetAttribute(_mapFromAttributeTypeSymbol) != null);
        }

        private ImmutableArray<MappedProperty> GetMappedProperties(ITypeSymbol typeSymbol, ITypeSymbol sourceTypeSymbol, bool isClassInheritFromMappedBaseClass)
        {
            var mappedProperties = new List<MappedProperty>();
            var sourceProperties = sourceTypeSymbol.GetAllMembers().OfType<IPropertySymbol>().ToArray();
            var classProperties = typeSymbol.GetAllMembers(!isClassInheritFromMappedBaseClass)
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
                _diagnostics.Add(DiagnosticsFactory.NoMatchingPropertyTypeFoundError(property));
            }

            return _diagnostics.IsEmpty();
        }

        private void AddUsingIfRequired(ISymbol? namedTypeSymbol) =>
            AddUsingIfRequired(namedTypeSymbol?.ContainingNamespace.IsGlobalNamespace == false, namedTypeSymbol?.ContainingNamespace.ToDisplayString());

        private void AddUsingIfRequired(bool condition, string? ns)
        {
            if (condition && ns is not null && ns != _typeSyntax.GetNamespace() && !_usings.Contains(ns))
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
                _diagnostics.Add(DiagnosticsFactory.InvalidTypeConverterGenericTypesError(property, sourceProperty));
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

        private INamedTypeSymbol? GetSourceTypeSymbol(TypeDeclarationSyntax typeDeclarationSyntax, SemanticModel? semanticModel = null) =>
            GetSourceTypeSymbol(typeDeclarationSyntax.GetAttribute(MapFromAttributeSource.AttributeName), semanticModel);

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

            return sourceTypeExpressionSyntax is not null ? ModelExtensions.GetTypeInfo(semanticModel, sourceTypeExpressionSyntax.Type).Type as INamedTypeSymbol : null;
        }
    }
}