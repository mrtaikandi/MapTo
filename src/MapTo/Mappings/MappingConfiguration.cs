using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo.Mappings;

internal readonly record struct MappingConfiguration(
    TypeMapping SourceType,
    bool? CopyPrimitiveArrays,
    ReferenceHandling? ReferenceHandling,
    NullHandling? NullHandling,
    EnumMappingStrategy? EnumMappingStrategy,
    StrictEnumMapping? StrictEnumMapping,
    object? EnumMappingFallbackValue,
    ProjectionType? ProjectTo,
    ExpressionSyntax? BeforeMap,
    Location BeforeMapArgumentLocation,
    ExpressionSyntax? AfterMap,
    Location AfterMapArgumentLocation,
    ImmutableDictionary<string, TypeConverterMapping> TypeConverters,
    ImmutableArray<string> IgnoredProperties,
    ImmutableDictionary<string, (string From, NullHandling NullHandling)> MappedProperties);

internal static class AttributeDataMappingExtensions
{
    public static MappingConfiguration ToMappingConfiguration(this AttributeData attribute)
    {
        var sourceType = attribute switch
        {
            { AttributeClass.TypeArguments.Length: > 0 } => attribute.AttributeClass!.TypeArguments[0],
            { ConstructorArguments.Length: > 0 } => attribute.ConstructorArguments[0].Value as ITypeSymbol,
            _ => null
        };

        return CreateMappingConfiguration(attribute, sourceType);
    }

    public static Result<MappingConfiguration> ToMappingConfiguration(
        this AttributeData attribute,
        SemanticModel semanticModel,
        INamedTypeSymbol sourceTypeSymbol,
        INamedTypeSymbol targetTypeSymbol)
    {
        var compilation = semanticModel.Compilation;
        var mappingConfiguration = CreateMappingConfiguration(attribute, sourceTypeSymbol);

        var configurationMethod = attribute.GetConfigurationMethod(compilation, sourceTypeSymbol, targetTypeSymbol);
        return configurationMethod.Kind switch
        {
            ResultKind.Undetermined => Result.Success(mappingConfiguration),
            ResultKind.Failure => configurationMethod.Error!,
            ResultKind.Success => mappingConfiguration.WithConfigurationMethod(configurationMethod.Value, semanticModel),
            _ => throw new ArgumentOutOfRangeException($"Unexpected result kind: {configurationMethod.Kind}")
        };
    }

    private static Result<IMethodSymbol> GetConfigurationMethod(
        this AttributeData attributeData,
        Compilation compilation,
        INamedTypeSymbol sourceTypeSymbol,
        INamedTypeSymbol targetTypeSymbol)
    {
        if (attributeData.AttributeConstructor?.Parameters.Length != 1 ||
            attributeData.ConstructorArguments.Length != 1 ||
            attributeData.ConstructorArguments[0].Type?.SpecialType != SpecialType.System_String)
        {
            return Result.Undetermined<IMethodSymbol>();
        }

        var configurationExpression = attributeData.GetAttributeSyntax()?.ArgumentList?.Arguments.FirstOrDefault()?.Expression;
        var configurationMethodSymbol = configurationExpression.GetMethodSymbol(compilation);
        if (configurationMethodSymbol == null)
        {
            return Result.Undetermined<IMethodSymbol>();
        }

        var validationResult = ValidateConfigurationMethod(configurationMethodSymbol, sourceTypeSymbol, targetTypeSymbol);
        return validationResult.IsSuccess ? Result.Success(configurationMethodSymbol) : validationResult;
    }

