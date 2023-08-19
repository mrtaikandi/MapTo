namespace MapTo.Tests.Extensions;

internal static class DumpExtensions
{
    internal static void Dump(this ITestSourceBuilder builder, ITestOutputHelper output, ITestFileBuilder file) =>
        Dump(output, builder.GetSourceContent(file));

    internal static void Dump<T>(this T? value, ITestOutputHelper? output) =>
        Dump(output, value);

    internal static void Dump<T>(this ITestOutputHelper? output, T? value)
    {
        if (output is null)
        {
            return;
        }

        output.WriteLine("------------------ DUMP ------------------");
        output.WriteLine(value switch
        {
            string s => s,
            SyntaxTree tree => tree.ToString(),
            Compilation compilation => compilation.PrintSyntaxTree(),
            _ => value?.ToString() ?? "NULL"
        });

        output.WriteLine("-------------------------------------------");
    }

    private static void Dump<T>(this ITestOutputHelper output, T value, Func<T, string?> action)
    {
        output.WriteLine("------------------ DUMP ------------------");
        output.WriteLine(action(value) ?? "NULL");
        output.WriteLine("-------------------------------------------");
    }
}