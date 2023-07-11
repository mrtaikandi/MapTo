using System;
using MapTo;

namespace TestConsoleApp;

public class Program
{
    public static void Main()
    {
        Console.WriteLine("Hello, World!");

        var source = new SourceClass { Id = 1 };
        var target = source.MapToTargetClass();
    }
}

public class SourceClass
{
    public int Id { get; set; }
}

[MapFrom(typeof(SourceClass))]
public class TargetClass
{
    public int Id { get; set; }
}