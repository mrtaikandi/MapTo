using System;
using TestConsoleApp.TestData;

namespace TestConsoleApp;

public class Program
{
    public static void Main()
    {
        Console.WriteLine("Hello, World!");

        var source = new SourceClass
        {
            Prop1 = 1,
            Prop2 = new SourceClass
            {
                Prop1 = 2
            }
        };

        var target = source.MapToTargetClass();

        Console.WriteLine(target.Prop1);
    }
}