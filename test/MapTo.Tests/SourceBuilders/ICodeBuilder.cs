using MapTo.Generators;

namespace MapTo.Tests.SourceBuilders;

internal interface ICodeBuilder
{
    void Build(CodeWriter writer, TestSourceBuilderOptions options);

    public int GetOrder() => 10;
}

internal interface IMemberedCodeBuilder : ICodeBuilder
{
    IReadOnlyCollection<ICodeBuilder> Members { get; }

    void AddMember(ICodeBuilder member);
}

internal record SimpleCodeBuilder(int Order, Action<CodeWriter, TestSourceBuilderOptions> Builder) : ICodeBuilder
{
    /// <inheritdoc />
    public void Build(CodeWriter writer, TestSourceBuilderOptions options)
    {
        Builder(writer, options);
    }

    public int GetOrder() => Order;
}

internal static class CodeBuilderExtensions
{
    internal static void BuildAll(this IEnumerable<ICodeBuilder> builders, CodeWriter writer, TestSourceBuilderOptions options)
    {
        var index = 0;
        foreach (var builder in builders.OrderBy(o => o.GetOrder()))
        {
            writer.WriteLineIf(++index > 1);
            builder.Build(writer, options);
        }
    }
}