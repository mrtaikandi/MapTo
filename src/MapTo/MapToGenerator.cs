using MapTo.Generators;
using MapTo.Mappings;

namespace MapTo;

/// <summary>
/// MapTo source generator.
/// </summary>
[Generator]
public class MapToGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        var mappingOptions = initContext.AnalyzerConfigOptionsProvider
            .Select(CodeGeneratorOptions.Create)
            .WithTrackingName("CreateCodeGeneratorOptions");

        var mappingContext = initContext.SyntaxProvider
            .CreateMappingContext()
            .Combine(mappingOptions)
            .Select(MappingContext.WithOptions)
            .WithTrackingName("ApplyCodeGeneratorOptions");

        initContext.RegisterSourceOutput(mappingContext, Execute);
    }

    private static void Execute(SourceProductionContext context, MappingContext mappingContext)
    {
        if (mappingContext.HasError)
        {
            context.ReportDiagnostics(mappingContext.Diagnostics);
            return;
        }

        var mapping = TargetMappingFactory.Create(mappingContext);
        if (mappingContext.HasError)
        {
            context.ReportDiagnostics(mappingContext.Diagnostics);
            return;
        }

        var codeGenerator = new CodeGenerator(mapping, mappingContext.CompilerOptions);
        context.GenerateSource(codeGenerator);
    }
}