namespace MapTo.Tests.Infrastructure;

internal readonly record struct CompilationResult(Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics);