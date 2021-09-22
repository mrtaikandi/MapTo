using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MapTo.Extensions;
using MapTo.Sources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo
{
    internal abstract class MappingContext
    {
        private readonly List<SymbolDisplayPart> _ignoredNamespaces;

        protected MappingContext(Compilation compilation, SourceGenerationOptions sourceGenerationOptions, TypeDeclarationSyntax typeSyntax)
        {
            _ignoredNamespaces = new();
            Diagnostics = ImmutableArray<Diagnostic>.Empty;
            Usings = ImmutableArray.Create("System", Constants.RootNamespace);
            SourceGenerationOptions = sourceGenerationOptions;
            TypeSyntax = typeSyntax;
            Compilation = compilation;

            IgnorePropertyAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(IgnorePropertyAttributeSource.FullyQualifiedName);
            MapTypeConverterAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(MapTypeConverterAttributeSource.FullyQualifiedName);
            TypeConverterInterfaceTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(ITypeConverterSource.FullyQualifiedName);
            MapPropertyAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(MapPropertyAttributeSource.FullyQualifiedName);
            MapFromAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(MapFromAttributeSource.FullyQualifiedName);
            MappingContextTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(MappingContextSource.FullyQualifiedName);

            AddUsingIfRequired(sourceGenerationOptions.SupportNullableStaticAnalysis, "System.Diagnostics.CodeAnalysis");
        }

        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }

        public IEnumerable<MappingModel> Models { get; private set; } = new List<MappingModel>();

        protected Compilation Compilation { get; }

        protected INamedTypeSymbol IgnorePropertyAttributeTypeSymbol { get; }

        protected INamedTypeSymbol MapFromAttributeTypeSymbol { get; }

        protected INamedTypeSymbol MappingContextTypeSymbol { get; }

        protected INamedTypeSymbol MapPropertyAttributeTypeSymbol { get; }

        protected INamedTypeSymbol MapTypeConverterAttributeTypeSymbol { get; }

        protected SourceGenerationOptions SourceGenerationOptions { get; }

        protected INamedTypeSymbol TypeConverterInterfaceTypeSymbol { get; }

        protected TypeDeclarationSyntax TypeSyntax { get; }

        protected ImmutableArray<string> Usings { get; private set; }

        public static MappingContext Create(Compilation compilation, SourceGenerationOptions sourceGenerationOptions, TypeDeclarationSyntax typeSyntax)
        {
            MappingContext context = typeSyntax switch
            {
                ClassDeclarationSyntax => new ClassMappingContext(compilation, sourceGenerationOptions, typeSyntax),
                RecordDeclarationSyntax => new RecordMappingContext(compilation, sourceGenerationOptions, typeSyntax),
                _ => throw new ArgumentOutOfRangeException()
            };

            context.Models = context.CreateMappingModelList();

            return context;
        }

        protected void AddDiagnostic(Diagnostic diagnostic)
        {
            Diagnostics = Diagnostics.Add(diagnostic);
        }

        protected void AddUsingIfRequired(ISymbol? namedTypeSymbol) =>
            AddUsingIfRequired(namedTypeSymbol?.ContainingNamespace.IsGlobalNamespace == false, namedTypeSymbol?.ContainingNamespace);

        protected void AddUsingIfRequired(bool condition, INamespaceSymbol? ns) =>
            AddUsingIfRequired(condition && ns is not null && !_ignoredNamespaces.Contains(ns.ToDisplayParts().First()), ns?.ToDisplayString());

        protected void AddUsingIfRequired(bool condition, string? ns)
        {
            if (ns is not null && condition && ns != TypeSyntax.GetNamespace() && !Usings.Contains(ns))
            {
                Usings = Usings.Add(ns);
            }
        }

        protected IPropertySymbol? FindSourceProperty(ISymbol sourceTypeSymbol, IEnumerable<IPropertySymbol> sourceProperties, ISymbol property)
        {
            var propertyName = property
                .GetAttributes(MapPropertyAttributeTypeSymbol)
                .FirstOrDefault(a =>
                {
                    if(a.GetAttributeParameterValue(MapPropertyAttributeSource.SourceTypeName) is ISymbol sourcePropertyType)
                    {
                        return sourcePropertyType.Equals(sourceTypeSymbol, SymbolEqualityComparer.Default);
                    }

                    return true;
                }
                )
                ?.NamedArguments
                .SingleOrDefault(a => a.Key == MapPropertyAttributeSource.SourcePropertyNamePropertyName)
                .Value.Value as string ?? property.Name;

            return sourceProperties.SingleOrDefault(p => p.Name == propertyName);
        }

        protected abstract ImmutableArray<MappedProperty> GetMappedProperties(ITypeSymbol typeSymbol, ITypeSymbol sourceTypeSymbol, bool isInheritFromMappedBaseClass);

        protected IEnumerable<INamedTypeSymbol> GetSourceTypesSymbol(TypeDeclarationSyntax typeDeclarationSyntax, SemanticModel? semanticModel = null) =>
            GetSourceTypesSymbol(typeDeclarationSyntax.GetAttributes(MapFromAttributeSource.AttributeName), semanticModel);

        protected IEnumerable<INamedTypeSymbol> GetSourceTypesSymbol(IEnumerable<SyntaxNode> attributeSyntaxList, SemanticModel? semanticModel = null)
        {
            var result = new List<INamedTypeSymbol>();
            if (attributeSyntaxList is null || attributeSyntaxList.IsEmpty())
            {
                return result;
            }

            foreach (var attributeSyntax in attributeSyntaxList)
            {
                semanticModel ??= Compilation.GetSemanticModel(attributeSyntax.SyntaxTree);
                var sourceTypeExpressionSyntax = attributeSyntax
                    .DescendantNodes()
                    .OfType<TypeOfExpressionSyntax>()
                    .SingleOrDefault();

                var typeSymbol = sourceTypeExpressionSyntax is not null ? semanticModel.GetTypeInfo(sourceTypeExpressionSyntax.Type).Type as INamedTypeSymbol : null;
                if (typeSymbol is not null)
                {
                    result.Add(typeSymbol);
                }
            }

            return result;
        }

        protected bool IsTypeInheritFromMappedBaseClass(SemanticModel semanticModel)
        {
            return TypeSyntax.BaseList is not null && TypeSyntax.BaseList.Types
                .Select(t => semanticModel.GetTypeInfo(t.Type).Type)
                .Any(t => t?.GetAttribute(MapFromAttributeTypeSymbol) != null);
        }

        protected virtual MappedProperty? MapProperty(ISymbol sourceTypeSymbol, IReadOnlyCollection<IPropertySymbol> sourceProperties, ISymbol property)
        {
            var sourceProperty = FindSourceProperty(sourceTypeSymbol, sourceProperties, property);
            if (sourceProperty is null || !property.TryGetTypeSymbol(out var propertyType))
            {
                return null;
            }

            string? converterFullyQualifiedName = null;
            var converterParameters = ImmutableArray<string>.Empty;
            ITypeSymbol? mappedSourcePropertyType = null;
            ITypeSymbol? enumerableTypeArgumentType = null;

            if (!Compilation.HasCompatibleTypes(sourceProperty, property))
            {
                if (!TryGetMapTypeConverter(sourceTypeSymbol, property, sourceProperty, out converterFullyQualifiedName, out converterParameters) &&
                    !TryGetNestedObjectMappings(property, out mappedSourcePropertyType, out enumerableTypeArgumentType))
                {
                    return null;
                }
            }

            AddUsingIfRequired(propertyType);
            AddUsingIfRequired(enumerableTypeArgumentType);
            AddUsingIfRequired(mappedSourcePropertyType);

            return new MappedProperty(
                property.Name,
                ToQualifiedDisplayName(propertyType) ?? propertyType.Name,
                converterFullyQualifiedName,
                converterParameters.ToImmutableArray(),
                sourceProperty.Name,
                ToQualifiedDisplayName(mappedSourcePropertyType),
                ToQualifiedDisplayName(enumerableTypeArgumentType));
        }

        protected bool TryGetMapTypeConverter(ISymbol sourceTypeSymbol, ISymbol property, IPropertySymbol sourceProperty, out string? converterFullyQualifiedName,
            out ImmutableArray<string> converterParameters)
        {
            converterFullyQualifiedName = null;
            converterParameters = ImmutableArray<string>.Empty;

            if (!Diagnostics.IsEmpty())
            {
                return false;
            }

            var typeConverterAttribute = property
                .GetAttributes(MapTypeConverterAttributeTypeSymbol)
                .SingleOrDefault(a => {
                    if (a.GetAttributeParameterValue(MapTypeConverterAttributeSource.SourceTypeName) is ISymbol propertyTypeFromAttribute)
                    {
                        return propertyTypeFromAttribute.Equals(sourceTypeSymbol, SymbolEqualityComparer.Default);
                    }

                    return true;
                });

            if (typeConverterAttribute?.ConstructorArguments.First().Value is not INamedTypeSymbol converterTypeSymbol)
            {
                return false;
            }

            var baseInterface = GetTypeConverterBaseInterface(converterTypeSymbol, property, sourceProperty);
            if (baseInterface is null)
            {
                AddDiagnostic(DiagnosticsFactory.InvalidTypeConverterGenericTypesError(property, sourceProperty));
                return false;
            }

            converterFullyQualifiedName = converterTypeSymbol.ToDisplayString();
            converterParameters = GetTypeConverterParameters(typeConverterAttribute);
            return true;
        }

        protected bool TryGetNestedObjectMappings(ISymbol property, out ITypeSymbol? mappedSourcePropertyType, out ITypeSymbol? enumerableTypeArgument)
        {
            mappedSourcePropertyType = null;
            enumerableTypeArgument = null;

            if (!Diagnostics.IsEmpty())
            {
                return false;
            }

            if (!property.TryGetTypeSymbol(out var propertyType))
            {
                AddDiagnostic(DiagnosticsFactory.NoMatchingPropertyTypeFoundError(property));
                return false;
            }

            var mapFromAttribute = propertyType.GetAttribute(MapFromAttributeTypeSymbol);
            if (mapFromAttribute is null &&
                propertyType is INamedTypeSymbol namedTypeSymbol &&
                !propertyType.IsPrimitiveType() &&
                (Compilation.IsGenericEnumerable(propertyType) || propertyType.AllInterfaces.Any(i => Compilation.IsGenericEnumerable(i))))
            {
                enumerableTypeArgument = namedTypeSymbol.TypeArguments.First();
                mapFromAttribute = enumerableTypeArgument.GetAttribute(MapFromAttributeTypeSymbol);
            }

            mappedSourcePropertyType = mapFromAttribute?.ConstructorArguments.First().Value as INamedTypeSymbol;

            if (mappedSourcePropertyType is null && enumerableTypeArgument is null)
            {
                AddDiagnostic(DiagnosticsFactory.NoMatchingPropertyTypeFoundError(property));
            }

            return Diagnostics.IsEmpty();
        }

        private static ImmutableArray<string> GetTypeConverterParameters(AttributeData typeConverterAttribute)
        {
            var converterParameter = typeConverterAttribute.ConstructorArguments.Skip(1).FirstOrDefault();
            return converterParameter.IsNull
                ? ImmutableArray<string>.Empty
                : converterParameter.Values.Where(v => v.Value is not null).Select(v => v.Value!.ToSourceCodeString()).ToImmutableArray();
        }

        private IEnumerable<MappingModel> CreateMappingModelList()
        {
            var semanticModel = Compilation.GetSemanticModel(TypeSyntax.SyntaxTree);
            var result = new List<MappingModel>();
            if (semanticModel.GetDeclaredSymbol(TypeSyntax) is not INamedTypeSymbol typeSymbol)
            {
                AddDiagnostic(DiagnosticsFactory.TypeNotFoundError(TypeSyntax.GetLocation(), TypeSyntax.Identifier.ValueText));
                return result;
            }

            var sourceTypeSymbolList = GetSourceTypesSymbol(TypeSyntax, semanticModel);
            if (sourceTypeSymbolList is null || !sourceTypeSymbolList.Any())
            {
                AddDiagnostic(DiagnosticsFactory.MapFromAttributeNotFoundError(TypeSyntax.GetLocation()));
                return result;
            }

            result = sourceTypeSymbolList
                .Select(s => CreateMappingModel(s, typeSymbol, semanticModel))
                .Where(s => s is not null)
                .ToList()!;

            return result;
        }

        private MappingModel? CreateMappingModel(INamedTypeSymbol sourceTypeSymbol, INamedTypeSymbol typeSymbol, SemanticModel semanticModel)
        {
            _ignoredNamespaces.Add(sourceTypeSymbol.ContainingNamespace.ToDisplayParts().First());

            var typeIdentifierName = TypeSyntax.GetIdentifierName();
            var sourceTypeIdentifierName = sourceTypeSymbol.Name;
            var isTypeInheritFromMappedBaseClass = IsTypeInheritFromMappedBaseClass(semanticModel);
            var shouldGenerateSecondaryConstructor = ShouldGenerateSecondaryConstructor(semanticModel, sourceTypeSymbol);

            var mappedProperties = GetMappedProperties(typeSymbol, sourceTypeSymbol, isTypeInheritFromMappedBaseClass);
            if (!mappedProperties.Any())
            {
                AddDiagnostic(DiagnosticsFactory.NoMatchingPropertyFoundError(TypeSyntax.GetLocation(), typeSymbol, sourceTypeSymbol));
                return null;
            }

            AddUsingIfRequired(mappedProperties.Any(p => p.IsEnumerable), "System.Linq");

            return new MappingModel(
                SourceGenerationOptions,
                TypeSyntax.GetNamespace(),
                TypeSyntax.Modifiers,
                TypeSyntax.Keyword.Text,
                typeIdentifierName,
                sourceTypeSymbol.ContainingNamespace.ToDisplayString(),
                sourceTypeIdentifierName,
                sourceTypeSymbol.ToDisplayString(),
                mappedProperties,
                isTypeInheritFromMappedBaseClass,
                Usings,
                shouldGenerateSecondaryConstructor);
        }
        private INamedTypeSymbol? GetTypeConverterBaseInterface(ITypeSymbol converterTypeSymbol, ISymbol property, IPropertySymbol sourceProperty)
        {
            if (!property.TryGetTypeSymbol(out var propertyType))
            {
                return null;
            }

            return converterTypeSymbol.AllInterfaces
                .SingleOrDefault(i =>
                    i.TypeArguments.Length == 2 &&
                    SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, TypeConverterInterfaceTypeSymbol) &&
                    SymbolEqualityComparer.Default.Equals(sourceProperty.Type, i.TypeArguments[0]) &&
                    SymbolEqualityComparer.Default.Equals(propertyType, i.TypeArguments[1]));
        }

        private bool ShouldGenerateSecondaryConstructor(SemanticModel semanticModel, ISymbol sourceTypeSymbol)
        {
            var constructorSyntax = TypeSyntax.DescendantNodes()
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
                !SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(arguments[0].Expression).ConvertedType, MappingContextTypeSymbol) ||
                !SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(arguments[1].Expression).ConvertedType, sourceTypeSymbol))
            {
                AddDiagnostic(DiagnosticsFactory.MissingConstructorArgument(constructorSyntax));
            }

            return false;
        }

        private string? ToQualifiedDisplayName(ISymbol? symbol)
        {
            if (symbol is null)
            {
                return null;
            }

            var containingNamespace = TypeSyntax.GetNamespace();
            var symbolNamespace = symbol.ContainingNamespace.ToDisplayString();
            return  containingNamespace != symbolNamespace && _ignoredNamespaces.Contains(symbol.ContainingNamespace.ToDisplayParts().First())
                ? symbol.ToDisplayString()
                : symbol.Name;
        }
    }
}