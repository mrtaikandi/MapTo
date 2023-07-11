namespace MapTo.Tests.SourceBuilders;

internal interface ITestSourceBuilder
{
    TestSourceBuilderOptions Options { get; }

    ImmutableArray<TestSource> Sources { get; }

    ImmutableArray<TestSource> Build();
}