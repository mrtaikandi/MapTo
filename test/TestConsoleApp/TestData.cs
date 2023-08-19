using MapTo;

namespace TestConsoleApp.TestData;

public class SourceClass
{
    public int Prop1 { get; set; }

    public SourceClass? Prop2 { get; set; }
}

[MapFrom(typeof(SourceClass))]
public class TargetClass
{
    public int Prop1 { get; set; }

    public TargetClass? Prop2 { get; set; }
}