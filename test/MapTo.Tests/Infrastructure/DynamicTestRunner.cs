using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;
using System.Text;

namespace MapTo.Tests.Infrastructure;

internal static class DynamicTestRunner
{
    private const string TestClassNamespace = "MapTo.Tests.DynamicCode";

    internal static CompilationResult ExecuteAndAssertDynamicCode(
        this ITestSourceBuilder builder,
        string dynamicClassName,
        [StringSyntax("csharp")] string code,
        IEnumerable<string>? usings = null,
        ITestOutputHelper? logger = null)
    {
        var testCodeFile = builder.AddFile($"{dynamicClassName}Runner", ns: TestClassNamespace, usings: usings);
        testCodeFile.AddClass(body: $$"""
                                      public static class {{dynamicClassName}}
                                      {
                                          public static string Execute()
                                          {
                                              try
                                              {
                                                  {{code}}
                                              }
                                              catch(System.Exception ex)
                                              {
                                                  return $"Execute:Exception::{ex.ToString()}";
                                              }
                                      
                                              return "Execute:Success";
                                          }
                                      }
                                      """);

        var builderResult = builder.Compile();
        builderResult.Compilation.Dump(logger);
        builderResult.Diagnostics.ShouldBeSuccessful();

        var fileName = Path.Combine(Directory.GetCurrentDirectory(), $"DynamicIntegrationTest_{dynamicClassName}.dll");
        var compilationResult = builderResult.Compilation.Emit(fileName);

        if (compilationResult.Success)
        {
            // Load the assembly
            var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(fileName);
            var executeMethod = asm.GetType($"{TestClassNamespace}.{dynamicClassName}")?.GetMethod("Execute");

            logger?.WriteLine("Executing...");
            var objectResult = executeMethod?.Invoke(null, null)?.ToString();
            logger?.WriteLine("Executed!");

            logger?.WriteLine("Result:");
            logger?.WriteLine(objectResult ?? "null");

            if (objectResult != "Execute:Success")
            {
                Assert.Fail($"Dynamic code execution failed: {objectResult}");
            }
        }
        else
        {
            var errorBuilder = new StringBuilder();
            foreach (var diagnostic in compilationResult.Diagnostics)
            {
                errorBuilder.AppendLine(diagnostic.ToString());
            }

            Assert.Fail(errorBuilder.ToString());
        }

        return builderResult;
    }
}