    private static MappingConfiguration WithConfigurationMethod(this MappingConfiguration config, IMethodSymbol configurationMethodSymbol, SemanticModel semanticModel)
    {
        if (configurationMethodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not MethodDeclarationSyntax configurationMethodDeclarationSyntax)
        {
            return config;
        }

        var statements = configurationMethodDeclarationSyntax.Body?.Statements ?? [];
        foreach (var configurationStatement in statements.OfType<ExpressionStatementSyntax>())
        {
            switch (configurationStatement.Expression)
            {
                case AssignmentExpressionSyntax assignmentSyntax:
                    var value = assignmentSyntax.Right;
                    config = (assignmentSyntax.Left as MemberAccessExpressionSyntax)?.Name.Identifier.Text switch
                    {
                        nameof(MappingConfiguration.BeforeMap) => config with { BeforeMap = value, BeforeMapArgumentLocation = value.GetLocation() },
                        nameof(MappingConfiguration.AfterMap) => config with { AfterMap = value, AfterMapArgumentLocation = value.GetLocation() },
                        nameof(MappingConfiguration.NullHandling) => config with { NullHandling = value.GetEnumValue<NullHandling>() },
                        nameof(MappingConfiguration.ReferenceHandling) => config with { ReferenceHandling = value.GetEnumValue<ReferenceHandling>() },
                        nameof(MappingConfiguration.EnumMappingStrategy) => config with { EnumMappingStrategy = value.GetEnumValue<EnumMappingStrategy>() },
                        nameof(MappingConfiguration.ProjectTo) => config with { ProjectTo = value.GetEnumValue<ProjectionType>() },
                        nameof(MappingConfiguration.CopyPrimitiveArrays) => config with { CopyPrimitiveArrays = bool.Parse(value.ToString()) },
                        nameof(MappingConfiguration.StrictEnumMapping) => config with { StrictEnumMapping = value.GetEnumValue<StrictEnumMapping>() },
                        nameof(MappingConfiguration.EnumMappingFallbackValue) => config with { EnumMappingFallbackValue = (value as LiteralExpressionSyntax)?.Token.Value },
                        _ => config
                    };

                    break;

                case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess } invocationExpression
                    when memberAccess.TryGetTargetPropertyName(out var propertyName):

                    config = memberAccess.Name.Identifier.Text switch
                    {
                        KnownTypes.MappingConfigurationTypeConverterSelectorName => config with
                        {
                            TypeConverters = config.TypeConverters.SetItem(propertyName, invocationExpression.GetTypeConverterMapping(semanticModel, propertyName))
                        },
                        KnownTypes.MappingConfigurationIgnoreSelectorName => config with
                        {
                            IgnoredProperties = config.IgnoredProperties.Add(propertyName)
                        },
                        KnownTypes.MappingConfigurationMapToSelectorName => config with
                        {
                            MappedProperties = config.MappedProperties.SetItem(propertyName, invocationExpression.GetMappedProperty(semanticModel))
                        },
                        _ => throw new ArgumentOutOfRangeException($"Unexpected method name: {memberAccess.Name.Identifier.Text}")
                    };

                    break;
            }
        }

