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
        private readonly INamedTypeSymbol _ignorePropertyAttributeTypeSymbol;
        private readonly INamedTypeSymbol _mapFromAttributeTypeSymbol;
        private readonly INamedTypeSymbol _mapPropertyAttributeTypeSymbol;
        private readonly INamedTypeSymbol _mapTypeConverterAttributeTypeSymbol;
        private readonly SemanticModel _semanticModel;
        private readonly SourceGenerationOptions _sourceGenerationOptions;
        private readonly INamedTypeSymbol _typeConverterInterfaceTypeSymbol;

        internal MappingContext(Compilation compilation, SourceGenerationOptions sourceGenerationOptions, ClassDeclarationSyntax classSyntax)
        {
            Diagnostics = ImmutableArray<Diagnostic>.Empty;
            _sourceGenerationOptions = sourceGenerationOptions;
            _classSyntax = classSyntax;
            _compilation = compilation;
            _semanticModel = _compilation.GetSemanticModel(_classSyntax.SyntaxTree);

            _ignorePropertyAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(IgnorePropertyAttributeSource.FullyQualifiedName);
            _mapTypeConverterAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(MapTypeConverterAttributeSource.FullyQualifiedName);
            _typeConverterInterfaceTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(ITypeConverterSource.FullyQualifiedName);
            _mapPropertyAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(MapPropertyAttributeSource.FullyQualifiedName);
            _mapFromAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(MapFromAttributeSource.FullyQualifiedName);

            Initialize();
        }

        public MappingModel? Model { get; private set; }

        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }

        private void Initialize()
        {
            if (!(_semanticModel.GetDeclaredSymbol(_classSyntax) is INamedTypeSymbol classTypeSymbol))
            {
                ReportDiagnostic(DiagnosticProvider.TypeNotFoundError(_classSyntax.GetLocation(), _classSyntax.Identifier.ValueText));
                return;
            }

            var sourceTypeSymbol = GetSourceTypeSymbol(_classSyntax);
            if (sourceTypeSymbol is null)
            {
                ReportDiagnostic(DiagnosticProvider.MapFromAttributeNotFoundError(_classSyntax.GetLocation()));
                return;
            }

            var className = _classSyntax.GetClassName();
            var sourceClassName = sourceTypeSymbol.Name;

            var mappedProperties = GetMappedProperties(classTypeSymbol, sourceTypeSymbol);
            if (!mappedProperties.Any())
            {
                ReportDiagnostic(DiagnosticProvider.NoMatchingPropertyFoundError(_classSyntax.GetLocation(), classTypeSymbol, sourceTypeSymbol));
                return;
            }

            Model = new MappingModel(
                _sourceGenerationOptions,
                _classSyntax.GetNamespace(),
                _classSyntax.Modifiers,
                className,
                sourceTypeSymbol.ContainingNamespace.ToString(),
                sourceClassName,
                sourceTypeSymbol.ToString(),
                mappedProperties.ToImmutableArray());
        }

        private ImmutableArray<MappedProperty> GetMappedProperties(ITypeSymbol classSymbol, ITypeSymbol sourceTypeSymbol)
        {
            var mappedProperties = new List<MappedProperty>();
            var sourceProperties = sourceTypeSymbol.GetAllMembers().OfType<IPropertySymbol>().ToArray();
            var classProperties = classSymbol.GetAllMembers().OfType<IPropertySymbol>().Where(p => !p.HasAttribute(_ignorePropertyAttributeTypeSymbol));

            foreach (var property in classProperties)
            {
                var sourceProperty = FindSourceProperty(sourceProperties, property);
                if (sourceProperty is null)
                {
                    continue;
                }

                string? converterFullyQualifiedName = null;
                var converterParameters = ImmutableArray<string>.Empty;
                string? mappedSourcePropertyType = null;

                if (!_compilation.HasCompatibleTypes(sourceProperty, property))
                {
                    if (!TryGetMapTypeConverter(property, sourceProperty, out converterFullyQualifiedName, out converterParameters) && 
                        !TryGetNestedObjectMappings(property, out mappedSourcePropertyType))
                    {
                        continue;
                    }
                }

                mappedProperties.Add(new MappedProperty(
                    property.Name, 
                    property.Type.Name,
                    converterFullyQualifiedName, 
                    converterParameters.ToImmutableArray(), 
                    sourceProperty.Name, 
                    mappedSourcePropertyType));
            }

            return mappedProperties.ToImmutableArray();
        }

        private bool TryGetNestedObjectMappings(IPropertySymbol property, out string? mappedSourcePropertyType)
        {
            mappedSourcePropertyType = null;
            
            if (!Diagnostics.IsEmpty)
            {
                return false;
            }
            
            var nestedSourceMapFromAttribute = property.Type.GetAttribute(_mapFromAttributeTypeSymbol);
            if (nestedSourceMapFromAttribute is null)
            {
                ReportDiagnostic(DiagnosticProvider.NoMatchingPropertyTypeFoundError(property));
                return false;
            }

            var nestedAttributeSyntax = nestedSourceMapFromAttribute.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;
            if (nestedAttributeSyntax is null)
            {
                ReportDiagnostic(DiagnosticProvider.NoMatchingPropertyTypeFoundError(property));
                return false;
            }

            var nestedSourceTypeSymbol = GetSourceTypeSymbol(nestedAttributeSyntax);
            if (nestedSourceTypeSymbol is null)
            {
                ReportDiagnostic(DiagnosticProvider.NoMatchingPropertyTypeFoundError(property));
                return false;
            }

            mappedSourcePropertyType = nestedSourceTypeSymbol.Name;
            return true;
        }

        private bool TryGetMapTypeConverter(IPropertySymbol property, IPropertySymbol sourceProperty, out string? converterFullyQualifiedName, out ImmutableArray<string> converterParameters)
        {
            converterFullyQualifiedName = null;
            converterParameters = ImmutableArray<string>.Empty;
            
            if (!Diagnostics.IsEmpty)
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
                ReportDiagnostic(DiagnosticProvider.InvalidTypeConverterGenericTypesError(property, sourceProperty));
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
        
        private void ReportDiagnostic(Diagnostic diagnostic)
        {
            Diagnostics = Diagnostics.Add(diagnostic);
        }

        private INamedTypeSymbol? GetSourceTypeSymbol(ClassDeclarationSyntax classDeclarationSyntax) =>
            GetSourceTypeSymbol(classDeclarationSyntax.GetAttribute(MapFromAttributeSource.AttributeName));

        private INamedTypeSymbol? GetSourceTypeSymbol(AttributeSyntax? attributeSyntax)
        {
            var sourceTypeExpressionSyntax = attributeSyntax
                ?.DescendantNodes()
                .OfType<TypeOfExpressionSyntax>()
                .SingleOrDefault();

            return sourceTypeExpressionSyntax is not null ? _semanticModel.GetTypeInfo(sourceTypeExpressionSyntax.Type).Type as INamedTypeSymbol : null;
        }
    }
}