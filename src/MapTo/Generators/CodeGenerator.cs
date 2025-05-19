using System.Text;
using MapTo.Mappings;
using Microsoft.CodeAnalysis.Text;

namespace MapTo.Generators;

internal readonly record struct CodeGenerator(
    TargetMapping Mapping,
    CompilerOptions CompilerOptions)
{
    public string HintName => Mapping.Namespace.IsGlobalNamespace
        ? $"{Mapping.Source.Name}To{Mapping.Name}.g.cs"
        : $"{Mapping.Source.Namespace}.{Mapping.Source.Name}To{Mapping.Namespace}.{Mapping.Name}.g.cs";

    public string Build()
    {
        var writer = new CodeWriter(CompilerOptions);
        var generators = new ICodeGenerator[]
        {
            new FileGenerator(CompilerOptions.NullableReferenceTypes, CompilerOptions.FileScopedNamespace, Mapping),
            new PartialClassGenerator(Mapping, CompilerOptions),
            new ExtensionClassGenerator(CompilerOptions, Mapping),
            new ExtensionEnumGenerator(CompilerOptions, Mapping)
        };

        return writer
            .WriteCodeGenerators(generators)
            .ToString();
    }
}

internal static class CodeGeneratorExtensions
{
    internal static void GenerateSource(this SourceProductionContext context, CodeGenerator generator)
    {
        var source = generator.Build();
        context.AddSource(generator.HintName, SourceText.From(source, Encoding.UTF8));
    }

    internal static CodeWriter WriteCodeGenerators(this CodeWriter writer, ICodeGenerator[] generators)
    {
        foreach (var generator in generators)
        {
            generator.BeginWrite(writer);
        }

        foreach (var generator in generators.OfType<ICodeBlockGenerator>())
        {
            generator.EndWrite(writer);
        }

        return writer;
    }
}