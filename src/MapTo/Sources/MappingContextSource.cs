using System.Collections.Generic;
using static MapTo.Sources.Constants;

namespace MapTo.Sources
{
    internal static class MappingContextSource
    {
        internal const string ClassName = "MappingContext";
        internal const string FullyQualifiedName = RootNamespace + "." + ClassName;
        internal const string FactoryMethodName = "Create";
        internal const string RegisterMethodName = "Register";
        internal const string MapMethodName = "MapFromWithContext";
        
        internal static SourceCode Generate(SourceGenerationOptions options)
        {
            var usings = new List<string> { "System", "System.Collections.Generic", "System.Reflection" };

            using var builder = new SourceBuilder()
                .WriteLine(GeneratedFilesHeader)
                .WriteLine()
                .WriteUsings(usings)
                .WriteLine()

                // Namespace declaration
                .WriteLine($"namespace {RootNamespace}")
                .WriteOpeningBracket()

                // Class declaration
                .WriteLine($"internal sealed class {ClassName}")
                .WriteOpeningBracket()

                .WriteLine("private readonly Dictionary<object, object> _cache;")
                .WriteLine()

                // Constructor
                .WriteLine($"internal {ClassName}()")
                .WriteOpeningBracket()
                .WriteLine("_cache = new Dictionary<object, object>(1);")
                .WriteClosingBracket()
                .WriteLine()

                // Factory
                .WriteLine($"internal static TMapped {FactoryMethodName}<TOriginal, TMapped>(TOriginal original)")
                .WriteOpeningBracket()
                .WriteLine("if (original == null) throw new ArgumentNullException(nameof(original));")
                .WriteLine()
                .WriteLine("var context = new MappingContext();")
                .WriteLine("var mapped = context.MapFromWithContext<TOriginal, TMapped>(original);")
                .WriteLine()
                .WriteLine("if (mapped == null)")
                .WriteOpeningBracket()
                .WriteLine("throw new InvalidOperationException();")
                .WriteClosingBracket()
                .WriteLine()
                .WriteLine("return mapped;")
                .WriteClosingBracket()
                .WriteLine()

                // MapFromWithContext method
                .WriteLine($"internal TMapped MapFromWithContext<TOriginal, TMapped>(TOriginal original)")
                .WriteOpeningBracket()
                .WriteLine("if (original == null)")
                .WriteOpeningBracket()
                .WriteLine("return default(TMapped);")
                .WriteClosingBracket()
                .WriteLine()
                .WriteLine("if (!TryGetValue<TOriginal, TMapped>(original, out var mapped))")
                .WriteOpeningBracket()
                .WriteLine("var instance = Activator.CreateInstance(typeof(TMapped), BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { this, original }, null);")
                .WriteLine("if (instance != null)")
                .WriteOpeningBracket()
                .WriteLine("mapped = (TMapped)instance;")
                .WriteClosingBracket()
                .WriteClosingBracket()
                .WriteLine()
                .WriteLine("return mapped;")
                .WriteClosingBracket()
                .WriteLine()

                // Register method
                .WriteLine("internal void Register<TOriginal, TMapped>(TOriginal original, TMapped mapped)")
                .WriteOpeningBracket()
                .WriteLine("if (original == null) throw new ArgumentNullException(nameof(original));")
                .WriteLine("if (mapped == null) throw new ArgumentNullException(nameof(mapped));")
                .WriteLine()
                .WriteLine("if (!_cache.ContainsKey(original))")
                .WriteOpeningBracket()
                .WriteLine("_cache.Add(original, mapped);")
                .WriteClosingBracket()
                .WriteClosingBracket()
                .WriteLine()

                // TryGetValue method
                .WriteLine("private bool TryGetValue<TOriginal, TMapped>(TOriginal original, out TMapped mapped)")
                .WriteOpeningBracket()
                .WriteLine("if (original != null && _cache.TryGetValue(original, out var value))")
                .WriteOpeningBracket()
                .WriteLine("mapped = (TMapped)value;")
                .WriteLine("return true;")
                .WriteClosingBracket()
                .WriteLine()
                .WriteLine("mapped = default(TMapped);")
                .WriteLine("return false;")
                .WriteClosingBracket()

                // End class declaration
                .WriteClosingBracket()

                // End namespace declaration
                .WriteClosingBracket();
            
            return new(builder.ToString(), $"{ClassName}.g.cs");
        }
    }
}