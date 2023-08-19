using System.Reflection;
using System.Text;
using MapTo.Configuration;
using MapTo.Generators;
using MapTo.Mappings;
using Microsoft.CodeAnalysis.Text;

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
        initContext.RegisterPostInitializationOutput(RegisterResources);

        var mappingOptions = initContext.AnalyzerConfigOptionsProvider
            .Select(CodeGeneratorOptions.Create);

        var mappingContext = initContext.SyntaxProvider
            .CreateMappingContext()
            .Combine(mappingOptions)
            .Select(MappingContext.WithOptions);

        initContext.RegisterSourceOutput(mappingContext, Execute);
    }

    private static void Execute(SourceProductionContext context, MappingContext mappingContext)
    {
        var mapping = TargetMappingFactory.Create(mappingContext);
        if (mappingContext.HasError)
        {
            context.ReportDiagnostics(mappingContext.Diagnostics);
            return;
        }

        var codeGenerator = new CodeGenerator(mappingContext.CompilerOptions, mappingContext.CodeGeneratorOptions, mapping);
        context.GenerateSource(codeGenerator);
    }

    private static void RegisterResources(IncrementalGeneratorPostInitializationContext context)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var sources = assembly.GetManifestResourceNames()
            .Where(x => x.StartsWith("MapTo.Resources."));

        foreach (var source in sources)
        {
            using var resourceStream = assembly.GetManifestResourceStream(source) ?? throw new InvalidOperationException("Unable to get resource stream.");
            using var streamReader = new StreamReader(resourceStream);

            var name = Path.GetFileNameWithoutExtension(source);
            context.AddSource($"{name}.g.cs", SourceText.From(streamReader.ReadToEnd(), Encoding.UTF8));
        }
    }
}