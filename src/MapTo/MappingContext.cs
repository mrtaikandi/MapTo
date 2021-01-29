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
    internal class MappingContext
    {
        private MappingContext(Compilation compilation)
        {
            Diagnostics = ImmutableArray<Diagnostic>.Empty;
            Compilation = compilation;

            IgnorePropertyAttributeTypeSymbol = compilation.GetTypeByMetadataName(IgnorePropertyAttributeSource.FullyQualifiedName)
                                                ?? throw new TypeLoadException($"Unable to find '{IgnorePropertyAttributeSource.FullyQualifiedName}' type.");

            MapTypeConverterAttributeTypeSymbol = compilation.GetTypeByMetadataName(MapTypeConverterAttributeSource.FullyQualifiedName)
                                                  ?? throw new TypeLoadException($"Unable to find '{MapTypeConverterAttributeSource.FullyQualifiedName}' type.");

            TypeConverterInterfaceTypeSymbol = compilation.GetTypeByMetadataName(ITypeConverterSource.FullyQualifiedName)
                                               ?? throw new TypeLoadException($"Unable to find '{ITypeConverterSource.FullyQualifiedName}' type.");
            
            MapPropertyAttributeTypeSymbol = compilation.GetTypeByMetadataName(MapPropertyAttributeSource.FullyQualifiedName)
                                               ?? throw new TypeLoadException($"Unable to find '{MapPropertyAttributeSource.FullyQualifiedName}' type.");
        }

        private Compilation Compilation { get; }

        public MappingModel? Model { get; private set; }

        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }

        public INamedTypeSymbol IgnorePropertyAttributeTypeSymbol { get; }
        
        public INamedTypeSymbol MapTypeConverterAttributeTypeSymbol { get; }

        public INamedTypeSymbol TypeConverterInterfaceTypeSymbol { get; }
        
        public INamedTypeSymbol MapPropertyAttributeTypeSymbol { get; }

        internal static MappingContext Create(Compilation compilation, ClassDeclarationSyntax classSyntax, SourceGenerationOptions sourceGenerationOptions)
        {
            var context = new MappingContext(compilation);
            var root = classSyntax.GetCompilationUnit();

            var semanticModel = compilation.GetSemanticModel(classSyntax.SyntaxTree);
            if (!(semanticModel.GetDeclaredSymbol(classSyntax) is INamedTypeSymbol classTypeSymbol))
            {
                return context.ReportDiagnostic(DiagnosticProvider.TypeNotFoundError(classSyntax.GetLocation(), classSyntax.Identifier.ValueText));
            }

            var sourceTypeSymbol = GetSourceTypeSymbol(semanticModel, classSyntax);
            if (sourceTypeSymbol is null)
            {
                return context.ReportDiagnostic(DiagnosticProvider.MapFromAttributeNotFoundError(classSyntax.GetLocation()));
            }

            var className = classSyntax.GetClassName();
            var sourceClassName = sourceTypeSymbol.Name;

            var mappedProperties = GetMappedProperties(context, classTypeSymbol, sourceTypeSymbol);
            if (!mappedProperties.Any())
            {
                return context.ReportDiagnostic(DiagnosticProvider.NoMatchingPropertyFoundError(classSyntax.GetLocation(), classTypeSymbol, sourceTypeSymbol));
            }

            context.Model = new MappingModel(
                sourceGenerationOptions,
                classSyntax.GetNamespace(),
                classSyntax.Modifiers,
                className,
                sourceTypeSymbol.ContainingNamespace.ToString(),
                sourceClassName,
                sourceTypeSymbol.ToString(),
                mappedProperties.ToImmutableArray());

            return context;
        }

        private MappingContext ReportDiagnostic(Diagnostic diagnostic)
        {
            Diagnostics = Diagnostics.Add(diagnostic);
            return this;
        }

        private static INamedTypeSymbol? GetSourceTypeSymbol(SemanticModel semanticModel, ClassDeclarationSyntax classSyntax)
        {
            var sourceTypeExpressionSyntax = classSyntax
                .GetAttribute(MapFromAttributeSource.AttributeName)
                ?.DescendantNodes()
                .OfType<TypeOfExpressionSyntax>()
                .SingleOrDefault();

            return sourceTypeExpressionSyntax is not null ? semanticModel.GetTypeInfo(sourceTypeExpressionSyntax.Type).Type as INamedTypeSymbol : null;
        }

        private static ImmutableArray<MappedProperty> GetMappedProperties(MappingContext context, ITypeSymbol classSymbol, ITypeSymbol sourceTypeSymbol)
        {
            var mappedProperties = new List<MappedProperty>();
            var sourceProperties = sourceTypeSymbol.GetAllMembers().OfType<IPropertySymbol>().ToArray();
            var classProperties = classSymbol.GetAllMembers().OfType<IPropertySymbol>().Where(p => !p.HasAttribute(context.IgnorePropertyAttributeTypeSymbol));

            foreach (var property in classProperties)
            {
                var sourceProperty = FindSourceProperty(context, sourceProperties, property);
                if (sourceProperty is null)
                {
                    continue;
                }

                string? converterFullyQualifiedName = null;
                var converterParameters = new List<string>();

                if (!context.Compilation.HasCompatibleTypes(sourceProperty, property))
                {
                    var typeConverterAttribute = property.GetAttribute(context.MapTypeConverterAttributeTypeSymbol);
                    if (typeConverterAttribute is null)
                    {
                        context.ReportDiagnostic(DiagnosticProvider.NoMatchingPropertyTypeFoundError(property));
                        continue;
                    }

                    var converterTypeSymbol = typeConverterAttribute.ConstructorArguments.First().Value as INamedTypeSymbol;
                    if (converterTypeSymbol is null)
                    {
                        context.ReportDiagnostic(DiagnosticProvider.NoMatchingPropertyTypeFoundError(property));
                        continue;
                    }

                    var baseInterface = GetTypeConverterBaseInterface(context, converterTypeSymbol, property, sourceProperty);
                    if (baseInterface is null)
                    {
                        context.ReportDiagnostic(DiagnosticProvider.InvalidTypeConverterGenericTypesError(property, sourceProperty));
                        continue;
                    }

                    converterFullyQualifiedName = converterTypeSymbol.ToDisplayString();
                    converterParameters.AddRange(GetTypeConverterParameters(typeConverterAttribute));
                }

                mappedProperties.Add(new MappedProperty(property.Name, converterFullyQualifiedName, converterParameters.ToImmutableArray(), sourceProperty.Name));
            }

            return mappedProperties.ToImmutableArray();
        }

        private static IPropertySymbol? FindSourceProperty(MappingContext context, IEnumerable<IPropertySymbol> sourceProperties, IPropertySymbol property)
        {
            var propertyName = property
                .GetAttribute(context.MapPropertyAttributeTypeSymbol)
                ?.NamedArguments
                .SingleOrDefault(a => a.Key == MapPropertyAttributeSource.SourcePropertyNamePropertyName)
                .Value.Value as string ?? property.Name;

            return sourceProperties.SingleOrDefault(p => p.Name == propertyName);
        }

        private static INamedTypeSymbol? GetTypeConverterBaseInterface(MappingContext context, ITypeSymbol converterTypeSymbol, IPropertySymbol property, IPropertySymbol sourceProperty)
        {
            return converterTypeSymbol.AllInterfaces
                .SingleOrDefault(i =>
                    i.TypeArguments.Length == 2 &&
                    SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, context.TypeConverterInterfaceTypeSymbol) &&
                    SymbolEqualityComparer.Default.Equals(sourceProperty.Type, i.TypeArguments[0]) &&
                    SymbolEqualityComparer.Default.Equals(property.Type, i.TypeArguments[1]));
        }

        private static IEnumerable<string> GetTypeConverterParameters(AttributeData typeConverterAttribute)
        {
            var converterParameter = typeConverterAttribute.ConstructorArguments.Skip(1).FirstOrDefault();
            return converterParameter.IsNull
                ? Enumerable.Empty<string>()
                : converterParameter.Values.Where(v => v.Value is not null).Select(v => v.Value!.ToSourceCodeString());
        }
    }
}