        return config;
    }

    private static (string From, NullHandling NullHandling) GetMappedProperty(this InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel)
    {
        var invocationArgs = invocationExpression.ArgumentList.Arguments;
        if (invocationArgs.Count is < 1 or > 2)
        {
            return default;
        }

        var expression = invocationArgs[0].Expression as SimpleLambdaExpressionSyntax;
        var from = expression?.Body as MemberAccessExpressionSyntax ?? throw new InvalidOperationException("Unable to determine target property from MapTo attribute.");

        return (
            from.Name.Identifier.Text,
            invocationArgs.Skip(1).SingleOrDefault()?.Expression.GetEnumValue<NullHandling>(semanticModel) ?? NullHandling.Auto);
    }

    private static TypeConverterMapping GetTypeConverterMapping(this InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, string propertyName)
    {
        var invocationArgs = invocationExpression.ArgumentList.Arguments;
        if (invocationArgs.Count is < 1 or > 2)
        {
            return default;
        }

        var expression = invocationArgs[0].Expression;
        switch (expression)
        {
            // example: UseTypeConverter<string>(StringToIntTypeConverter, new[] { 1 }) or UseTypeConverter<string>(StringToIntTypeConverter)
            case IdentifierNameSyntax syntax:
                var converterMethodSymbol = semanticModel.Compilation.GetMethodSymbol(syntax, symbol =>
                {
                    return symbol.CandidateReason is CandidateReason.OverloadResolutionFailure
                        ? symbol.CandidateSymbols.OfType<IMethodSymbol>().SingleOrDefault(m => m.Parameters.Length == invocationArgs.Count)
                        : null;
                });

                return converterMethodSymbol is null
                    ? default
                    : new TypeConverterMapping(
                        new MethodMapping(
                            converterMethodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            syntax.Identifier.Text,
                            converterMethodSymbol.ReturnType.ToTypeMapping()),
                        true,
                        invocationArgs.Skip(1).SingleOrDefault()?.Expression.ToString(),
                        true);

            // examples:
            // UseTypeConverter((string x) => { return int.Parse(x); }) or UseTypeConverter((string x) => { return int.Parse(x); }, new[] { 1 })
            // UseTypeConverter((string x) => int.Parse(x)) or UseTypeConverter((string x) => int.Parse(x), new[] { 1 })
            // UseTypeConverter<string>(x => { return int.Parse(x); }) or UseTypeConverter<string>(x => { return int.Parse(x); }, new[] { 1 })
            // UseTypeConverter<string>(x => int.Parse(x)) or UseTypeConverter<string>(x => int.Parse(x), new[] { 1 })
            case LambdaExpressionSyntax syntax:
                var returnType = semanticModel.GetTypeInfo(syntax).GetDelegateReturnType().ToTypeMapping();
                return new TypeConverterMapping(
                    new MethodMapping(
                        string.Empty,
                        $"Generated_{propertyName}To{returnType.Name}Converter",
                        returnType,
                        syntax switch
                        {
                            SimpleLambdaExpressionSyntax simpleLambda => simpleLambda.GetParameterMappings(semanticModel),
                            ParenthesizedLambdaExpressionSyntax parenthesizedLambda => parenthesizedLambda.GetParameterMappings(semanticModel),
                            _ => ImmutableArray<ParameterMapping>.Empty
                        },
                        syntax.Body switch
                        {
                            BlockSyntax b => b.Statements.Select(s => s.ToString()).ToImmutableArray(),
                            _ => ImmutableArray.Create(syntax.Body.ToString())
                        }),
                    true,
                    invocationArgs.Skip(1).SingleOrDefault()?.Expression.ToString(),
                    true);

            default:
                return default;
        }
    }

    private static bool TryGetTargetPropertyName(this MemberAccessExpressionSyntax memberAccess, out string propertyName)
    {
        if (memberAccess.Expression is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 1 } invocationExpressionSyntax &&
            invocationExpressionSyntax.ArgumentList.Arguments[0].Expression is SimpleLambdaExpressionSyntax { Body: MemberAccessExpressionSyntax memberAccessExpressionSyntax })
        {
            propertyName = memberAccessExpressionSyntax.Name.Identifier.Text;
            return true;
        }

        propertyName = string.Empty;
        return false;
    }

    private static Result<IMethodSymbol> ValidateConfigurationMethod(IMethodSymbol methodSymbol, ITypeSymbol sourceTypeSymbol, ITypeSymbol targetTypeSymbol)
    {
        if (methodSymbol.Parameters.Length != 1 ||
            methodSymbol.Parameters[0].Type is not INamedTypeSymbol configurationMethodTypeSymbol ||
            configurationMethodTypeSymbol.TypeArguments.Length != 2 ||
            configurationMethodTypeSymbol.TypeArguments[0].Equals(sourceTypeSymbol, SymbolEqualityComparer.Default) == false ||
            configurationMethodTypeSymbol.TypeArguments[1].Equals(targetTypeSymbol, SymbolEqualityComparer.Default) == false)
        {
            return DiagnosticsFactory.IncorrectConfigurationMethodParameters(methodSymbol, sourceTypeSymbol, targetTypeSymbol);
        }

        return Result.Success(methodSymbol);
    }

    private static MappingConfiguration CreateMappingConfiguration(AttributeData attribute, ITypeSymbol? sourceType) => new(
        sourceType?.ToTypeMapping() ?? throw new InvalidOperationException("Unable to determine source type from MapTo attribute."),
        attribute.GetNamedArgumentOrNull<bool>(nameof(MapFromAttribute.CopyPrimitiveArrays)),
        attribute.GetNamedArgumentOrNull<ReferenceHandling>(nameof(MapFromAttribute.ReferenceHandling)),
        attribute.GetNamedArgumentOrNull<NullHandling>(nameof(MapFromAttribute.NullHandling)),
        attribute.GetNamedArgumentOrNull<EnumMappingStrategy>(nameof(MapFromAttribute.EnumMappingStrategy)),
        attribute.GetNamedArgumentOrNull<StrictEnumMapping>(nameof(MapFromAttribute.StrictEnumMapping)),
        attribute.GetNamedArgument(nameof(MapFromAttribute.EnumMappingFallbackValue)),
        attribute.GetNamedArgumentOrNull<ProjectionType>(nameof(MapFromAttribute.ProjectTo)),
        attribute.GetNamedArgumentExpression(nameof(MapFromAttribute.BeforeMap)),
        AfterMap: attribute.GetNamedArgumentExpression(nameof(MapFromAttribute.AfterMap)),
        BeforeMapArgumentLocation: attribute.GetNamedArgumentLocation(nameof(MapFromAttribute.BeforeMap)),
        AfterMapArgumentLocation: attribute.GetNamedArgumentLocation(nameof(MapFromAttribute.AfterMap)),
        TypeConverters: ImmutableDictionary<string, TypeConverterMapping>.Empty,
        IgnoredProperties: ImmutableArray<string>.Empty,
        MappedProperties: ImmutableDictionary<string, (string From, NullHandling NullHandling)>.Empty);
}